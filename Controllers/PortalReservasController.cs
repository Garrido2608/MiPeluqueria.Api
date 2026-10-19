using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiPeluqueria.Api.Data.Context;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Turnos;
using MiPeluqueria.Api.Integrations.Payments;
using MiPeluqueria.Api.Models.Clientes;
using MiPeluqueria.Api.Models.Turnos;
using MiPeluqueria.Api.Repositories.Interfaces;
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Controllers
{
    [Route("api/public")]
    [ApiController]
    [AllowAnonymous]
    public class PortalReservasController : ControllerBase
    {
        private readonly IServicioService _servicioService;
        private readonly IPeluqueroService _peluqueroService;
        private readonly ITurnoService _turnoService;
        private readonly IUnitOfWork _uow;
        private readonly ApplicationDbContext _context;
        private readonly IPaymentGatewayService _paymentGateway;

        public PortalReservasController(
            IServicioService servicioService,
            IPeluqueroService peluqueroService,
            ITurnoService turnoService,
            IUnitOfWork uow,
            ApplicationDbContext context,
            IPaymentGatewayService paymentGateway)
        {
            _servicioService = servicioService;
            _peluqueroService = peluqueroService;
            _turnoService = turnoService;
            _uow = uow;
            _context = context;
            _paymentGateway = paymentGateway;
        }

        [HttpGet("catalogo-servicios")]
        public async Task<IActionResult> GetCatalogoServicios()
        {
            var res = await _servicioService.GetAllAsync();
            return Ok(res);
        }

        [HttpGet("peluqueros")]
        public async Task<IActionResult> GetPeluqueros()
        {
            var res = await _peluqueroService.GetAllAsync();
            return Ok(res);
        }

        [HttpGet("disponibilidad")]
        public async Task<IActionResult> GetDisponibilidad(
            [FromQuery] int peluqueroId,
            [FromQuery] DateTime fecha,
            [FromQuery] List<int>? serviciosIds = null)
        {
            var res = await _turnoService.ObtenerHorariosDisponiblesAsync(peluqueroId, fecha, serviciosIds);
            return Ok(res);
        }

        [HttpPost("reservar")]
        public async Task<IActionResult> ReservarTurnoPublico([FromBody] ReservaPublicaRequestDto dto, CancellationToken ct = default)
        {
            string telNorm = dto.TelefonoCliente.Trim();
            string? emailNorm = dto.EmailCliente?.Trim().ToLowerInvariant();

            // 1. Buscar cliente existente por teléfono o email (o registrarlo en el acto)
            var cliente = await _uow.Clientes.GetAllQueryable()
                .FirstOrDefaultAsync(c => c.Telefono == telNorm || (emailNorm != null && c.Email == emailNorm), ct);

            if (cliente == null)
            {
                cliente = new Cliente
                {
                    Nombre = dto.NombreCliente.Trim(),
                    Apellido = dto.ApellidoCliente.Trim(),
                    Telefono = telNorm,
                    Email = emailNorm,
                    FechaNacimiento = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    FechaRegistro = DateTime.UtcNow,
                    Activo = true
                };
                await _uow.Clientes.AddAsync(cliente);
                await _uow.SaveChangesAsync(ct);
            }

            // 2. Crear el turno utilizando el motor transaccional existente
            var crearTurnoDto = new CrearTurnoDto
            {
                ClienteId = cliente.Id,
                PeluqueroId = dto.PeluqueroId,
                FechaHoraInicio = dto.FechaHoraInicio,
                ServiciosIds = dto.ServiciosIds,
                Observaciones = $"[Reserva Online] {dto.Observaciones}".Trim()
            };

            var turnoRes = await _turnoService.CreateAsync(crearTurnoDto, usuarioId: 1);
            if (!turnoRes.Success || turnoRes.Data == null)
                return BadRequest(turnoRes);

            // 3. Verificar si la configuración del salón exige cobro de seña online
            var config = await _context.ConfiguracionSalon.AsNoTracking().FirstOrDefaultAsync(ct);
            decimal porcentajeSena = config?.PorcentajeSenaReserva ?? 0m;
            string monedaIso = config?.MonedaIso ?? "ARS";

            var response = new ReservaPublicaResponseDto
            {
                TurnoId = turnoRes.Data.Id,
                ClienteId = cliente.Id,
                EstadoTurno = turnoRes.Data.EstadoNombre,
                MontoTotalEstimado = turnoRes.Data.MontoTotalEstimado,
                RequiereSena = porcentajeSena > 0
            };

            if (porcentajeSena > 0 && turnoRes.Data.MontoTotalEstimado > 0)
            {
                decimal montoSena = Math.Round(turnoRes.Data.MontoTotalEstimado * (porcentajeSena / 100m), 2);
                var checkout = await _paymentGateway.CrearPreferenciaSenaTurnoAsync(
                    turnoRes.Data.Id,
                    montoSena,
                    monedaIso,
                    cliente.Email ?? "cliente@mipeluqueria.com",
                    dto.PasarelaPreferida,
                    ct);

                response.MontoSena = montoSena;
                response.CheckoutPagoUrl = checkout.CheckoutUrl;
                response.PreferenciaPagoId = checkout.PreferenceId;
            }

            return Ok(ApiResponse<ReservaPublicaResponseDto>.Exito(response, "Reserva registrada correctamente."));
        }
    }

    [Route("api/webhooks/pagos")]
    [ApiController]
    [AllowAnonymous]
    public class WebhooksController : ControllerBase
    {
        private readonly ITurnoService _turnoService;
        private readonly IPaymentGatewayService _paymentGateway;

        public WebhooksController(ITurnoService turnoService, IPaymentGatewayService paymentGateway)
        {
            _turnoService = turnoService;
            _paymentGateway = paymentGateway;
        }

        [HttpPost("{proveedor}")]
        public async Task<IActionResult> RecibirNotificacionPago(
            string proveedor,
            [FromBody] WebhookPagoNotificacionDto payload,
            [FromHeader(Name = "x-signature")] string? signature = null)
        {
            if (!_paymentGateway.ValidarFirmaWebhook(proveedor, signature, payload.ExternalReference ?? string.Empty))
                return Unauthorized(ApiResponse<bool>.Falla("Firma de webhook inválida."));

            if (!string.IsNullOrWhiteSpace(payload.ExternalReference) &&
                payload.ExternalReference.StartsWith("TURNO_", StringComparison.OrdinalIgnoreCase) &&
                (string.Equals(payload.Status, "approved", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(payload.Status, "paid", StringComparison.OrdinalIgnoreCase)))
            {
                string idStr = payload.ExternalReference.Replace("TURNO_", "", StringComparison.OrdinalIgnoreCase);
                if (int.TryParse(idStr, out int turnoId))
                {
                    await _turnoService.CambiarEstadoAsync(turnoId, new CambiarEstadoTurnoDto
                    {
                        NuevoEstadoId = (int)EstadoTurnoEnum.Confirmado,
                        Observaciones = $"Seña acreditada automáticamente vía Webhook ({proveedor})."
                    }, usuarioId: 1);
                }
            }

            return Ok(ApiResponse<bool>.Exito(true, "Webhook procesado de forma idempotente."));
        }
    }
}