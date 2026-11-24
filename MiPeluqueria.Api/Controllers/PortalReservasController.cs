using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using MiPeluqueria.Api.DTOs.Clientes;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Turnos;
using MiPeluqueria.Api.Exceptions;
using MiPeluqueria.Api.Integrations.Payments;
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Controllers
{
    /// <summary>
    /// Habilita el buffering del stream HTTP antes del Model Binding para permitir la lectura
    /// del payload crudo (Raw Body) en la verificación criptográfica HMAC-SHA256 de Webhooks.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    internal sealed class EnableRequestBufferingAttribute : Attribute, IResourceFilter
    {
        public void OnResourceExecuting(ResourceExecutingContext context)
        {
            context.HttpContext.Request.EnableBuffering();
        }

        public void OnResourceExecuted(ResourceExecutedContext context)
        {
        }
    }

    [ApiController]
    [Route("api/public")]
    [Produces("application/json")]
    [AllowAnonymous]
    public class PortalReservasController : ControllerBase
    {
        private readonly ITurnoService _turnoService;
        private readonly IServicioService _servicioService;
        private readonly IPeluqueroService _peluqueroService;
        private readonly IClienteService _clienteService;
        private readonly IConfiguracionSalonService _configuracionSalonService;
        private readonly IPaymentGatewayService _paymentGateway;
        private readonly ILogger<PortalReservasController> _logger;

        public PortalReservasController(
            ITurnoService turnoService,
            IServicioService servicioService,
            IPeluqueroService peluqueroService,
            IClienteService clienteService,
            IConfiguracionSalonService configuracionSalonService,
            IPaymentGatewayService paymentGateway,
            ILogger<PortalReservasController> logger)
        {
            _turnoService = turnoService;
            _servicioService = servicioService;
            _peluqueroService = peluqueroService;
            _clienteService = clienteService;
            _configuracionSalonService = configuracionSalonService;
            _paymentGateway = paymentGateway;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene el catálogo público consolidado de servicios y profesionales activos del salón.
        /// </summary>
        [HttpGet("catalogo")]
        public async Task<IActionResult> ObtenerCatalogoPublico(CancellationToken ct = default)
        {
            var serviciosRes = await _servicioService.GetAllAsync(soloActivos: true, ct);
            var peluquerosRes = await _peluqueroService.GetAllAsync(soloActivos: true, ct);

            return Ok(ApiResponse<object>.Ok(new
            {
                Servicios = serviciosRes.Data ?? Enumerable.Empty<ServicioDto>(),
                Profesionales = peluquerosRes.Data ?? Enumerable.Empty<PeluqueroDto>()
            }, "Catálogo público cargado con éxito."));
        }

        /// <summary>
        /// Obtiene la lista pública de servicios activos del salón.
        /// </summary>
        [HttpGet("catalogo-servicios")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<ServicioDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ObtenerCatalogoServicios(CancellationToken ct = default)
        {
            var res = await _servicioService.GetAllAsync(soloActivos: true, ct);
            return Ok(res);
        }

        /// <summary>
        /// Obtiene la lista pública de profesionales activos del salón.
        /// </summary>
        [HttpGet("peluqueros")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<PeluqueroDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ObtenerPeluquerosPublicos(CancellationToken ct = default)
        {
            var res = await _peluqueroService.GetAllAsync(soloActivos: true, ct);
            return Ok(res);
        }

        /// <summary>
        /// Consulta los slots disponibles en tiempo real para el wizard de reserva web.
        /// </summary>
        [HttpGet("disponibilidad")]
        [ProducesResponseType(typeof(ApiResponse<DisponibilidadHorarioResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ConsultarDisponibilidadPublica(
            [FromQuery] int peluqueroId,
            [FromQuery] DateTime fecha,
            [FromQuery] List<int>? serviciosIds = null,
            CancellationToken ct = default)
        {
            var dispRes = await _turnoService.ObtenerHorariosDisponiblesAsync(
                peluqueroId,
                fecha,
                serviciosIds,
                ct);

            return dispRes.Success ? Ok(dispRes) : BadRequest(dispRes);
        }

        /// <summary>
        /// Crea una reserva online identificando o dando de alta al cliente mediante IClienteService
        /// y generando el link de pago de seña si la política del salón lo requiere.
        /// </summary>
        [HttpPost("reservar")]
        [ProducesResponseType(typeof(ApiResponse<ReservaPublicaResponseDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<ReservaPublicaResponseDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ReservarTurnoPublico(
            [FromBody] ReservaPublicaRequestDto dto,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(dto.NombreCliente) || string.IsNullOrWhiteSpace(dto.TelefonoCliente))
                throw new BusinessRuleException("El nombre y teléfono de contacto son obligatorios para reservar.");

            string telNorm = dto.TelefonoCliente.Trim();
            string? emailNorm = string.IsNullOrWhiteSpace(dto.EmailCliente)
                ? null
                : dto.EmailCliente.Trim().ToLowerInvariant();

            // 1. Buscar cliente existente por teléfono o email a través de IClienteService
            int clienteId = 0;
            var busquedaTel = await _clienteService.ObtenerTodosAsync(
                new PaginationFilterDto { PageNumber = 1, PageSize = 50, Search = telNorm },
                ct);

            var clienteExistente = busquedaTel.Data?.Data?.FirstOrDefault(c =>
                string.Equals(c.Telefono?.Trim(), telNorm, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrWhiteSpace(emailNorm) && string.Equals(c.Email?.Trim(), emailNorm, StringComparison.OrdinalIgnoreCase)));

            if (clienteExistente == null && !string.IsNullOrWhiteSpace(emailNorm))
            {
                var busquedaEmail = await _clienteService.ObtenerTodosAsync(
                    new PaginationFilterDto { PageNumber = 1, PageSize = 50, Search = emailNorm },
                    ct);

                clienteExistente = busquedaEmail.Data?.Data?.FirstOrDefault(c =>
                    string.Equals(c.Email?.Trim(), emailNorm, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(c.Telefono?.Trim(), telNorm, StringComparison.OrdinalIgnoreCase));
            }

            if (clienteExistente != null)
            {
                clienteId = clienteExistente.Id;
            }
            else
            {
                var nuevoClienteRes = await _clienteService.CrearClienteAsync(new ClienteRequestDto
                {
                    Nombre = dto.NombreCliente.Trim(),
                    Apellido = string.IsNullOrWhiteSpace(dto.ApellidoCliente) ? "-" : dto.ApellidoCliente.Trim(),
                    Telefono = telNorm,
                    Email = emailNorm,
                    FechaNacimiento = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                });

                if (!nuevoClienteRes.Success || nuevoClienteRes.Data == null)
                    return BadRequest(ApiResponse<ReservaPublicaResponseDto>.Fail(
                        nuevoClienteRes.Message ?? "No se pudo registrar el cliente para la reserva online.",
                        nuevoClienteRes.Errors));

                clienteId = nuevoClienteRes.Data.Id;
            }

            // 2. Prefijar Observaciones con "[Reserva Online]" para conciliar con RecordatorioTurnosBackgroundService
            string observacionesNormalizadas = string.IsNullOrWhiteSpace(dto.Observaciones)
                ? "[Reserva Online]"
                : dto.Observaciones.Trim().StartsWith("[Reserva Online]", StringComparison.OrdinalIgnoreCase)
                    ? dto.Observaciones.Trim()
                    : $"[Reserva Online] {dto.Observaciones.Trim()}";

            var crearTurnoDto = new CrearTurnoDto
            {
                ClienteId = clienteId,
                PeluqueroId = dto.PeluqueroId,
                FechaHoraInicio = dto.FechaHoraInicio,
                ServiciosIds = dto.ServiciosIds,
                Observaciones = observacionesNormalizadas
            };

            var turnoRes = await _turnoService.CreateAsync(crearTurnoDto, 1, ct);
            if (!turnoRes.Success || turnoRes.Data == null)
            {
                return BadRequest(ApiResponse<ReservaPublicaResponseDto>.Fail(
                    turnoRes.Message ?? "No se pudo registrar la reserva online.",
                    turnoRes.Errors));
            }

            var turnoCreado = turnoRes.Data;
            var config = await _configuracionSalonService.ObtenerConfiguracionAsync(ct);

            string? checkoutUrl = null;
            string? preferenceId = null;

            if (turnoCreado.RequiereSena && turnoCreado.MontoSena > 0m)
            {
                var prefResult = await _paymentGateway.CrearPreferenciaSenaTurnoAsync(
                    turnoCreado.Id,
                    turnoCreado.MontoSena,
                    string.IsNullOrWhiteSpace(config.MonedaIso) ? "ARS" : config.MonedaIso,
                    emailNorm ?? "cliente@mipeluqueria.com",
                    string.IsNullOrWhiteSpace(dto.PasarelaPreferida) ? "MercadoPago" : dto.PasarelaPreferida,
                    ct);

                if (prefResult.Success)
                {
                    checkoutUrl = prefResult.CheckoutUrl;
                    preferenceId = prefResult.PreferenceId;
                }
            }

            var respuesta = new ReservaPublicaResponseDto
            {
                TurnoId = turnoCreado.Id,
                ClienteId = clienteId,
                EstadoTurno = turnoCreado.EstadoNombre,
                MontoTotalEstimado = turnoCreado.MontoTotalEstimado,
                RequiereSena = turnoCreado.RequiereSena,
                MontoSena = turnoCreado.MontoSena,
                CheckoutPagoUrl = checkoutUrl,
                PreferenciaPagoId = preferenceId
            };

            string msg = turnoCreado.RequiereSena
                ? $"Turno pre-reservado. Aboná la seña de ${turnoCreado.MontoSena:N2} en los próximos 30 minutos para confirmar tu lugar."
                : "Turno reservado exitosamente. ¡Te esperamos!";

            return StatusCode(StatusCodes.Status201Created, ApiResponse<ReservaPublicaResponseDto>.Ok(respuesta, msg));
        }
    }

    // =========================================================================
    // CONTROLADOR DE WEBHOOKS DE PASARELAS DE PAGO (MERCADO PAGO / STRIPE)
    // =========================================================================

    [ApiController]
    [Route("api/webhooks/pagos")]
    [AllowAnonymous]
    [EnableRequestBuffering]
    public class WebhooksController : ControllerBase
    {
        private readonly ITurnoService _turnoService;
        private readonly IPaymentGatewayService _paymentGateway;
        private readonly ILogger<WebhooksController> _logger;

        public WebhooksController(
            ITurnoService turnoService,
            IPaymentGatewayService paymentGateway,
            ILogger<WebhooksController> logger)
        {
            _turnoService = turnoService;
            _paymentGateway = paymentGateway;
            _logger = logger;
        }

        [HttpPost("{proveedor}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RecibirNotificacionPago(
            string proveedor,
            [FromBody] WebhookPagoNotificacionDto payload,
            [FromHeader(Name = "x-signature")] string? signature = null,
            CancellationToken ct = default)
        {
            if (payload == null || string.IsNullOrWhiteSpace(payload.ExternalReference))
                return BadRequest(ApiResponse<bool>.Fail("Payload de webhook inválido."));

            if (string.IsNullOrWhiteSpace(signature) && Request.Headers.TryGetValue("X-Webhook-Signature", out var altSig))
                signature = altSig.ToString();

            string rawBody = string.Empty;
            if (Request.Body.CanSeek)
            {
                Request.Body.Position = 0;
                using var reader = new StreamReader(
                    Request.Body,
                    Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: false,
                    bufferSize: 1024,
                    leaveOpen: true);
                rawBody = await reader.ReadToEndAsync(ct);
                Request.Body.Position = 0;
            }

            string bodyParaFirma = !string.IsNullOrWhiteSpace(rawBody) ? rawBody : payload.ExternalReference;
            bool firmaValida = _paymentGateway.ValidarFirmaWebhook(proveedor, signature ?? string.Empty, bodyParaFirma);

            if (!firmaValida && !string.IsNullOrWhiteSpace(rawBody))
            {
                // Fallback si el emisor firmó directamente el manifiesto ExternalReference
                firmaValida = _paymentGateway.ValidarFirmaWebhook(proveedor, signature ?? string.Empty, payload.ExternalReference);
            }

            if (!firmaValida)
            {
                _logger.LogWarning("Firma de webhook inválida rechazada para proveedor {Proveedor}.", proveedor);
                return Unauthorized(ApiResponse<bool>.Fail("Firma criptográfica de webhook inválida."));
            }

            // Extraer TurnoId desde "TURNO-123" o "TURNO_123"
            string refLimpia = payload.ExternalReference
                .Replace("TURNO-", "", StringComparison.OrdinalIgnoreCase)
                .Replace("TURNO_", "", StringComparison.OrdinalIgnoreCase)
                .Trim();

            if (!int.TryParse(refLimpia, out int turnoId))
                return BadRequest(ApiResponse<bool>.Fail("Referencia externa de turno no reconocida."));

            var res = await _turnoService.ProcesarPagoSenaWebhookAsync(turnoId, payload, proveedor, ct);
            return res.Success ? Ok(res) : BadRequest(res);
        }
    }
}
