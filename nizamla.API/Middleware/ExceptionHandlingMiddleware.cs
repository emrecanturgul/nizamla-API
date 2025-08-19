using System.Net;
using System.Text.Json;

namespace nizamla.Api.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Beklenmeyen bir hata oluştu");

                context.Response.ContentType = "application/json";

              
                var statusCode = (int)HttpStatusCode.InternalServerError;
                var message = "Sunucuda beklenmeyen bir hata oluştu.";

                if (ex is UnauthorizedAccessException)
                {
                    statusCode = (int)HttpStatusCode.Unauthorized;
                    message = "Yetkisiz erişim.";
                }
                else if (ex is KeyNotFoundException)
                {
                    statusCode = (int)HttpStatusCode.NotFound;
                    message = "Kayıt bulunamadı.";
                }
                else if (ex is ArgumentException)
                {
                    statusCode = (int)HttpStatusCode.BadRequest;
                    message = ex.Message;
                }

                context.Response.StatusCode = statusCode;

                var errorResponse = new
                {
                    statusCode,
                    error = message,
                    details = ex.Message 
                };

                var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                var json = JsonSerializer.Serialize(errorResponse, options);

                await context.Response.WriteAsync(json);
            }
        }
    }
}
