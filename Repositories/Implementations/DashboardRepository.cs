using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MiPeluqueria.Api.Data.Context;
using MiPeluqueria.Api.Models.Turnos;
using MiPeluqueria.Api.Models.Ventas;
using MiPeluqueria.Api.Repositories.Interfaces;

namespace MiPeluqueria.Api.Repositories.Implementations
{
    public class DashboardRepository : GenericRepository<Venta>, IDashboardRepository
    {
        public DashboardRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IReadOnlyList<Venta>> GetVentasEnRangoAsync(DateTime fechaDesde, DateTime fechaHasta, CancellationToken ct = default)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(v => v.DetallesVenta)
                .Include(v => v.Pagos)
                    .ThenInclude(p => p.MedioPago)
                .Where(v => v.Fecha >= fechaDesde && v.Fecha <= fechaHasta && !v.IsDeleted)
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<Turno>> GetTurnosEnRangoAsync(DateTime fechaDesde, DateTime fechaHasta, CancellationToken ct = default)
        {
            return await _context.Turnos
                .AsNoTracking()
                .Include(t => t.Peluquero)
                .Include(t => t.Estado)
                .Include(t => t.TurnoServicios)
                    .ThenInclude(ts => ts.Servicio)
                .Where(t => t.FechaHoraInicio >= fechaDesde && t.FechaHoraInicio <= fechaHasta && !t.IsDeleted)
                .ToListAsync(ct);
        }

        public async Task<int> CountTurnosPendientesEnRangoAsync(DateTime inicioUtc, DateTime finUtc, CancellationToken ct = default)
        {
            return await _context.Turnos
                .AsNoTracking()
                .CountAsync(t => t.FechaHoraInicio >= inicioUtc
                              && t.FechaHoraInicio <= finUtc
                              && !t.IsDeleted
                              && (t.EstadoId == (int)EstadoTurnoEnum.Pendiente || t.EstadoId == (int)EstadoTurnoEnum.Confirmado), ct);
        }

        public async Task<int> CountNuevosClientesEnRangoAsync(DateTime fechaDesde, DateTime fechaHasta, CancellationToken ct = default)
        {
            return await _context.Clientes
                .AsNoTracking()
                .CountAsync(c => c.FechaRegistro >= fechaDesde && c.FechaRegistro <= fechaHasta && !c.IsDeleted, ct);
        }

        public async Task<int> CountProductosStockCriticoAsync(CancellationToken ct = default)
        {
            return await _context.Productos
                .AsNoTracking()
                .CountAsync(p => p.StockActual <= p.StockMinimoAlerta && !p.IsDeleted, ct);
        }
    }
}
