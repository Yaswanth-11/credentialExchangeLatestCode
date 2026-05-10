using Credential.Models;
using Credential.Models.Exceptions;
using Credential.Services.Utilities;
using Lux.Infrastructure;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Credential.Middleware
{
    public class ChecksumValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ChecksumValidationMiddleware> _logger;

        public ChecksumValidationMiddleware(
            RequestDelegate next,
            ILogger<ChecksumValidationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Optional:
            // Skip GET/DELETE requests
            if (HttpMethods.IsGet(context.Request.Method) ||
                HttpMethods.IsDelete(context.Request.Method))
            {
                await _next(context);
                return;
            }

            // Read checksum header
            string checksum = context.Request.Headers["X-Checksum"].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(checksum))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;

                await context.Response.WriteAsJsonAsync(
                    new ServiceResult(
                        false,
                        "Checksum header missing.",
                        401,
                        "Checksum validation failed",
                        null));

                return;
            }

            // Enable rereading body
            context.Request.EnableBuffering();

            string requestBody;

            using (var reader = new StreamReader(
                context.Request.Body,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                leaveOpen: true))
            {
                requestBody = await reader.ReadToEndAsync();
            }

            // Reset stream position
            context.Request.Body.Position = 0;

            if (string.IsNullOrWhiteSpace(requestBody))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;

                await context.Response.WriteAsJsonAsync(
                    new ServiceResult(
                        false,
                        "Request body empty.",
                        400,
                        "Validation failed",
                        null));

                return;
            }

            Console.WriteLine($"Request Body: {requestBody}");


            byte[] requestBytes =
                Encoding.UTF8.GetBytes(requestBody);

            

            //verify checksum
            int verifyResult =
                     PKIMethods.Instance.VerifyChecksum(
                        requestBytes,
                        checksum);

            if (verifyResult != 1)
            {
                _logger.LogWarning(
                    "Checksum validation failed. Path={Path}",
                    context.Request.Path);

                context.Response.StatusCode =
                    StatusCodes.Status401Unauthorized;

                await context.Response.WriteAsJsonAsync(
                    new ServiceResult(
                        false,
                        "Request integrity validation failed.",
                        401,
                        "Checksum verification failed",
                        null));

                return;
            }

            await _next(context);
        }
    }
}