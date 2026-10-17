using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MiPeluqueria.Api.Data.Context;
using MiPeluqueria.Api.Models.Clientes;
using MiPeluqueria.Api.Models.Stock;
using MiPeluqueria.Api.Models.Turnos;
using MiPeluqueria.Api.Models.Ventas;
using MiPeluqueria.Api.Repositories.Interfaces;

namespace MiPeluqueria.Api.Repositories.Implementations
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly ApplicationDbContext _context;

        public DashboardRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<Venta>> GetVentasEnRangoAsync(DateTime fechaDesde, DateTime fechaHasta, CancellationToken ct = default)
        {
            return await _context.Set<Venta>()
                .AsNoTracking()
                .Include(v => v.DetallesVenta)
                .Include(v => v.Pagos)
                    .ThenInclude(p => p.MedioPago)
                .Where(v => v.Fecha >= fechaDesde && v.Fecha <= fechaHasta && !v.IsDeleted)
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<Turno>> GetTurnosEnRangoAsync(DateTime fechaDesde, DateTime fechaHasta, CancellationToken ct = default)
        {
            return await _context.Set<Turno>()
                .AsNoTracking()
                .Include(t => t.Peluquero)
                .Include(t => t.Estado)
                .Include(t => t.TurnoServicios)
                    .ThenInclude(ts => ts.Servicio)
                .Where(t => t.FechaHoraInicio >= fechaDesde && t.FechaHoraInicio <= fechaHasta && !t.IsDeleted)
                .ToListAsync(ct);
        }

        public async Task<int> CountNuevosClientesEnRangoAsync(DateTime fechaDesde, DateTime fechaHasta, CancellationToken ct = default)
        {
            return await _context.Set<Cliente>()
                .AsNoTracking()
                .CountAsync(c => c.FechaRegistro >= fechaDesde && c.FechaRegistro <= fechaHasta && !c.IsDeleted, ct);
        }

        public async Task<int> CountProductosStockCriticoAsync(CancellationToken ct = default)
        {
            return await _context.Set<Producto>()
                .AsNoTracking()
                .CountAsync(p => p.StockActual <= p.StockMinimoAlerta && !p.IsDeleted, ct);
        }
    }
}