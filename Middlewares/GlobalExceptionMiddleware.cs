using System.Net;
using System.Text.Json;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.Exceptions;

namespace MiPeluqueria.Api.Middlewares
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excepción capturada por GlobalExceptionMiddleware: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var statusCode = exception switch
            {
                NotFoundException or KeyNotFoundException => (int)HttpStatusCode.NotFound,
                UnauthorizedAppException or UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
                BusinessRuleException or InvalidOperationException or ArgumentException => (int)HttpStatusCode.BadRequest,
                _ => (int)HttpStatusCode.InternalServerError
            };

            context.Response.StatusCode = statusCode;

            var message = statusCode == (int)HttpStatusCode.InternalServerError
                ? "Ocurrió un error interno en el servidor. Por favor, intente nuevamente."
                : exception.Message;

            var response = ApiResponse<object>.Fail(
                message,
                new List<string> { exception.Message }
            );

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(response, options);
            return context.Response.WriteAsync(json);
        }
    }
}