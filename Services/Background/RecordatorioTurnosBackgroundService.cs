using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MiPeluqueria.Api.DTOs.Turnos;
using MiPeluqueria.Api.Integrations.Notifications;
using MiPeluqueria.Api.Models.Turnos;
using MiPeluqueria.Api.Repositories.Interfaces;
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Services.Background
{
    public class RecordatorioTurnosBackgroundService : BackgroundService
    {
        private const int FallbackIntervaloMinutos = 15;
        private const int FallbackMinutosExpiracionSena = 30;
        private const string TagRecordatorio24H = "[WA-24H-OK]";
        private const string TagRecordatorio2H = "[WA-2H-OK]";

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RecordatorioTurnosBackgroundService> _logger;

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
            _logger.LogInformation("[BACKGROUND WORKER] Motor Multi-Tenant de Recordatorios y Expiración de Señas iniciado.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcesarCicloMultiTenantAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[BACKGROUND WORKER] Error ejecutando ciclo global de recordatorios Multi-Tenant.");
                }

                var intervalo = ObtenerIntervaloEjecucion();
                try
                {
                    await Task.Delay(intervalo, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }

        private async Task ProcesarCicloMultiTenantAsync(CancellationToken ct)
        {
            List<(int TenantId, int SucursalId)> sucursalesActivas;

            using (var discoveryScope = _scopeFactory.CreateScope())
            {
                var discoveryUow = discoveryScope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                var sucursales = await discoveryUow.SaaS.AsQueryable()
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(s => s.Activa && !s.IsDeleted && s.Tenant != null && s.Tenant.Activo && !s.Tenant.IsDeleted)
                    .OrderBy(s => s.TenantId)
                    .ThenBy(s => s.Id)
                    .Select(s => new { s.TenantId, s.Id })
                    .ToListAsync(ct);

                sucursalesActivas = sucursales
                    .Select(s => (s.TenantId, s.Id))
                    .ToList();
            }

            foreach (var (tenantId, sucursalId) in sucursalesActivas)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    await ProcesarSucursalAsync(tenantId, sucursalId, ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "[BACKGROUND WORKER] Error procesando ciclo de turnos para Tenant #{TenantId}, Sucursal #{SucursalId}.",
                        tenantId,
                        sucursalId);
                }
            }
        }

        private async Task ProcesarSucursalAsync(int tenantId, int sucursalId, CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();

            var tenantProvider = scope.ServiceProvider.GetRequiredService<ITenantProvider>();
            tenantProvider.SetTenantContext(tenantId, sucursalId);

            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var configService = scope.ServiceProvider.GetRequiredService<IConfiguracionSalonService>();
            var turnoService = scope.ServiceProvider.GetRequiredService<ITurnoService>();
            var whatsAppService = scope.ServiceProvider.GetRequiredService<IWhatsAppService>();

            var config = await configService.ObtenerConfiguracionAsync(ct);
            bool whatsAppActivo = config.NotificacionesWhatsAppActivas;

            // Fallback conservador: si se borra la clave, NO se cancelan reservas de clientes.
            bool cancelarImpagosActivo = _configuration.GetValue<bool?>("BackgroundJobs:CancelarReservasOnlineImpagas") ?? false;
            int minutosExpiracionSena = ObtenerMinutosExpiracionSena();

            var ahoraUtc = DateTime.UtcNow;

            // =========================================================================
            // 1. EXPIRACIÓN AUTOMÁTICA DE RESERVAS ONLINE CON SEÑA IMPAGA
            // =========================================================================
            if (cancelarImpagosActivo)
            {
                var limiteExpiracionSena = ahoraUtc.AddMinutes(-minutosExpiracionSena);

                var turnosImpagosVencidos = await uow.Turnos.AsQueryable()
                    .AsNoTracking()
                    // Solo reservas del portal público: TurnoService marca RequiereSena también en turnos
                    // cargados por el staff, que jamás deben expirar automáticamente.
                    .Where(t => t.EstadoId == (int)EstadoTurnoEnum.Pendiente
                             && t.RequiereSena
                             && !t.SenaPagada
                             && t.Observaciones != null
                             && t.Observaciones.StartsWith("[Reserva Online]")
                             && t.CreatedAt <= limiteExpiracionSena)
                    .Select(t => t.Id)
                    .ToListAsync(ct);

                if (turnosImpagosVencidos.Count > 0)
                {
                    int motivoCancelacionId = await ObtenerOCrearMotivoExpiracionAsync(uow, ct);

                    foreach (var turnoId in turnosImpagosVencidos)
                    {
                        var resCancelacion = await turnoService.CambiarEstadoAsync(
                            turnoId,
                            new CambiarEstadoTurnoDto
                            {
                                NuevoEstadoId = (int)EstadoTurnoEnum.Cancelado,
                                MotivoCancelacionId = motivoCancelacionId,
                                Observaciones = $"Cancelación automática: seña online no abonada en el plazo de {minutosExpiracionSena} minutos."
                            },
                            usuarioId: 1,
                            rolEjecutor: "Sistema",
                            ct: ct);

                        if (resCancelacion.Success)
                        {
                            _logger.LogInformation(
                                "[BACKGROUND WORKER] Turno #{TurnoId} (T:{TenantId}/S:{SucursalId}) liberado por falta de pago de seña.",
                                turnoId,
                                tenantId,
                                sucursalId);
                        }
                    }
                }
            }

            if (!whatsAppActivo)
                return;

            var ahoraSalon = ObtenerHoraLocalSalon(config.TimeZoneId);
            bool huboCambiosRecordatorios = false;

            // =========================================================================
            // 2. RECORDATORIO 24 HORAS ANTES (Entre 2h y 24h previas al turno en hora local)
            // =========================================================================
            var desde24h = ahoraSalon.AddHours(2);
            var hasta24h = ahoraSalon.AddHours(24);

            var turnosParaAviso24h = await uow.Turnos.AsQueryable()
                .Include(t => t.Cliente)
                .Include(t => t.Peluquero)
                .Where(t => (t.EstadoId == (int)EstadoTurnoEnum.Confirmado || t.EstadoId == (int)EstadoTurnoEnum.Pendiente)
                         && t.FechaHoraInicio > desde24h
                         && t.FechaHoraInicio <= hasta24h
                         && (t.Observaciones == null || !t.Observaciones.Contains(TagRecordatorio24H)))
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
                    turno.Observaciones = TruncarConTag(turno.Observaciones, TagRecordatorio24H, 500);
                    uow.Turnos.Update(turno);
                    huboCambiosRecordatorios = true;

                    _logger.LogInformation(
                        "[WHATSAPP 24H] Recordatorio de 24hs enviado para Turno #{TurnoId} (T:{TenantId}/S:{SucursalId}).",
                        turno.Id,
                        tenantId,
                        sucursalId);
                }
            }

            // =========================================================================
            // 3. RECORDATORIO 2 HORAS ANTES (Entre 10 min y 2h previas al turno en hora local)
            // =========================================================================
            var desde2h = ahoraSalon.AddMinutes(10);
            var hasta2h = ahoraSalon.AddHours(2);

            var turnosParaAviso2h = await uow.Turnos.AsQueryable()
                .Include(t => t.Cliente)
                .Include(t => t.Peluquero)
                .Where(t => (t.EstadoId == (int)EstadoTurnoEnum.Confirmado || t.EstadoId == (int)EstadoTurnoEnum.Pendiente)
                         && t.FechaHoraInicio >= desde2h
                         && t.FechaHoraInicio <= hasta2h
                         && (t.Observaciones == null || !t.Observaciones.Contains(TagRecordatorio2H)))
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

                string mensaje2Horas =
                    $"¡Hola {nombreCliente}! Te recordamos que hoy a las {horaTexto} hs (en menos de 2 horas) tenés tu turno con {nombrePeluquero}. ¡Te esperamos!";

                bool enviado = await whatsAppService.EnviarMensajeAsync(turno.Cliente.Telefono, mensaje2Horas);

                if (enviado)
                {
                    turno.Observaciones = TruncarConTag(turno.Observaciones, TagRecordatorio2H, 500);
                    uow.Turnos.Update(turno);
                    huboCambiosRecordatorios = true;

                    _logger.LogInformation(
                        "[WHATSAPP 2H] Recordatorio de 2hs enviado para Turno #{TurnoId} (T:{TenantId}/S:{SucursalId}).",
                        turno.Id,
                        tenantId,
                        sucursalId);
                }
            }

            if (huboCambiosRecordatorios)
            {
                await uow.SaveChangesAsync(ct);
            }
        }

        private TimeSpan ObtenerIntervaloEjecucion()
        {
            int minutosConfig = _configuration.GetValue<int?>("BackgroundJobs:RecordatoriosIntervaloMinutos")
                ?? FallbackIntervaloMinutos;
            int minutosSeguros = Math.Max(1, minutosConfig);
            return TimeSpan.FromMinutes(minutosSeguros);
        }

        private int ObtenerMinutosExpiracionSena()
        {
            int minutosConfig = _configuration.GetValue<int?>("BackgroundJobs:MinutosExpiracionSenaOnline")
                ?? FallbackMinutosExpiracionSena;
            return Math.Max(1, minutosConfig);
        }

        private static async Task<int> ObtenerOCrearMotivoExpiracionAsync(IUnitOfWork uow, CancellationToken ct)
        {
            var motivos = await uow.Catalogos.GetMotivosCancelacionAsync(ct);
            var existente = motivos.FirstOrDefault();
            if (existente != null)
            {
                return existente.Id;
            }

            var nuevoMotivo = new MotivoCancelacion
            {
                Descripcion = "Expiración automática de seña online"
            };

            await uow.Catalogos.AddEntityAsync(nuevoMotivo, ct);
            await uow.SaveChangesAsync(ct);
            return nuevoMotivo.Id;
        }

        private static DateTime ObtenerHoraLocalSalon(string? timeZoneId)
        {
            TimeZoneInfo tz;
            try
            {
                tz = TimeZoneInfo.FindSystemTimeZoneById(
                    string.IsNullOrWhiteSpace(timeZoneId) ? "America/Argentina/Cordoba" : timeZoneId);
            }
            catch
            {
                tz = TimeZoneInfo.CreateCustomTimeZone("AR", TimeSpan.FromHours(-3), "Argentina", "Argentina");
            }

            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
        }

        private static string TruncarConTag(string? observacionesActuales, string tag, int maxLength)
        {
            string baseText = string.IsNullOrWhiteSpace(observacionesActuales)
                ? string.Empty
                : observacionesActuales.Trim();

            if (baseText.Length == 0)
                return tag;

            int maxBaseLength = Math.Max(0, maxLength - tag.Length - 1);
            if (baseText.Length > maxBaseLength)
            {
                baseText = baseText[..maxBaseLength].TrimEnd();
            }

            return $"{baseText} {tag}";
        }
    }
}