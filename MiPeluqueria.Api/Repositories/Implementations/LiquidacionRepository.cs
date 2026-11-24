using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MiPeluqueria.Api.Data.Context;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.Models.Ventas;
using MiPeluqueria.Api.Repositories.Interfaces;

namespace MiPeluqueria.Api.Repositories.Implementations
{
    public class LiquidacionRepository : GenericRepository<Liquidacion>, ILiquidacionRepository
    {
        public LiquidacionRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Liquidacion?> GetByIdWithDetallesAsync(int id, bool tracking = true, CancellationToken cancellationToken = default)
        {
            var query = tracking ? _dbSet.AsQueryable() : _dbSet.AsNoTracking();
            return await query
                .Include(l => l.Peluquero)
                .Include(l => l.DetallesLiquidacion)
                .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
        }

        public async Task<IReadOnlyList<Liquidacion>> GetAllWithPeluqueroAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(l => l.Peluquero)
                .Include(l => l.DetallesLiquidacion)
                .OrderByDescending(l => l.FechaEmision)
                .ThenByDescending(l => l.Id)
                .ToListAsync(cancellationToken);
        }

        public async Task<(IReadOnlyList<Liquidacion> Items, int TotalRecords)> GetPagedAsync(
            PaginationFilterDto filter,
            int? peluqueroId = null,
            EstadoLiquidacionEnum? estado = null,
            CancellationToken ct = default)
        {
            var query = _dbSet
                .AsNoTracking()
                .Include(l => l.Peluquero)
                .Include(l => l.DetallesLiquidacion)
                .AsQueryable();

            if (peluqueroId.HasValue)
                query = query.Where(l => l.PeluqueroId == peluqueroId.Value);

            if (estado.HasValue)
                query = query.Where(l => l.Estado == estado.Value);

            if (filter.FechaDesde.HasValue)
                query = query.Where(l => l.FechaDesde >= filter.FechaDesde.Value);

            if (filter.FechaHasta.HasValue)
                query = query.Where(l => l.FechaHasta <= filter.FechaHasta.Value);

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim().ToLower();
                query = query.Where(l =>
                    (l.Peluquero != null && (l.Peluquero.Nombre.ToLower().Contains(term) || l.Peluquero.Apellido.ToLower().Contains(term))) ||
                    (l.Observaciones != null && l.Observaciones.ToLower().Contains(term)));
            }

            int totalRecords = await query.CountAsync(ct);

            var items = await query
                .OrderByDescending(l => l.FechaEmision)
                .ThenByDescending(l => l.Id)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync(ct);

            return (items, totalRecords);
        }

        public async Task<IReadOnlyList<Liquidacion>> GetByPeluqueroIdAsync(int peluqueroId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(l => l.Peluquero)
                .Include(l => l.DetallesLiquidacion)
                .Where(l => l.PeluqueroId == peluqueroId)
                .OrderByDescending(l => l.FechaEmision)
                .ThenByDescending(l => l.Id)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> ExisteLiquidacionEnPeriodoAsync(
            int peluqueroId,
            DateTime fechaDesde,
            DateTime fechaHasta,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .AsNoTracking()
                .AnyAsync(l => l.PeluqueroId == peluqueroId
                            && !l.IsDeleted
                            && l.Estado != EstadoLiquidacionEnum.Anulada
                            && l.FechaDesde < fechaHasta
                            && l.FechaHasta > fechaDesde, cancellationToken);
        }

        public async Task<IReadOnlyList<DetalleVenta>> GetDetallesVentaPendientesLiquidacionAsync(
            int peluqueroId,
            DateTime fechaDesde,
            DateTime fechaHasta,
            CancellationToken ct = default)
        {
            return await _context.DetallesVenta
                .AsNoTracking()
                .Include(d => d.Venta)
                    .ThenInclude(v => v!.Turno)
                .Where(d => !d.IsDeleted
                         && d.Venta != null
                         && !d.Venta.IsDeleted
                         && d.Venta.Fecha >= fechaDesde
                         && d.Venta.Fecha <= fechaHasta
                         && (d.PeluqueroId == peluqueroId || (d.PeluqueroId == null && d.Venta.Turno != null && d.Venta.Turno.PeluqueroId == peluqueroId))
                         && !_context.DetallesLiquidacion.Any(dl =>
                                dl.DetalleVentaId == d.Id
                                && !dl.IsDeleted
                                && dl.Liquidacion != null
                                && !dl.Liquidacion.IsDeleted
                                && dl.Liquidacion.Estado != EstadoLiquidacionEnum.Anulada))
                .ToListAsync(ct);
        }

        public async Task<Dictionary<int, DetalleVenta>> GetDetallesVentaByIdsDictionaryAsync(
            IEnumerable<int> ids,
            CancellationToken ct = default)
        {
            if (ids == null)
                return new Dictionary<int, DetalleVenta>();

            var distinctIds = ids.Distinct().ToList();
            if (distinctIds.Count == 0)
                return new Dictionary<int, DetalleVenta>();

            return await _context.DetallesVenta
                .AsNoTracking()
                .Include(dv => dv.Venta)
                .Where(dv => distinctIds.Contains(dv.Id))
                .ToDictionaryAsync(dv => dv.Id, ct);
        }

        public void AnularDetallesLiquidacion(IEnumerable<DetalleLiquidacion> detalles)
        {
            var now = DateTime.UtcNow;
            foreach (var detalle in detalles)
            {
                detalle.IsDeleted = true;
                detalle.DeletedAt = now;
                detalle.UpdatedAt = now;
                _context.DetallesLiquidacion.Update(detalle);
            }
        }
    }
}
