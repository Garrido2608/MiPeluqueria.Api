using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.Exceptions;

namespace MiPeluqueria.Api.Middlewares
{
    public class GlobalExceptionMiddleware
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

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
            if (context.Response.HasStarted)
                return Task.CompletedTask;

            context.Response.ContentType = "application/json";

            var (statusCode, message) = exception switch
            {
                NotFoundException or KeyNotFoundException =>
                    ((int)HttpStatusCode.NotFound, exception.Message),
                UnauthorizedAppException or UnauthorizedAccessException =>
                    ((int)HttpStatusCode.Unauthorized, exception.Message),
                ForbiddenAccessException =>
                    ((int)HttpStatusCode.Forbidden, exception.Message),
                DbUpdateConcurrencyException =>
                    ((int)HttpStatusCode.Conflict, "El registro fue modificado simultáneamente por otro usuario. Recargue la pantalla e intente nuevamente."),
                BusinessRuleException or InvalidOperationException or ArgumentException =>
                    ((int)HttpStatusCode.BadRequest, exception.Message),
                _ =>
                    ((int)HttpStatusCode.InternalServerError, "Ocurrió un error interno en el servidor. Por favor, intente nuevamente.")
            };

            context.Response.StatusCode = statusCode;

            var response = ApiResponse<object>.Fail(message);
            var json = JsonSerializer.Serialize(response, JsonOptions);

            return context.Response.WriteAsync(json);
        }
    }
}