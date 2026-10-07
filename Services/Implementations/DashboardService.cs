using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MiPeluqueria.Api.Data.Context;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.Models.Turnos;
using MiPeluqueria.Api.Models.Ventas;

namespace MiPeluqueria.Api.Services.Implementations
{
    public class DashboardKpiResponseDto
    {
        public decimal FacturacionTotalMes { get; set; }
        public int CantidadVentasMes { get; set; }
        public int TotalTurnosAtendidosMes { get; set; }
        public int TurnosPendientesHoy { get; set; }
        public decimal TicketPromedio { get; set; }
        public string ServicioMasVendido { get; set; } = string.Empty;
        public string PeluqueroEstrella { get; set; } = string.Empty;
        public int ProductosEnStockCritico { get; set; }
    }

    public interface IDashboardService
    {
        Task<ApiResponse<DashboardKpiResponseDto>> ObtenerMetricasMesActualAsync();
    }

    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;

        public DashboardService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<DashboardKpiResponseDto>> ObtenerMetricasMesActualAsync()
        {
            var ahora = DateTime.UtcNow;
            var inicioMes = new DateTime(ahora.Year, ahora.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var inicioHoy = ahora.Date;
            var finHoy = inicioHoy.AddDays(1);

            // 1. Agregaciones directas en SQL Server para Ventas del mes
            var queryVentasMes = _context.Ventas.Where(v => v.Fecha >= inicioMes);
            int cantidadVentas = await queryVentasMes.CountAsync();
            decimal facturacion = cantidadVentas > 0
                ? await queryVentasMes.SumAsync(v => v.Total)
                : 0m;
            decimal ticketPromedio = cantidadVentas > 0
                ? Math.Round(facturacion / cantidadVentas, 2)
                : 0m;

            // 2. Conteo de turnos completados en el mes y pendientes para hoy
            int turnosCompletados = await _context.Turnos
                .CountAsync(t => t.FechaHoraInicio >= inicioMes && t.EstadoId == (int)EstadoTurnoEnum.Completado);

            int turnosHoy = await _context.Turnos
                .CountAsync(t => t.FechaHoraInicio >= inicioHoy
                                 && t.FechaHoraInicio < finHoy
                                 && (t.EstadoId == (int)EstadoTurnoEnum.Pendiente
                                     || t.EstadoId == (int)EstadoTurnoEnum.Confirmado));

            // 3. Servicio más vendido real en el mes mediante GroupBy en SQL Server
            var topServicioGroup = await _context.DetallesVenta
                .Where(d => d.Venta != null && d.Venta.Fecha >= inicioMes && d.TipoItem == TipoItemVentaEnum.Servicio)
                .GroupBy(d => d.ItemId)
                .Select(g => new { ServicioId = g.Key, TotalUnidades = g.Sum(x => x.Cantidad) })
                .OrderByDescending(x => x.TotalUnidades)
                .FirstOrDefaultAsync();

            string nombreServicioTop = "Sin servicios facturados en el mes";
            if (topServicioGroup != null)
            {
                var serv = await _context.Servicios
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == topServicioGroup.ServicioId);

                if (serv != null)
                    nombreServicioTop = $"{serv.Nombre} ({topServicioGroup.TotalUnidades} realizados)";
            }

            // 4. Profesional estrella real del mes (por facturación generada)
            var topPeluqueroGroup = await _context.DetallesVenta
                .Where(d => d.Venta != null && d.Venta.Fecha >= inicioMes && d.PeluqueroId != null)
                .GroupBy(d => d.PeluqueroId!.Value)
                .Select(g => new { PeluqueroId = g.Key, TotalGenerado = g.Sum(x => x.Subtotal) })
                .OrderByDescending(x => x.TotalGenerado)
                .FirstOrDefaultAsync();

            string nombrePeluqueroTop = "Sin registros en el período";
            if (topPeluqueroGroup != null)
            {
                var pel = await _context.Peluqueros
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == topPeluqueroGroup.PeluqueroId);

                if (pel != null)
                    nombrePeluqueroTop = $"{pel.Nombre} {pel.Apellido} (${topPeluqueroGroup.TotalGenerado:N2})";
            }

            // 5. Cantidad de productos en alerta de stock mínimo
            int stockCriticoCount = await _context.Productos
                .CountAsync(p => p.StockActual <= p.StockMinimoAlerta);

            var kpi = new DashboardKpiResponseDto
            {
                FacturacionTotalMes = facturacion,
                CantidadVentasMes = cantidadVentas,
                TotalTurnosAtendidosMes = turnosCompletados,
                TurnosPendientesHoy = turnosHoy,
                TicketPromedio = ticketPromedio,
                ServicioMasVendido = nombreServicioTop,
                PeluqueroEstrella = nombrePeluqueroTop,
                ProductosEnStockCritico = stockCriticoCount
            };

            return ApiResponse<DashboardKpiResponseDto>.Exito(kpi, "KPIs gerenciales calculados en tiempo real.");
        }
    }
}