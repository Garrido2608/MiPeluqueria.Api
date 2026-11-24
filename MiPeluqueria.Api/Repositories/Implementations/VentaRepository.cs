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
    public class VentaRepository : GenericRepository<Venta>, IVentaRepository
    {
        public VentaRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Venta?> GetByIdCompletaAsync(int ventaId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(v => v.Cliente)
                .Include(v => v.Turno)
                .Include(v => v.SesionCaja)
                .Include(v => v.DetallesVenta)
                .Include(v => v.Pagos)
                    .ThenInclude(p => p.MedioPago)
                .FirstOrDefaultAsync(v => v.Id == ventaId, cancellationToken);
        }

        public async Task<Venta?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(idempotencyKey))
                return null;

            var normalized = idempotencyKey.Trim();
            return await _dbSet
                .Include(v => v.Cliente)
                .Include(v => v.Turno)
                .Include(v => v.SesionCaja)
                .Include(v => v.DetallesVenta)
                .Include(v => v.Pagos)
                    .ThenInclude(p => p.MedioPago)
                .FirstOrDefaultAsync(v => v.IdempotencyKey == normalized && !v.IsDeleted, cancellationToken);
        }

        public async Task<bool> ExisteVentaActivaConSenaPorTurnoAsync(
            int turnoId,
            int? excludeVentaId = null,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .AsNoTracking()
                .AnyAsync(v => v.TurnoId == turnoId
                            && !v.IsDeleted
                            && v.DescuentoSenaAplicado > 0
                            && (!excludeVentaId.HasValue || v.Id != excludeVentaId.Value), cancellationToken);
        }

        public async Task<bool> TieneRenglonesEnLiquidacionActivaAsync(int ventaId, CancellationToken cancellationToken = default)
        {
            return await _context.DetallesVenta
                .AsNoTracking()
                .AnyAsync(dv => dv.VentaId == ventaId
                             && !dv.IsDeleted
                             && _context.DetallesLiquidacion.Any(dl =>
                                    dl.DetalleVentaId == dv.Id
                                    && !dl.IsDeleted
                                    && dl.Liquidacion != null
                                    && !dl.Liquidacion.IsDeleted
                                    && dl.Liquidacion.Estado != EstadoLiquidacionEnum.Anulada), cancellationToken);
        }

        public async Task<IReadOnlyList<Venta>> GetBySesionCajaIdAsync(int sesionCajaId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(v => v.Cliente)
                .Include(v => v.DetallesVenta)
                .Include(v => v.Pagos)
                    .ThenInclude(p => p.MedioPago)
                .Where(v => v.SesionCajaId == sesionCajaId)
                .OrderByDescending(v => v.Fecha)
                .ToListAsync(cancellationToken);
        }

        public async Task<(IReadOnlyList<Venta> Items, int TotalRecords)> GetBySesionCajaIdPagedAsync(
            int sesionCajaId,
            PaginationFilterDto filter,
            CancellationToken cancellationToken = default)
        {
            var query = _dbSet
                .AsNoTracking()
                .Include(v => v.Cliente)
                .Include(v => v.DetallesVenta)
                .Include(v => v.Pagos)
                    .ThenInclude(p => p.MedioPago)
                .Where(v => v.SesionCajaId == sesionCajaId);

            if (filter.FechaDesde.HasValue)
                query = query.Where(v => v.Fecha >= filter.FechaDesde.Value);

            if (filter.FechaHasta.HasValue)
                query = query.Where(v => v.Fecha <= filter.FechaHasta.Value);

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim().ToLower();
                query = query.Where(v =>
                    v.Cliente != null && (v.Cliente.Nombre.ToLower().Contains(term) || v.Cliente.Apellido.ToLower().Contains(term)));
            }

            int totalRecords = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(v => v.Fecha)
                .ThenByDescending(v => v.Id)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync(cancellationToken);

            return (items, totalRecords);
        }

        public async Task<IReadOnlyList<Venta>> GetVentasParaLiquidacionAsync(
            int peluqueroId,
            DateTime fechaDesde,
            DateTime fechaHasta,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(v => v.DetallesVenta)
                .Include(v => v.Turno)
                .Where(v => !v.IsDeleted
                         && v.Fecha >= fechaDesde
                         && v.Fecha <= fechaHasta
                         && (v.DetallesVenta.Any(d => d.PeluqueroId == peluqueroId) || (v.Turno != null && v.Turno.PeluqueroId == peluqueroId)))
                .ToListAsync(cancellationToken);
        }

        public async Task<(string Nombre, decimal Precio)?> GetDatosServicioAsync(int servicioId, CancellationToken cancellationToken = default)
        {
            var servicio = await _context.Servicios
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == servicioId && !s.IsDeleted, cancellationToken);

            if (servicio == null)
                return null;

            return (servicio.Nombre, servicio.Precio);
        }

        public async Task<string?> GetNombrePeluqueroAsync(int peluqueroId, CancellationToken cancellationToken = default)
        {
            var peluquero = await _context.Peluqueros
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == peluqueroId && !p.IsDeleted, cancellationToken);

            if (peluquero == null)
                return null;

            return $"{peluquero.Nombre} {peluquero.Apellido}".Trim();
        }
    }
}
