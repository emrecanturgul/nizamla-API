// nizamla.API/Middleware/ExceptionHandlingMiddleware.cs
using Microsoft.AspNetCore.Http;
using nizamla.Application.Exceptions;
using Serilog;
using Serilog.Events;
using System.Net;
using System.Text.Json;

namespace nizamla.Api.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionHandlingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                if (ex is HttpException httpEx)
                {
                    var httpJson = JsonSerializer.Serialize(new
                    {
                        statusCode = httpEx.StatusCode,
                        error = httpEx.ClientError,
                        details = httpEx.Message
                    });

                    context.Response.StatusCode = httpEx.StatusCode;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(httpJson);
                    return;
                }

                var (statusCode, clientMessage, logLevel, logText) = ex switch
                {
                    UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Yetkisiz erişim.", LogEventLevel.Warning, "Unauthorized access"),
                    KeyNotFoundException => (HttpStatusCode.NotFound, "Kayıt bulunamadı.", LogEventLevel.Warning, "Record not found"),
                    ArgumentException => (HttpStatusCode.BadRequest, "Geçersiz argüman.", LogEventLevel.Warning, "Invalid argument"),
                    ValidationException => (HttpStatusCode.BadRequest, "Doğrulama hatası.", LogEventLevel.Warning, "Validation failed"),
                    _ => (HttpStatusCode.InternalServerError, "Sunucuda beklenmeyen bir hata oluştu.", LogEventLevel.Error, "Unhandled exception")
                };

                var logDetails = new
                {
                    StatusCode = (int)statusCode,
                    Message = clientMessage,
                    Path = context.Request.Path,
                    Method = context.Request.Method,
                    Query = context.Request.QueryString.ToString(),
                    StackTrace = ex.StackTrace
                };

                Log.Write(logLevel, ex, "{LogText} | {@Details}", logText, logDetails);

                var errorResponse = new
                {
                    statusCode = (int)statusCode,
                    error = clientMessage,
                    details = ex.Message
                };

                var json = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                context.Response.Clear();
                context.Response.StatusCode = (int)statusCode;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(json);
            }
        }
    }
}
