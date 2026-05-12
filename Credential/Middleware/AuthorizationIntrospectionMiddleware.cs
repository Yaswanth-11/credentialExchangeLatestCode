using Credential.Models;
using Credential.Models.Exceptions;
using Credential.RedisDB;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Credential.Middleware
{
    public class AuthorizationIntrospectionMiddleware
    {
        private readonly RequestDelegate _next;

        private readonly ILogger<AuthorizationIntrospectionMiddleware> _logger;

        private readonly IHttpClientFactory _httpClientFactory;

        private readonly TokenIntrospectionOptions _options;

        private readonly IRedisTransactionStore _redisTransactionStore;

        private static readonly TimeSpan TokenCacheTtl =
            TimeSpan.FromMinutes(5);

        private const string TokenCacheKeyPrefix =
            "wallet:token:";

        private const string TokenCacheDataType =
            "token-introspection";

        public AuthorizationIntrospectionMiddleware(
            RequestDelegate next,
            ILogger<AuthorizationIntrospectionMiddleware> logger,
            IHttpClientFactory httpClientFactory,
            IRedisTransactionStore redisTransactionStore,
            IOptions<TokenIntrospectionOptions> options)
        {
            _next = next;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _redisTransactionStore = redisTransactionStore;
            _options = options.Value;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (ShouldSkipRequest(context))
            {
                await _next(context);
                return;
            }

            string accessToken =
                GetAccessToken(context);

            string cacheKey =
                BuildCacheKey(accessToken);

            TokenCacheEntry? cachedToken =
                await TryGetCachedTokenAsync(cacheKey);

            if (cachedToken != null && cachedToken.Active)
            {
                ApplyContextItems(context, cachedToken, accessToken);
                await _next(context);
                return;
            }

            TokenCacheEntry cacheEntry =
                await IntrospectTokenAsync(accessToken);

            await CacheTokenAsync(cacheKey, cacheEntry);
            ApplyContextItems(context, cacheEntry, accessToken);

            await _next(context);
        }

        private bool ShouldSkipRequest(HttpContext context)
        {
            if (!_options.Enabled)
            {
                return true;
            }

            if (context.Request.Path.StartsWithSegments("/health"))
            {
                return true;
            }

            return context.Request.Path.StartsWithSegments(
                "/api/verifier/presentation/request/uri");
        }

        private static string BuildCacheKey(string accessToken)
        {
            using var sha = SHA256.Create();

            return TokenCacheKeyPrefix +
                   Convert.ToHexString(
                       sha.ComputeHash(
                           Encoding.UTF8.GetBytes(accessToken)));
        }

        private static string GetAccessToken(HttpContext context)
        {
            string? authHeader =
                context.Request.Headers["Authorization"].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(authHeader))
            {
                throw new UnauthorizedAccessException(
                    ApiMessages.AuthorizationHeaderMissing);
            }

            if (!authHeader.StartsWith(
                    "Bearer ",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException(
                    ApiMessages.InvalidAuthorizationScheme);
            }

            string accessToken =
                authHeader.Substring("Bearer ".Length).Trim();

            if (string.IsNullOrWhiteSpace(accessToken))
            {
                throw new UnauthorizedAccessException(
                    ApiMessages.AccessTokenMissing);
            }

            return accessToken;
        }

        private static void ApplyContextItems(
            HttpContext context,
            TokenCacheEntry cacheEntry,
            string accessToken)
        {
            context.Items["client_id"] =
                cacheEntry.ClientId;

            context.Items["username"] =
                cacheEntry.Username;

            context.Items["scope"] =
                cacheEntry.Scope;

            context.Items["org_id"] =
                cacheEntry.OrgId;

            context.Items["access_token"] =
                accessToken;
        }

        private async Task<TokenCacheEntry> IntrospectTokenAsync(
            string accessToken)
        {
            HttpClient client =
                _httpClientFactory.CreateClient();

            client.Timeout =
                TimeSpan.FromSeconds(
                    _options.TimeoutSeconds);

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(
                    "Basic",
                    _options.BasicAuth);

            using var form =
                new FormUrlEncodedContent(
                    new Dictionary<string, string>
                    {
                        ["token"] = accessToken
                    });

            HttpResponseMessage response;

            try
            {
                response =
                    await client.PostAsync(
                        _options.IntrospectionUrl,
                        form);
            }
            catch (Exception ex) when (
                ex is HttpRequestException || ex is TaskCanceledException)
            {
                _logger.LogWarning(
                    ex,
                    "Introspection API request failed");

                throw new UnauthorizedAccessException(
                    ApiMessages.TokenValidationFailed);
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Introspection API failed. StatusCode={StatusCode}",
                    response.StatusCode);

                throw new UnauthorizedAccessException(
                    ApiMessages.TokenValidationFailed);
            }

            string responseJson =
                await response.Content.ReadAsStringAsync();

            JsonDocument doc;

            try
            {
                doc = JsonDocument.Parse(responseJson);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Introspection API returned invalid JSON");

                throw new UnauthorizedAccessException(
                    ApiMessages.TokenValidationFailed);
            }

            using (doc)
            {
                return BuildCacheEntry(doc.RootElement);
            }
        }

        private static TokenCacheEntry BuildCacheEntry(
            JsonElement root)
        {
            if (!root.TryGetProperty(
                    "active",
                    out JsonElement activeElement) ||
                (activeElement.ValueKind != JsonValueKind.True &&
                 activeElement.ValueKind != JsonValueKind.False))
            {
                throw new UnauthorizedAccessException(
                    ApiMessages.TokenValidationFailed);
            }

            bool active =
                activeElement.GetBoolean();

            if (!active)
            {
                throw new UnauthorizedAccessException(
                    ApiMessages.AccessTokenInactive);
            }

            string? clientId = null;
            string? username = null;
            string? scope = null;
            string? orgId = null;

            if (root.TryGetProperty(
                "client_id",
                out JsonElement clientIdElement))
            {
                clientId =
                    clientIdElement.GetString();
            }

            if (root.TryGetProperty(
                "username",
                out JsonElement usernameElement))
            {
                username =
                    usernameElement.GetString();
            }

            if (root.TryGetProperty(
                    "scope",
                    out JsonElement scopeElement))
            {
                scope =
                    scopeElement.GetString();
            }

            if (root.TryGetProperty(
                    "org_id",
                    out JsonElement orgIdElement))
            {
                orgId =
                    orgIdElement.GetString();
            }

            return new TokenCacheEntry
            {
                Active = true,
                ClientId = clientId,
                Username = username,
                Scope = scope,
                OrgId = orgId
            };
        }

        private async Task<TokenCacheEntry?> TryGetCachedTokenAsync(
            string cacheKey)
        {
            try
            {
                return await _redisTransactionStore.GetRequiredObjectAsync<TokenCacheEntry>(
                    cacheKey,
                    cacheKey,
                    TokenCacheDataType);
            }
            catch (TransactionStateException)
            {
                return null;
            }
            catch (RedisException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Redis token cache read failed. Key={Key}",
                    cacheKey);

                return null;
            }
        }

        private async Task CacheTokenAsync(
            string cacheKey,
            TokenCacheEntry cacheEntry)
        {
            try
            {
                string payload =
                    JsonSerializer.Serialize(cacheEntry);

                await _redisTransactionStore.StoreStringAsync(
                    cacheKey,
                    cacheKey,
                    payload,
                    TokenCacheDataType,
                    TokenCacheTtl);
            }
            catch (Exception ex) when (
                ex is TransactionStateException ||
                ex is RedisException ||
                ex is JsonException)
            {
                _logger.LogWarning(
                    ex,
                    "Redis token cache write failed. Key={Key}",
                    cacheKey);
            }
        }
    }
}