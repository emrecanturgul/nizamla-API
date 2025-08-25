using Microsoft.AspNetCore.Http;
using nizamla.Application.Exceptions;
using Serilog;
using Serilog.Events;
using System.ComponentModel.DataAnnotations;
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

        private static readonly Dictionary<Type, (HttpStatusCode StatusCode, string Message, LogEventLevel LogLevel, string LogText)> ExceptionMappings = new()
        {
            { typeof(UnauthorizedAccessException), (HttpStatusCode.Unauthorized, "Yetkisiz erişim.", LogEventLevel.Warning, "Unauthorized access") },
            { typeof(KeyNotFoundException), (HttpStatusCode.NotFound, "Kayıt bulunamadı.", LogEventLevel.Warning, "Record not found") },
            { typeof(ArgumentException), (HttpStatusCode.BadRequest, "Geçersiz argüman.", LogEventLevel.Warning, "Invalid argument") },
            { typeof(ValidationException), (HttpStatusCode.BadRequest, "Doğrulama hatası.", LogEventLevel.Warning, "Validation failed") },
            { typeof(BusinessRuleException), (HttpStatusCode.Conflict, "İş kuralı ihlali.", LogEventLevel.Warning, "Business rule violation") }
        };

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                var exceptionType = ex.GetType();
                var (statusCode, clientMessage, logLevel, logText) = ExceptionMappings.ContainsKey(exceptionType)
                    ? ExceptionMappings[exceptionType]
                    : (HttpStatusCode.InternalServerError, "Sunucuda beklenmeyen bir hata oluştu.", LogEventLevel.Error, "Unhandled exception");

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

                context.Response.Clear();
                context.Response.StatusCode = (int)statusCode;
                context.Response.ContentType = "application/json";

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

                await context.Response.WriteAsync(json);
            }
        }
    }

 
}
