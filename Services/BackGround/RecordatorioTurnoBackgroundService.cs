using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MiPeluqueria.Api.Data.Context;
using MiPeluqueria.Api.DTOs.Turnos;
using MiPeluqueria.Api.Integrations.Notifications;
using MiPeluqueria.Api.Models.Turnos;
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Services.Background
{
    public class RecordatorioTurnosBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RecordatorioTurnosBackgroundService> _logger;

        // Corre cada 15 minutos para tener precisión exacta en el aviso de 2 horas antes
        private static readonly TimeSpan IntervaloEjecucion = TimeSpan.FromMinutes(15);

        public RecordatorioTurnosBackgroundService(
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration,
            ILogger<RecordatorioTurnosBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[BACKGROUND WORKER] Motor de Recordatorios WhatsApp (24h y 2h) iniciado.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcesarCicloTurnosAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[BACKGROUND WORKER] Error ejecutando ciclo automático de recordatorios.");
                }

                await Task.Delay(IntervaloEjecucion, stoppingToken);
            }
        }

        private async Task ProcesarCicloTurnosAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var turnoService = scope.ServiceProvider.GetRequiredService<ITurnoService>();
            var whatsAppService = scope.ServiceProvider.GetRequiredService<IWhatsAppService>();

            var config = await context.ConfiguracionSalon.AsNoTracking().FirstOrDefaultAsync(ct);
            bool whatsAppActivo = config?.NotificacionesWhatsAppActivas ?? true;

            // Por defecto FALSE: permite reservar online sin pagar por adelantado.
            bool cancelarImpagosActivo = _configuration.GetValue<bool>("BackgroundJobs:CancelarReservasOnlineImpagas", false);
            bool salonExigeSena = (config?.PorcentajeSenaReserva ?? 0m) > 0m;

            var ahora = DateTime.UtcNow;

            // =========================================================================
            // 1. OPCIONAL (APAGADO POR DEFECTO): Liberar reservas web si se exige seña
            // =========================================================================
            if (cancelarImpagosActivo && salonExigeSena)
            {
                var limiteExpiracionSena = ahora.AddMinutes(-30);

                var turnosImpagosVencidos = await context.Turnos
                    .AsNoTracking()
                    .Where(t => t.EstadoId == (int)EstadoTurnoEnum.Pendiente
                             && t.CreatedAt <= limiteExpiracionSena
                             && t.Observaciones != null
                             && t.Observaciones.StartsWith("[Reserva Online]"))
                    .Select(t => t.Id)
                    .ToListAsync(ct);

                foreach (var turnoId in turnosImpagosVencidos)
                {
                    await turnoService.CambiarEstadoAsync(turnoId, new CambiarEstadoTurnoDto
                    {
                        NuevoEstadoId = (int)EstadoTurnoEnum.Cancelado,
                        MotivoCancelacionId = 1,
                        Observaciones = "Cancelación automática: seña online no abonada en tiempo límite."
                    }, usuarioId: 1);

                    _logger.LogInformation("[BACKGROUND WORKER] Turno #{TurnoId} liberado por falta de pago de seña.", turnoId);
                }
            }

            if (!whatsAppActivo)
                return;

            // =========================================================================
            // 2. RECORDATORIO 24 HORAS ANTES (Entre 2h y 24h previas al turno)
            // =========================================================================
            const string tag24h = "[WA-24H-OK]";
            var desde24h = ahora.AddHours(2);
            var hasta24h = ahora.AddHours(24);

            var turnosParaAviso24h = await context.Turnos
                .Include(t => t.Cliente)
                .Include(t => t.Peluquero)
                .Where(t => (t.EstadoId == (int)EstadoTurnoEnum.Confirmado || t.EstadoId == (int)EstadoTurnoEnum.Pendiente)
                         && t.FechaHoraInicio > desde24h
                         && t.FechaHoraInicio <= hasta24h
                         && (t.Observaciones == null || !t.Observaciones.Contains(tag24h)))
                .ToListAsync(ct);

            foreach (var turno in turnosParaAviso24h)
            {
                if (turno.Cliente == null || string.IsNullOrWhiteSpace(turno.Cliente.Telefono))
                    continue;

                string nombreCliente = $"{turno.Cliente.Nombre} {turno.Cliente.Apellido}".Trim();
                string nombrePeluquero = turno.Peluquero != null
                    ? $"{turno.Peluquero.Nombre} {turno.Peluquero.Apellido}".Trim()
                    : "nuestro equipo";
                string fechaHoraTexto = turno.FechaHoraInicio.ToString("dd/MM/yyyy HH:mm");

                bool enviado = await whatsAppService.EnviarRecordatorioTurnoAsync(
                    turno.Cliente.Telefono,
                    nombreCliente,
                    fechaHoraTexto,
                    nombrePeluquero);

                if (enviado)
                {
                    turno.Observaciones = $"{turno.Observaciones} {tag24h}".Trim();
                    context.Turnos.Update(turno);
                    _logger.LogInformation("[WHATSAPP 24H] Recordatorio de 24hs enviado para Turno #{TurnoId}.", turno.Id);
                }
            }

            // =========================================================================
            // 3. RECORDATORIO 2 HORAS ANTES (Entre 10 min y 2h previas al turno)
            // =========================================================================
            const string tag2h = "[WA-2H-OK]";
            var desde2h = ahora.AddMinutes(10);
            var hasta2h = ahora.AddHours(2);

            var turnosParaAviso2h = await context.Turnos
                .Include(t => t.Cliente)
                .Include(t => t.Peluquero)
                .Where(t => (t.EstadoId == (int)EstadoTurnoEnum.Confirmado || t.EstadoId == (int)EstadoTurnoEnum.Pendiente)
                         && t.FechaHoraInicio >= desde2h
                         && t.FechaHoraInicio <= hasta2h
                         && (t.Observaciones == null || !t.Observaciones.Contains(tag2h)))
                .ToListAsync(ct);

            foreach (var turno in turnosParaAviso2h)
            {
                if (turno.Cliente == null || string.IsNullOrWhiteSpace(turno.Cliente.Telefono))
                    continue;

                string nombreCliente = $"{turno.Cliente.Nombre} {turno.Cliente.Apellido}".Trim();
                string nombrePeluquero = turno.Peluquero != null
                    ? $"{turno.Peluquero.Nombre} {turno.Peluquero.Apellido}".Trim()
                    : "nuestro equipo";
                string horaTexto = turno.FechaHoraInicio.ToString("HH:mm");

                string mensaje2Horas = $"¡Hola {nombreCliente}! Te recordamos que hoy a las {horaTexto} hs (en menos de 2 horas) tenés tu turno con {nombrePeluquero}. ¡Te esperamos!";

                bool enviado = await whatsAppService.EnviarMensajeAsync(turno.Cliente.Telefono, mensaje2Horas);

                if (enviado)
                {
                    turno.Observaciones = $"{turno.Observaciones} {tag2h}".Trim();
                    context.Turnos.Update(turno);
                    _logger.LogInformation("[WHATSAPP 2H] Recordatorio de 2hs enviado para Turno #{TurnoId}.", turno.Id);
                }
            }

            if (turnosParaAviso24h.Count > 0 || turnosParaAviso2h.Count > 0)
            {
                await context.SaveChangesAsync(ct);
            }
        }
    }
}