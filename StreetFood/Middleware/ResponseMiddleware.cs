using BO.Common;
using BO.Exceptions;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq; 
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace StreetFood.Middleware
{
    public class ResponseMiddleware
    {
        private readonly RequestDelegate _next;

        public ResponseMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            // 1. Skip Middleware for Swagger/Scalar/Uploads
            var path = context.Request.Path.Value?.ToLower();
            if (path.StartsWith("/scalar") || path.StartsWith("/openapi") ||
                path.StartsWith("/swagger") || path.StartsWith("/uploads"))
            {
                await _next(context);
                return;
            }

            var originalBodyStream = context.Response.Body;
            using var memoryStream = new MemoryStream();
            context.Response.Body = memoryStream;

            try
            {
                await _next(context);

                memoryStream.Seek(0, SeekOrigin.Begin);
                var responseBody = await new StreamReader(memoryStream).ReadToEndAsync();
                memoryStream.Seek(0, SeekOrigin.Begin);

                if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
                {
                    await HandleSuccessAsync(context, responseBody, memoryStream);
                }
                else
                {
                    await HandleErrorAsync(context, responseBody, context.Response.StatusCode, memoryStream);
                }

                memoryStream.Seek(0, SeekOrigin.Begin);
                await memoryStream.CopyToAsync(originalBodyStream);
            }
            catch (Exception ex)
            {
                context.Response.Body = originalBodyStream;
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleSuccessAsync(HttpContext context, string body, MemoryStream memoryStream)
        {
            var data = string.IsNullOrEmpty(body) ? null : JsonSerializer.Deserialize<object>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            var response = new ApiResponse<object>(context.Response.StatusCode, "Success", data);
            await WriteResponseAsync(context, response, memoryStream);
        }

        private async Task HandleErrorAsync(HttpContext context, string body, int statusCode, MemoryStream memoryStream)
        {
            string message = "An error occurred";
            string errorCode = $"ERR_{statusCode}";
            object data = null;

            switch (statusCode)
            {
                case 400:
                    errorCode = "ERR_BAD_REQUEST";
                    message = "Bad Request";

                    if (!string.IsNullOrWhiteSpace(body))
                    {
                        try
                        {
                            using (JsonDocument doc = JsonDocument.Parse(body))
                            {

                                if (doc.RootElement.TryGetProperty("errors", out JsonElement errorsElement))
                                {
                                    // 1. Deserialize to a Dictionary we can manipulate
                                    var rawErrors = JsonSerializer.Deserialize<Dictionary<string, string[]>>(
                                        errorsElement.GetRawText(),
                                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                                    );

                                    var simplifiedErrors = new Dictionary<string, string[]>();

                                    if (rawErrors != null)
                                    {
                                        foreach (var field in rawErrors)
                                        {
                                            var fieldName = field.Key;
                                            var errorMessages = field.Value;

                                            // Check if any error is a "Required" error
                                            var requiredError = errorMessages.FirstOrDefault(e => e.Contains("required", StringComparison.OrdinalIgnoreCase));

                                            if (requiredError != null)
                                            {
                                                // If field is required, that is the ONLY important error.
                                                simplifiedErrors[fieldName] = new[] { requiredError };
                                            }
                                            else if (errorMessages.Length > 0)
                                            {
                                                // Otherwise, just take the FIRST error to keep UI clean.
                                                simplifiedErrors[fieldName] = new[] { errorMessages[0] };
                                            }
                                        }
                                    }

                                    data = simplifiedErrors;
                                    message = "Validation Error";
                                    errorCode = "ERR_VALIDATION";
                                }
                                else
                                {
                                    // Handle manual BadRequest
                                    data = JsonSerializer.Deserialize<object>(body);
                                }
                            }
                        }
                        catch
                        {
                            data = body;
                        }
                    }
                    break;
                case 401:
                    errorCode = "ERR_401";
                    message = "Unauthorized. Please log in.";
                    break;
                case 403:
                    errorCode = "ERR_403";
                    message = "Forbidden. You do not have permission.";
                    break;
                case 404:
                    errorCode = "ERR_404";
                    message = "Resource not found.";
                    break;
                case 500:
                    errorCode = "ERR_500";
                    message = "Internal Server Error.";
                    break;
            }

            var response = new ApiResponse<object>(statusCode, message, errorCode)
            {
                Data = data
            };

            await WriteResponseAsync(context, response, memoryStream);
        }
        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            ApiResponse<object> response;

            if (exception is DomainExceptions domainEx)
            {
                context.Response.StatusCode = 400;
                response = new ApiResponse<object>(400, domainEx.Message, domainEx.ErrorCode);
            }
            else
            {
                context.Response.StatusCode = 500;
                response = new ApiResponse<object>(500, "Internal Server Error", "ERR_500");
            }

            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            await context.Response.WriteAsync(json);
        }

        private async Task WriteResponseAsync(HttpContext context, object responseObj, MemoryStream memoryStream)
        {
            context.Response.ContentType = "application/json";
            var json = JsonSerializer.Serialize(responseObj, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });

            memoryStream.SetLength(0);
            using var writer = new StreamWriter(memoryStream, leaveOpen: true);
            await writer.WriteAsync(json);
            await writer.FlushAsync();
        }
    }
}