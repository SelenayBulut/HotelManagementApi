using System.Net;
using System.Text.Json;

namespace HotelManagementApi.Middleware
{

    // API'de oluşan beklenmeyen hataları merkezi olarak yakalayıp yönetir.
    public class GlobalExceptionMiddleware
    {

        // İsteği bir sonraki middleware veya Controller'a iletmek için kullanılır.
        private readonly RequestDelegate _next;

        // Oluşan hataları loglamak için kullanılır.
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        // Her HTTP isteğinde çalışan ana middleware metodudur.
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Beklenmeyen bir hata oluştu. " +
                    "Method: {Method}, Path: {Path}",
                    context.Request.Method,
                    context.Request.Path);

                await HandleExceptionAsync(context);
            }
        }

        // Oluşan Exception için kullanıcıya HTTP cevabı hazırlayan metottur.
        private static async Task HandleExceptionAsync(
            HttpContext context)
        {
            context.Response.StatusCode =
                (int)HttpStatusCode.InternalServerError;

            context.Response.ContentType =
                "application/json";

            var response = new
            {
                message = "Sunucu tarafında beklenmeyen bir hata oluştu."
            };

            var json = JsonSerializer.Serialize(response);

            await context.Response.WriteAsync(json);
        }
    }
}