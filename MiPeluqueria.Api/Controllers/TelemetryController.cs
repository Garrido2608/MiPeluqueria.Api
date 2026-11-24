using System;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using MiPeluqueria.Api.DTOs.Common;
using Serilog.Context;

namespace MiPeluqueria.Api.Controllers
{
    [ApiController]
    [Route("api/telemetry")]
    [Produces("application/json")]
    [AllowAnonymous]
    [EnableRateLimiting("ClientTelemetryPolicy")]
    public class TelemetryController : ControllerBase
    {
        private const int MaxPayloadBytes = 4096;
        private const int MaxMessageChars = 500;
        private const int MaxStackChars = 1500;
        private const int MaxRouteChars = 300;
        private const int MaxComponentStackChars = 800;

        private static readonly Regex JwtPattern = new(
            @"eyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+",
            RegexOptions.Compiled);

        private static readonly Regex EmailPattern = new(
            @"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}",
            RegexOptions.Compiled);

        private static readonly Regex SecretKeyValuePattern = new(
            @"(?i)(password|passwordhash|token|refresh_token|refreshtoken|jwt|secret|authorization|cookie|connectionstring)\s*([:=])\s*([^\s&;,""',}]+)",
            RegexOptions.Compiled);

        private readonly ILogger<TelemetryController> _logger;

        public TelemetryController(ILogger<TelemetryController> logger)
        {
            _logger = logger;
        }

        [HttpPost("client-errors")]
        [RequestSizeLimit(MaxPayloadBytes)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status202Accepted)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public IActionResult ReportClientError([FromBody] ClientErrorReportDto? dto)
        {
            if (Request.ContentLength is > MaxPayloadBytes)
            {
                return BadRequest(ApiResponse<object>.Fail(
                    "El reporte de telemetría excede el límite máximo permitido de 4 KB."));
            }

            if (dto == null || string.IsNullOrWhiteSpace(dto.Message))
            {
                return BadRequest(ApiResponse<object>.Fail(
                    "El mensaje del error de cliente es obligatorio."));
            }

            string correlationId = ResolveSafeCorrelationId(dto.CorrelationId, HttpContext.TraceIdentifier);
            string safeMessage = SanitizeZeroPii(dto.Message, MaxMessageChars) ?? "Sin detalle";
            string safeRoute = SanitizeZeroPii(dto.Route, MaxRouteChars) ?? "/";
            string? safeStack = SanitizeZeroPii(dto.Stack, MaxStackChars);
            string? safeComponentStack = SanitizeZeroPii(dto.ComponentStack, MaxComponentStackChars);

            bool isWarning = string.Equals(dto.Severity, "warning", StringComparison.OrdinalIgnoreCase);

            using (LogContext.PushProperty("Source", "Frontend"))
            using (LogContext.PushProperty("CorrelationId", correlationId))
            using (LogContext.PushProperty("Route", safeRoute))
            using (LogContext.PushProperty("StackTrace", safeStack))
            using (LogContext.PushProperty("ComponentStack", safeComponentStack))
            {
                if (isWarning)
                {
                    _logger.LogWarning(
                        "[FRONTEND_WARN] Source={Source} CorrelationId={CorrelationId} Route={Route} Message={ClientMessage} StackTrace={StackTrace}",
                        "Frontend",
                        correlationId,
                        safeRoute,
                        safeMessage,
                        safeStack);
                }
                else
                {
                    _logger.LogError(
                        "[FRONTEND_ERROR] Source={Source} CorrelationId={CorrelationId} Route={Route} Message={ClientMessage} StackTrace={StackTrace}",
                        "Frontend",
                        correlationId,
                        safeRoute,
                        safeMessage,
                        safeStack);
                }
            }

            var payload = ApiResponse<object>.Ok(
                new
                {
                    accepted = true,
                    correlationId,
                    source = "Frontend"
                },
                "Reporte de error registrado.");

            return StatusCode(StatusCodes.Status202Accepted, payload);
        }

        private static string ResolveSafeCorrelationId(string? dtoCorrelationId, string fallbackTraceId)
        {
            if (!string.IsNullOrWhiteSpace(dtoCorrelationId))
            {
                string trimmed = dtoCorrelationId.Trim();
                if (trimmed.Length <= 64)
                {
                    bool valid = true;
                    foreach (char c in trimmed)
                    {
                        if (!char.IsLetterOrDigit(c) && c != '-' && c != '_' && c != '.')
                        {
                            valid = false;
                            break;
                        }
                    }

                    if (valid)
                        return trimmed;
                }
            }

            return string.IsNullOrWhiteSpace(fallbackTraceId)
                ? Guid.NewGuid().ToString("D")
                : fallbackTraceId;
        }

        private static string? SanitizeZeroPii(string? raw, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            string cleaned = raw.Trim();
            cleaned = JwtPattern.Replace(cleaned, "[REDACTED_JWT]");
            cleaned = EmailPattern.Replace(cleaned, "[REDACTED_EMAIL]");
            cleaned = SecretKeyValuePattern.Replace(cleaned, "$1$2[REDACTED]");

            if (cleaned.Length > maxLength)
            {
                cleaned = cleaned[..maxLength] + "...[TRUNCATED]";
            }

            return cleaned;
        }
    }
}
