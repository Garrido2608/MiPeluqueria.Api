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
    public class SesionCajaRepository : GenericRepository<SesionCaja>, ISesionCajaRepository
    {
        public SesionCajaRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<SesionCaja?> GetCajaAbiertaActualAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(s => s.MovimientosCaja)
                .Include(s => s.Ventas)
                    .ThenInclude(v => v.Pagos)
                        .ThenInclude(p => p.MedioPago)
                .FirstOrDefaultAsync(s => s.EstadoAbierta && s.FechaCierre == null && !s.IsDeleted, cancellationToken);
        }

        public async Task<SesionCaja?> GetByIdWithMovimientosYVentasAsync(int sesionCajaId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(s => s.MovimientosCaja)
                .Include(s => s.Ventas)
                    .ThenInclude(v => v.Pagos)
                        .ThenInclude(p => p.MedioPago)
                .FirstOrDefaultAsync(s => s.Id == sesionCajaId, cancellationToken);
        }

        public async Task<IReadOnlyList<SesionCaja>> GetHistorialCajasAsync(
            DateTime? desde = null,
            DateTime? hasta = null,
            CancellationToken cancellationToken = default)
        {
            var query = _dbSet
                .AsNoTracking()
                .Where(s => !s.IsDeleted);

            if (desde.HasValue)
                query = query.Where(s => s.FechaApertura >= desde.Value);

            if (hasta.HasValue)
                query = query.Where(s => s.FechaApertura <= hasta.Value);

            return await query
                .OrderByDescending(s => s.FechaApertura)
                .ToListAsync(cancellationToken);
        }

        public async Task<(IReadOnlyList<SesionCaja> Items, int TotalRecords)> GetHistorialCajasPagedAsync(
            PaginationFilterDto filter,
            CancellationToken ct = default)
        {
            var query = _dbSet
                .AsNoTracking()
                .Where(s => !s.IsDeleted);

            if (filter.FechaDesde.HasValue)
                query = query.Where(s => s.FechaApertura >= filter.FechaDesde.Value);

            if (filter.FechaHasta.HasValue)
                query = query.Where(s => s.FechaApertura <= filter.FechaHasta.Value);

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim().ToLower();
                query = query.Where(s => s.ObservacionesCierre != null && s.ObservacionesCierre.ToLower().Contains(term));
            }

            int totalRecords = await query.CountAsync(ct);

            var items = await query
                .OrderByDescending(s => s.FechaApertura)
                .ThenByDescending(s => s.Id)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync(ct);

            return (items, totalRecords);
        }

        public async Task AbrirSesionCajaAsync(SesionCaja sesionCaja, CancellationToken cancellationToken = default)
        {
            await AddAsync(sesionCaja, cancellationToken);
        }

        public void ActualizarSesionCaja(SesionCaja sesionCaja)
        {
            Update(sesionCaja);
        }

        public async Task RegistrarMovimientoCajaAsync(MovimientoCaja movimiento, CancellationToken cancellationToken = default)
        {
            movimiento.CreatedAt = DateTime.UtcNow;
            await _context.MovimientosCaja.AddAsync(movimiento, cancellationToken);
        }

        public async Task<IReadOnlyList<MovimientoCaja>> GetMovimientosBySesionIdAsync(int sesionCajaId, CancellationToken cancellationToken = default)
        {
            return await _context.MovimientosCaja
                .AsNoTracking()
                .Where(m => m.SesionCajaId == sesionCajaId)
                .OrderByDescending(m => m.Fecha)
                .ToListAsync(cancellationToken);
        }
    }
}
