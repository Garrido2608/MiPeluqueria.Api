using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MiPeluqueria.Api.Data.Context;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Dashboard;
using MiPeluqueria.Api.Models.Turnos;
using MiPeluqueria.Api.Models.Ventas;
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Services.Implementations
{
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;

        public DashboardService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<DashboardKpiResponseDto>> ObtenerMetricasMesActualAsync()
        {
            return await ObtenerKpisAsync(null, null);
        }

        public async Task<ApiResponse<DashboardKpiResponseDto>> ObtenerKpisAsync(DateTime? fechaDesde = null, DateTime? fechaHasta = null)
        {
            var ahora = DateTime.UtcNow;
            var inicio = fechaDesde ?? new DateTime(ahora.Year, ahora.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var fin = fechaHasta ?? inicio.AddMonths(1).AddTicks(-1);
            var hoyInicio = ahora.Date;
            var hoyFin = hoyInicio.AddDays(1).AddTicks(-1);

            // 1. Facturación y Ventas del período
            var ventasPeriodo = await _context.Ventas
                .AsNoTracking()
                .Where(v => v.Fecha >= inicio && v.Fecha <= fin)
                .ToListAsync();

            decimal facturacion = ventasPeriodo.Sum(v => v.Total);
            int cantidadVentas = ventasPeriodo.Count;
            decimal ticketPromedio = cantidadVentas > 0 ? Math.Round(facturacion / cantidadVentas, 2) : 0m;

            // 2. Métricas de Turnos y Tasa de Cancelación
            int turnosCompletados = await _context.Turnos
                .CountAsync(t => t.FechaHoraInicio >= inicio && t.FechaHoraInicio <= fin && t.EstadoId == (int)EstadoTurnoEnum.Completado);

            int turnosCancelados = await _context.Turnos
                .CountAsync(t => t.FechaHoraInicio >= inicio && t.FechaHoraInicio <= fin && t.EstadoId == (int)EstadoTurnoEnum.Cancelado);

            int totalTurnosPeriodo = await _context.Turnos
                .CountAsync(t => t.FechaHoraInicio >= inicio && t.FechaHoraInicio <= fin);

            double tasaCancelacion = totalTurnosPeriodo > 0
                ? Math.Round((double)turnosCancelados / totalTurnosPeriodo * 100.0, 2)
                : 0.0;

            int turnosHoy = await _context.Turnos
                .CountAsync(t => t.FechaHoraInicio >= hoyInicio && t.FechaHoraInicio <= hoyFin
                              && (t.EstadoId == (int)EstadoTurnoEnum.Pendiente || t.EstadoId == (int)EstadoTurnoEnum.Confirmado));

            // 3. Servicio más vendido en el período
            string nombreServicioTop = "Sin datos";
            var topServicioGroup = await _context.DetallesVenta
                .Include(d => d.Venta)
                .Where(d => d.Venta != null && d.Venta.Fecha >= inicio && d.Venta.Fecha <= fin && d.TipoItem == TipoItemVentaEnum.Servicio)
                .GroupBy(d => d.ItemId)
                .Select(g => new { ServicioId = g.Key, Cantidad = g.Sum(x => x.Cantidad) })
                .OrderByDescending(x => x.Cantidad)
                .FirstOrDefaultAsync();

            if (topServicioGroup != null)
            {
                var serv = await _context.Servicios.AsNoTracking().FirstOrDefaultAsync(s => s.Id == topServicioGroup.ServicioId);
                if (serv != null)
                    nombreServicioTop = $"{serv.Nombre} ({topServicioGroup.Cantidad})";
            }

            // 4. Ranking de Peluqueros y Peluquero Estrella
            var rankingRaw = await _context.DetallesVenta
                .Include(d => d.Venta)
                .Where(d => d.Venta != null && d.Venta.Fecha >= inicio && d.Venta.Fecha <= fin && d.PeluqueroId.HasValue)
                .GroupBy(d => d.PeluqueroId!.Value)
                .Select(g => new
                {
                    PeluqueroId = g.Key,
                    ServiciosRealizados = g.Sum(x => x.Cantidad),
                    TotalFacturado = g.Sum(x => x.Subtotal)
                })
                .OrderByDescending(x => x.TotalFacturado)
                .ToListAsync();

            var peluquerosDict = await _context.Peluqueros
                .AsNoTracking()
                .ToDictionaryAsync(p => p.Id, p => $"{p.Nombre} {p.Apellido}".Trim());

            var rankingPeluqueros = rankingRaw.Select(r => new RankingPeluqueroDto
            {
                PeluqueroId = r.PeluqueroId,
                NombrePeluquero = peluquerosDict.TryGetValue(r.PeluqueroId, out var nom) ? nom : $"Peluquero #{r.PeluqueroId}",
                ServiciosRealizados = r.ServiciosRealizados,
                TotalFacturado = r.TotalFacturado
            }).ToList();

            string nombrePeluqueroTop = rankingPeluqueros.Any()
                ? $"{rankingPeluqueros.First().NombrePeluquero} (${rankingPeluqueros.First().TotalFacturado:N2})"
                : "Sin datos";

            // 5. Desglose de Ingresos por Medio de Pago
            var pagosRaw = await _context.Pagos
                .Include(p => p.Venta)
                .Include(p => p.MedioPago)
                .Where(p => p.Venta != null && p.Venta.Fecha >= inicio && p.Venta.Fecha <= fin)
                .GroupBy(p => p.MedioPago != null ? p.MedioPago.Nombre : "Otro")
                .Select(g => new IngresoPorMedioPagoDto
                {
                    MedioPago = g.Key,
                    TotalRecaudado = g.Sum(x => x.Importe),
                    CantidadOperaciones = g.Count()
                })
                .OrderByDescending(x => x.TotalRecaudado)
                .ToListAsync();

            // 6. Cantidad de productos en alerta de stock mínimo
            int stockCriticoCount = await _context.Productos
                .CountAsync(p => p.StockActual <= p.StockMinimoAlerta);

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