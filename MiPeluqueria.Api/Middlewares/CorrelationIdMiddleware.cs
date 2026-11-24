using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Serilog;
using Serilog.Context;

namespace MiPeluqueria.Api.Middlewares
{
    public class CorrelationIdMiddleware
    {
        public const string HeaderName = "X-Correlation-Id";
        private const int MaxCorrelationIdLength = 64;

        private readonly RequestDelegate _next;

        public CorrelationIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IDiagnosticContext diagnosticContext)
        {
            string correlationId = ResolveOrGenerateCorrelationId(context);

            context.TraceIdentifier = correlationId;
            context.Items[HeaderName] = correlationId;
            context.Response.Headers[HeaderName] = correlationId;

            context.Response.OnStarting(() =>
            {
                context.Response.Headers[HeaderName] = correlationId;
                return Task.CompletedTask;
            });

            diagnosticContext.Set("CorrelationId", correlationId);

            using (LogContext.PushProperty("CorrelationId", correlationId))
            {
                await _next(context);
            }
        }

        private static string ResolveOrGenerateCorrelationId(HttpContext context)
        {
            if (context.Request.Headers.TryGetValue(HeaderName, out var values))
            {
                string candidate = values.ToString().Trim();
                if (IsValidCorrelationId(candidate))
                {
                    return candidate;
                }
            }

            return Guid.NewGuid().ToString("D");
        }

        private static bool IsValidCorrelationId(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > MaxCorrelationIdLength)
                return false;

            foreach (char c in value)
            {
                if (!char.IsLetterOrDigit(c) && c != '-' && c != '_' && c != '.')
                    return false;
            }

            return true;
        }
    }
}
