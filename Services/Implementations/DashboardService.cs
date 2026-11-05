using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Dashboard;
using MiPeluqueria.Api.Models.Turnos;
using MiPeluqueria.Api.Models.Ventas;
using MiPeluqueria.Api.Repositories.Interfaces;
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Services.Implementations
{
    public class DashboardService : IDashboardService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguracionSalonService _configuracionSalonService;

        public DashboardService(
            IUnitOfWork unitOfWork,
            IConfiguracionSalonService configuracionSalonService)
        {
            _unitOfWork = unitOfWork;
            _configuracionSalonService = configuracionSalonService;
        }

        public async Task<ApiResponse<DashboardKpiResponseDto>> ObtenerMetricasMesActualAsync(CancellationToken ct = default)
        {
            return await ObtenerKpisAsync(null, null, ct);
        }

        public async Task<ApiResponse<DashboardKpiResponseDto>> ObtenerKpisAsync(
            DateTime? fechaDesde = null,
            DateTime? fechaHasta = null,
            CancellationToken ct = default)
        {
            var config = await _configuracionSalonService.ObtenerConfiguracionAsync(ct);
            string timeZoneId = !string.IsNullOrWhiteSpace(config.TimeZoneId)
                ? config.TimeZoneId
                : "America/Argentina/Cordoba";

            TimeZoneInfo tz;
            try
            {
                tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }
            catch
            {
                tz = TimeZoneInfo.CreateCustomTimeZone("AR", TimeSpan.FromHours(-3), "Argentina", "Argentina");
            }

            DateTime ahoraLocalSalon = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);

            var inicio = fechaDesde ?? new DateTime(ahoraLocalSalon.Year, ahoraLocalSalon.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var fin = fechaHasta.HasValue
                ? (fechaHasta.Value.TimeOfDay == TimeSpan.Zero ? fechaHasta.Value.Date.AddDays(1).AddTicks(-1) : fechaHasta.Value)
                : inicio.AddMonths(1).AddTicks(-1);

            var hoyInicio = ahoraLocalSalon.Date;
            var hoyFin = hoyInicio.AddDays(1).AddTicks(-1);

            // 1. Facturación, Ventas y Desglose del Período
            var ventasPeriodo = await _unitOfWork.Dashboard.GetVentasEnRangoAsync(inicio, fin, ct);

            decimal facturacion = ventasPeriodo.Sum(v => v.Total);
            int cantidadVentas = ventasPeriodo.Count;
            decimal ticketPromedio = cantidadVentas > 0 ? Math.Round(facturacion / cantidadVentas, 2) : 0m;

            // 2. Métricas de Turnos, Tasa de Cancelación y Turnos Pendientes Hoy (según TimeZoneInfo del salón)
            var turnosPeriodo = await _unitOfWork.Dashboard.GetTurnosEnRangoAsync(inicio, fin, ct);

            int turnosCompletados = turnosPeriodo.Count(t => t.EstadoId == (int)EstadoTurnoEnum.Completado);
            int turnosCancelados = turnosPeriodo.Count(t => t.EstadoId == (int)EstadoTurnoEnum.Cancelado);
            int totalTurnosPeriodo = turnosPeriodo.Count;

            double tasaCancelacion = totalTurnosPeriodo > 0
                ? Math.Round((double)turnosCancelados / totalTurnosPeriodo * 100.0, 2)
                : 0.0;

            int turnosHoy = await _unitOfWork.Dashboard.CountTurnosPendientesEnRangoAsync(hoyInicio, hoyFin, ct);

            // 3. Servicio más vendido y Ranking de Profesionales (precarga en lote O(1))
            var detallesActivos = ventasPeriodo
                .SelectMany(v => v.DetallesVenta)
                .Where(d => !d.IsDeleted)
                .ToList();

            string nombreServicioTop = "Sin datos";
            var topServicioGroup = detallesActivos
                .Where(d => d.TipoItem == TipoItemVentaEnum.Servicio)
                .GroupBy(d => d.ItemId)
                .Select(g => new { ServicioId = g.Key, Cantidad = g.Sum(x => x.Cantidad) })
                .OrderByDescending(x => x.Cantidad)
                .FirstOrDefault();

            if (topServicioGroup != null)
            {
                var serv = await _unitOfWork.Servicios.GetByIdAsync(topServicioGroup.ServicioId, ct);
                if (serv != null)
                    nombreServicioTop = $"{serv.Nombre} ({topServicioGroup.Cantidad})";
            }

            var rankingRaw = detallesActivos
                .Where(d => d.PeluqueroId.HasValue && d.PeluqueroId.Value > 0)
                .GroupBy(d => d.PeluqueroId!.Value)
                .Select(g => new
                {
                    PeluqueroId = g.Key,
                    ServiciosRealizados = g.Sum(x => x.Cantidad),
                    TotalFacturado = g.Sum(x => x.Subtotal)
                })
                .OrderByDescending(x => x.TotalFacturado)
                .ToList();

            var peluqueroIds = rankingRaw.Select(r => r.PeluqueroId).Distinct().ToList();
            var peluquerosDict = peluqueroIds.Count > 0
                ? await _unitOfWork.Peluqueros.GetByIdsDictionaryAsync(peluqueroIds, tracking: false, ct)
                : new Dictionary<int, Peluquero>();

            var rankingPeluqueros = rankingRaw.Select(r => new RankingPeluqueroDto
            {
                PeluqueroId = r.PeluqueroId,
                NombrePeluquero = peluquerosDict.TryGetValue(r.PeluqueroId, out var p)
                    ? $"{p.Nombre} {p.Apellido}".Trim()
                    : $"Peluquero #{r.PeluqueroId}",
                ServiciosRealizados = r.ServiciosRealizados,
                TotalFacturado = r.TotalFacturado
            }).ToList();

            string nombrePeluqueroTop = rankingPeluqueros.Count > 0
                ? $"{rankingPeluqueros[0].NombrePeluquero} (${rankingPeluqueros[0].TotalFacturado:N2})"
                : "Sin datos";

            // 4. Desglose de Ingresos por Medio de Pago
            var pagosRaw = ventasPeriodo
                .SelectMany(v => v.Pagos)
                .Where(p => !p.IsDeleted)
                .GroupBy(p => p.MedioPago != null && !string.IsNullOrWhiteSpace(p.MedioPago.Nombre) ? p.MedioPago.Nombre : "Otro")
                .Select(g => new IngresoPorMedioPagoDto
                {
                    MedioPago = g.Key,
                    TotalRecaudado = g.Sum(x => x.Importe),
                    CantidadOperaciones = g.Count()
                })
                .OrderByDescending(x => x.TotalRecaudado)
                .ToList();

            // 5. Clientes Nuevos del Período y Productos en Stock Crítico
            int clientesNuevos = await _unitOfWork.Dashboard.CountNuevosClientesEnRangoAsync(inicio, fin, ct);
            int stockCriticoCount = await _unitOfWork.Dashboard.CountProductosStockCriticoAsync(ct);

            var kpi = new DashboardKpiResponseDto
            {
                FechaDesde = inicio,
                FechaHasta = fin,
                FacturacionTotalMes = facturacion,
                CantidadVentasMes = cantidadVentas,
                TotalTurnosAtendidosMes = turnosCompletados,
                TurnosCanceladosPeriodo = turnosCancelados,
                TurnosPendientesHoy = turnosHoy,
                TasaCancelacionPorcentaje = tasaCancelacion,
                TicketPromedio = ticketPromedio,
                ClientesNuevosPeriodo = clientesNuevos,
                ServicioMasVendido = nombreServicioTop,
                PeluqueroEstrella = nombrePeluqueroTop,
                ProductosEnStockCritico = stockCriticoCount,
                IngresosPorMedioPago = pagosRaw,
                RankingPeluqueros = rankingPeluqueros
            };

            return ApiResponse<DashboardKpiResponseDto>.Exito(kpi, "KPIs gerenciales calculados exitosamente.");
        }
    }
}
