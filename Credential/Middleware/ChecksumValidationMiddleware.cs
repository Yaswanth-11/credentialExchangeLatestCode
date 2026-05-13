using Credential.Models;
using Credential.Models.Exceptions;
using Credential.RedisDB;
using Credential.Services.Utilities;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Credential.Middleware
{
    public class ChecksumValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ChecksumValidationMiddleware> _logger;
        private readonly ChecksumValidationOptions _options;

        private readonly IRedisTransactionStore _redisTransactionStore;

        private const string NonceKeyPrefix =
            "wallet:nonce:";

        private const string NonceDataType =
            "nonce";

        public ChecksumValidationMiddleware(
            RequestDelegate next,
            ILogger<ChecksumValidationMiddleware> logger,
            IOptions<ChecksumValidationOptions> options,
            IRedisTransactionStore redisTransactionStore)
        {
            _next = next;
            _logger = logger;
             _options = options.Value;
            _redisTransactionStore = redisTransactionStore;
        }

        private static JsonNode? CanonicalizeJson(
    JsonNode? node)
        {
            if (node == null)
                return null;

            // Object
            if (node is JsonObject obj)
            {
                JsonObject sortedObj = new();

                foreach (var property in obj
                    .OrderBy(p => p.Key, StringComparer.Ordinal))
                {
                    sortedObj[property.Key] =
                        CanonicalizeJson(property.Value);
                }

                return sortedObj;
            }

            // Array
            if (node is JsonArray arr)
            {
                JsonArray newArr = new();

                foreach (var item in arr)
                {
                    newArr.Add(
                        CanonicalizeJson(item));
                }

                return newArr;
            }

            // Primitive
            return node.DeepClone();
        }


        public async Task InvokeAsync(HttpContext context)
        {
            if (ShouldSkipRequest(context))
            {
                await _next(context);
                return;
            }

            string? platform =
                context.Request.Headers["X-Client-Platform"].FirstOrDefault();
            bool isWeb = IsWebPlatform(platform);
            bool isGetRequest = HttpMethods.IsGet(context.Request.Method);

            if (!IsValidationEnabledForPlatform(isWeb) ||
                !IsValidationRequiredForMethod(isWeb, isGetRequest))
            {
                await _next(context);
                return;
            }

            string checksum = GetRequiredHeader(
                context,
                "X-Checksum",
                ApiMessages.ChecksumHeaderMissing);

            string requestBody = await ReadRequestBodyAsync(context);

            if (!isGetRequest &&
                string.IsNullOrWhiteSpace(requestBody))
            {
                throw new ArgumentException(
                    ApiMessages.RequestBodyEmpty);
            }

            if (isWeb)
            {
                await ValidateWebChecksumAsync(
                    context,
                    checksum,
                    requestBody);
            }
            else
            {
                ValidateMobileChecksum(
                    context,
                    checksum,
                    requestBody);
            }

            await _next(context);
        }

        private bool ShouldSkipRequest(HttpContext context)
        {
            if (HttpMethods.IsDelete(context.Request.Method))
            {
                return true;
            }

            return !_options.Enabled;
        }

        private bool IsValidationEnabledForPlatform(bool isWeb)
        {
            if (isWeb)
            {
                return _options.EnableWeb;
            }

            return _options.EnableMobile;
        }

        private bool IsValidationRequiredForMethod(
            bool isWeb,
            bool isGetRequest)
        {
            if (isWeb)
            {
                if (isGetRequest)
                {
                    return _options.WebRequireChecksumForGet ?? true;
                }

                return _options.WebRequireChecksumForNonGet ?? true;
            }

            if (isGetRequest)
            {
                return _options.MobileRequireChecksumForGet ?? true;
            }

            return _options.MobileRequireChecksumForNonGet ?? true;
        }

        private static bool IsWebPlatform(string? platform)
        {
            return string.Equals(
                platform,
                "web",
                StringComparison.OrdinalIgnoreCase);
        }

        private static string GetRequiredHeader(
            HttpContext context,
            string headerName,
            string errorMessage)
        {
            string? value =
                context.Request.Headers[headerName].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new UnauthorizedAccessException(errorMessage);
            }

            return value;
        }

        private static async Task<string> ReadRequestBodyAsync(HttpContext context)
        {
            context.Request.EnableBuffering();

            using var reader = new StreamReader(
                context.Request.Body,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                leaveOpen: true);

            string requestBody =
                await reader.ReadToEndAsync();

            context.Request.Body.Position = 0;

            return requestBody;
        }

        private async Task ValidateWebChecksumAsync(
            HttpContext context,
            string checksum,
            string requestBody)
        {
            (string timestampHeader, string nonce) =
                GetWebHeaders(context);

            ValidateTimestamp(timestampHeader);

            string checksumData =
                GetChecksumDataForWeb(context, requestBody);

            string payload =
                $"{timestampHeader}|{nonce}|{checksumData}";

            string generatedChecksum =
                GenerateChecksum(payload);

            if (!string.Equals(
                    generatedChecksum,
                    checksum,
                    StringComparison.Ordinal))
            {
                _logger.LogWarning(
                    "Checksum validation failed. Path={Path}",
                    context.Request.Path);

                throw new UnauthorizedAccessException(
                    ApiMessages.RequestIntegrityFailed);
            }

            await EnforceNonceReplayProtectionAsync(nonce);
        }

        private async Task EnforceNonceReplayProtectionAsync(string nonce)
        {
            if (!_options.EnableReplayAttackProtection)
            {
                return;
            }

            TimeSpan ttl = GetNonceTtl();

            string key =
                NonceKeyPrefix + nonce;

            bool stored;

            try
            {
                stored = await _redisTransactionStore.TryStoreStringAsync(
                    key,
                    nonce,
                    "1",
                    NonceDataType,
                    ttl);
            }
            catch (Exception ex) when (
                ex is TransactionStateException ||
                ex is RedisException)
            {
                _logger.LogWarning(
                    ex,
                    "Nonce replay check failed. Key={Key}",
                    key);

                throw new UnauthorizedAccessException(
                    ApiMessages.RequestIntegrityFailed);
            }

            if (!stored)
            {
                throw new UnauthorizedAccessException(
                    ApiMessages.ReplayAttackDetected);
            }
        }

        private TimeSpan GetNonceTtl()
        {
            int ttlSeconds =
                _options.NonceTtlSeconds;

            if (ttlSeconds <= 0)
            {
                ttlSeconds = 120;
            }

            return TimeSpan.FromSeconds(ttlSeconds);
        }

        private static (string TimestampHeader, string Nonce) GetWebHeaders(
            HttpContext context)
        {
            string? timestampHeader =
                context.Request.Headers["X-Timestamp"].FirstOrDefault();
            string? nonce =
                context.Request.Headers["X-Nonce"].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(timestampHeader) ||
                string.IsNullOrWhiteSpace(nonce))
            {
                throw new UnauthorizedAccessException(
                    ApiMessages.TimestampOrNonceMissing);
            }

            return (timestampHeader, nonce);
        }

        private void ValidateTimestamp(string timestampHeader)
        {
            if (!long.TryParse(timestampHeader, out long requestTimestamp))
            {
                throw new UnauthorizedAccessException(
                    ApiMessages.InvalidTimestamp);
            }

            long currentTimestamp =
                DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            long diff =
                Math.Abs(currentTimestamp - requestTimestamp);

            if (diff > _options.AllowedTimestampDriftSeconds)
            {
                throw new UnauthorizedAccessException(
                    ApiMessages.RequestExpired);
            }
        }

        private string GetChecksumDataForWeb(
            HttpContext context,
            string requestBody)
        {
            if (HttpMethods.IsGet(context.Request.Method))
            {
                return context.Request.Path.Value?
                    .Split('/', StringSplitOptions.RemoveEmptyEntries)
                    .LastOrDefault() ?? string.Empty;
            }

            JsonNode? jsonNode;

            try
            {
                jsonNode = JsonNode.Parse(requestBody);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Invalid JSON payload. Path={Path}",
                    context.Request.Path);

                throw new ArgumentException(
                    ApiMessages.InvalidJsonPayload);
            }

            if (jsonNode == null)
            {
                throw new ArgumentException(
                    ApiMessages.InvalidJsonPayload);
            }

            JsonNode? canonicalJson = CanonicalizeJson(jsonNode);

            return canonicalJson!.ToJsonString(
                new JsonSerializerOptions
                {
                    WriteIndented = false
                });
        }

        private static string GenerateChecksum(string payload)
        {
            byte[] payloadBytes =
                Encoding.UTF8.GetBytes(payload);

            byte[] hashBytes =
                SHA256.HashData(payloadBytes);

            return Convert.ToBase64String(hashBytes);
        }

        private void ValidateMobileChecksum(
            HttpContext context,
            string checksum,
            string requestBody)
        {
            bool isGetRequest =
                HttpMethods.IsGet(context.Request.Method);

            string checksumData =
                isGetRequest
                    ? context.Request.Path.Value?
                        .Split('/', StringSplitOptions.RemoveEmptyEntries)
                        .LastOrDefault() ?? string.Empty
                    : requestBody;

            int requestType =
                isGetRequest ? 0 : 1;

            byte[] requestBytes =
                Encoding.UTF8.GetBytes(checksumData);

            int verifyResult =
                PKIMethods.Instance.VerifyChecksum(
                   requestBytes,
                   checksum,
                   requestType);

            if (verifyResult != 1)
            {
                _logger.LogWarning(
                    "Checksum validation failed. Path={Path}",
                    context.Request.Path);

                throw new UnauthorizedAccessException(
                    ApiMessages.RequestIntegrityFailed);
            }
        }
    }
}