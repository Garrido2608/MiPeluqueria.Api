using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace MiPeluqueria.Api.Middlewares
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public GlobalExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception)
            {
                context.Response.StatusCode = 500;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("{\"Success\":false, \"Message\":\"Error interno del servidor.\"}");
            }
        }
    }
}