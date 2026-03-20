using Credential.Models;
using Credential.Services;
using Credential.Services.Interface;
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
// Register Redis connection as a singleton
builder.Services.AddSingleton<IConnectionMultiplexer>(provider =>
{
    var configurationOptions = new ConfigurationOptions
    {
        EndPoints = { builder.Configuration["RedisSettings:redisConnectionString"] },
        Password = builder.Configuration["RedisSettings:redisPassword"],
        AbortOnConnectFail = false,
        ConnectTimeout = 100000,
    };

    return ConnectionMultiplexer.Connect(configurationOptions);
});

// Register VerifiableCredentialService and HttpClient
builder.Services.AddHttpClient<IVerifiableCredentialService, VerifiableCredentialService>();

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

// ================= PIPELINE =================

// IMPORTANT: Base path for reverse proxy hosting
var basePath = "/credential-exchange";
app.UsePathBase(basePath);

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

// Swagger MUST be enabled in production
app.UseSwagger(c =>
{
    c.PreSerializeFilters.Add((swaggerDoc, httpReq) =>
    {
        swaggerDoc.Servers = new List<OpenApiServer>
        {
            new() { Url = "/credential-exchange" }
        };
    });
});
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint($"{basePath}/swagger/v1/swagger.json", "Credential API V1");
    c.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();

app.UseCors("AllowAll");

// Global exception handling middleware - must be before routing/controllers
app.UseMiddleware<Credential.Middleware.GlobalExceptionMiddleware>();

app.UseAuthorization();
app.MapControllers();

app.Run();
