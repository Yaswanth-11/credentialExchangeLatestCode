using Credential.Models;
using Lux.Infrastructure;
using System.Net;
using System.Text.Json;

namespace Credential.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = null // PascalCase to match existing convention
        };

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (LxException ex)
            {
                _logger.LogError(ex,
                    "LxException occurred. ExceptionType={ExceptionType} Code={Code} Path={Path} TraceId={TraceId}",
                    nameof(LxException), ex.Code, context.Request.Path, context.TraceIdentifier);

                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                context.Response.ContentType = "application/json";

                var result = new ServiceResult(false, ex.Message, ex.Code,
                    LxErrorCodes.GetErrorMessage(ex.Code), null);

                await context.Response.WriteAsync(JsonSerializer.Serialize(result, JsonOptions));
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex,
                    "UnauthorizedAccessException occurred. ExceptionType={ExceptionType} Path={Path} TraceId={TraceId}",
                    nameof(UnauthorizedAccessException), context.Request.Path, context.TraceIdentifier);

                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                context.Response.ContentType = "application/json";

                await context.Response.WriteAsync(
                    JsonSerializer.Serialize(new { success = false, message = ex.Message }, JsonOptions));
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex,
                    "ArgumentException occurred. ExceptionType={ExceptionType} Path={Path} TraceId={TraceId}",
                    nameof(ArgumentException), context.Request.Path, context.TraceIdentifier);

                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                context.Response.ContentType = "application/json";

                await context.Response.WriteAsync(
                    JsonSerializer.Serialize(new { success = false, message = ex.Message }, JsonOptions));
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogError(ex,
                    "KeyNotFoundException occurred. ExceptionType={ExceptionType} Path={Path} TraceId={TraceId}",
                    nameof(KeyNotFoundException), context.Request.Path, context.TraceIdentifier);

                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                context.Response.ContentType = "application/json";

                await context.Response.WriteAsync(
                    JsonSerializer.Serialize(new { success = false, message = ex.Message }, JsonOptions));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unhandled exception occurred. ExceptionType={ExceptionType} Path={Path} TraceId={TraceId}",
                    ex.GetType().Name, context.Request.Path, context.TraceIdentifier);

                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                context.Response.ContentType = "application/json";

                await context.Response.WriteAsync(
                    JsonSerializer.Serialize(new { success = false, message = "An unexpected error occurred." }, JsonOptions));
            }
        }
    }
}
