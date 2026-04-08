using Credential.Models;
using Credential.RedisDB;
using Credential.Services;
using Credential.Services.Interface;
using Credential.Services.Utilities;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.DependencyInjection;
using NLog.Web;
using StackExchange.Redis;
using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = null; // Ensures PascalCase in response
});
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    // Let controllers and middleware produce existing response format instead of automatic validation payloads.
    options.SuppressModelStateInvalidFilter = true;
});
SecureStringHelper.Initialize(builder.Configuration);
var useVault = builder.Configuration.GetValue<bool>("UseVault", false);
var redisUrlKey = builder.Configuration["Redis:UrlKey"];
var redisUsernameKey = builder.Configuration["Redis:UsernameKey"];
var redisPasswordKey = builder.Configuration["Redis:PasswordKey"];

var redisConnectionSetting = useVault && !string.IsNullOrWhiteSpace(redisUrlKey)
    ? redisUrlKey
    : builder.Configuration["redisConnectionString"];
var redisUsernameSetting = useVault && !string.IsNullOrWhiteSpace(redisUsernameKey)
    ? redisUsernameKey
    : builder.Configuration["redisUsername"];
var redisPasswordSetting = useVault && !string.IsNullOrWhiteSpace(redisPasswordKey)
    ? redisPasswordKey
    : builder.Configuration["redisPassword"];
var redisConnectionString = await SecureStringHelper.Decrypt(redisConnectionSetting ?? string.Empty);
string? redisUsername = null;
if (!string.IsNullOrWhiteSpace(redisUsernameSetting))
{
    redisUsername = await SecureStringHelper.Decrypt(redisUsernameSetting);
}
string? redisConnectionPassword = null;
if (!string.IsNullOrWhiteSpace(redisPasswordSetting))
{
    redisConnectionPassword = await SecureStringHelper.Decrypt(redisPasswordSetting);
}
// Register Redis connection as a singleton
builder.Services.AddSingleton<IConnectionMultiplexer>(provider =>
{
    var endpoints = (redisConnectionString ?? string.Empty)
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    if (endpoints.Length == 0)
    {
        throw new InvalidOperationException("Redis connection string is missing or invalid.");
    }

    var isCluster = endpoints.Length > 1;
    var configurationOptions = new ConfigurationOptions
    {
        User = redisUsername,
        Password = redisConnectionPassword,
        AbortOnConnectFail = false,
    };

    if (!isCluster)
    {
        configurationOptions.EndPoints.Add(endpoints[0]);
        configurationOptions.ConnectTimeout = 100000;
    }
    else
    {
        configurationOptions.AllowAdmin = false;
        configurationOptions.ConnectRetry = 5;
        configurationOptions.ConnectTimeout = 10000;
        configurationOptions.SyncTimeout = 10000;
        configurationOptions.KeepAlive = 180;
        configurationOptions.TieBreaker = string.Empty;
        configurationOptions.CommandMap = CommandMap.Default;
        configurationOptions.DefaultVersion = new Version(6, 0);

        foreach (var endpoint in endpoints)
        {
            configurationOptions.EndPoints.Add(endpoint);
        }
    }

    return ConnectionMultiplexer.Connect(configurationOptions);
});

// Register VerifiableCredentialService and HttpClient
builder.Services.AddHttpClient<IVerifiableCredentialService, VerifiableCredentialService>();
builder.Services.AddScoped<IRedisTransactionStore, RedisTransactionStore>();

// Optionally, adjust service lifetime for VerifiableCredentialService
// builder.Services.AddScoped<IVerifiableCredentialService, VerifiableCredentialService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder => builder.AllowAnyOrigin()
                                                    .AllowAnyHeader()
                                                    .AllowAnyMethod());
});

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddLogging();
builder.Logging.ClearProviders();
builder.Logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information);
builder.Host.UseNLog();

var app = builder.Build();

// Forwarded headers (very important behind proxy / TLS termination)
var forwardOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto
};
forwardOptions.KnownNetworks.Clear();
forwardOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardOptions);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseStatusCodePages(async statusCodeContext =>
{
    var response = statusCodeContext.HttpContext.Response;
    if (response.HasStarted)
    {
        return;
    }

    if (response.ContentLength.HasValue && response.ContentLength.Value > 0)
    {
        return;
    }

    response.ContentType = "application/json";
    var payload = JsonSerializer.Serialize(new
    {
        success = false,
        message = $"HTTP {response.StatusCode}"
    });
    await response.WriteAsync(payload);
});

// Global exception handling middleware - must be before routing/controllers
app.UseMiddleware<Credential.Middleware.GlobalExceptionMiddleware>();

app.UseAuthorization();
app.MapControllers();

app.Run();
