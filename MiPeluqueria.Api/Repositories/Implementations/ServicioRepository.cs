using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MiPeluqueria.Api.Data.Context;
using MiPeluqueria.Api.Models.Turnos;
using MiPeluqueria.Api.Repositories.Interfaces;

namespace MiPeluqueria.Api.Repositories.Implementations
{
    public class ServicioRepository : GenericRepository<Servicio>, IServicioRepository
    {
        public ServicioRepository(ApplicationDbContext context) : base(context)
        {
        }

        public override async Task<IReadOnlyList<Servicio>> GetAllAsync(CancellationToken ct = default)
        {
            return await GetAllWithCategoriaAsync(false, ct);
        }

        public async Task<IReadOnlyList<Servicio>> GetAllWithCategoriaAsync(bool soloActivos = false, CancellationToken ct = default)
        {
            var query = _dbSet
                .AsNoTracking()
                .Include(s => s.Categoria)
                .AsQueryable();

            if (soloActivos)
                query = query.Where(s => s.Activo);

            return await query
                .OrderBy(s => s.Nombre)
                .ToListAsync(ct);
        }

        public override async Task<Servicio?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            return await GetByIdWithCategoriaAsync(id, true, ct);
        }

        public async Task<Servicio?> GetByIdWithCategoriaAsync(int id, bool tracking = false, CancellationToken ct = default)
        {
            var query = tracking ? _dbSet.AsQueryable() : _dbSet.AsNoTracking();
            return await query
                .Include(s => s.Categoria)
                .FirstOrDefaultAsync(s => s.Id == id, ct);
        }

        public async Task<IReadOnlyList<Servicio>> GetActivosByIdsAsync(IEnumerable<int> ids, CancellationToken ct = default)
        {
            if (ids == null)
                return Array.Empty<Servicio>();

            var distinctIds = ids.Distinct().ToList();
            if (distinctIds.Count == 0)
                return Array.Empty<Servicio>();

            return await _dbSet
                .AsNoTracking()
                .Include(s => s.Categoria)
                .Where(s => distinctIds.Contains(s.Id) && s.Activo)
                .ToListAsync(ct);
        }

        public async Task AddHistorialPrecioAsync(HistorialPrecioServicio historial, CancellationToken ct = default)
        {
            historial.CreatedAt = DateTime.UtcNow;
            await _context.HistorialPreciosServicio.AddAsync(historial, ct);
        }

        public async Task<IReadOnlyList<HistorialPrecioServicio>> GetHistorialPreciosAsync(int servicioId, CancellationToken ct = default)
        {
            return await _context.HistorialPreciosServicio
                .AsNoTracking()
                .Where(h => h.ServicioId == servicioId)
                .OrderByDescending(h => h.FechaCambio)
                .ThenByDescending(h => h.Id)
                .ToListAsync(ct);
        }

        public async Task<bool> TieneTurnosFuturosActivosAsync(int servicioId, DateTime desdeUtc, CancellationToken ct = default)
        {
            return await _context.TurnoServicios
                .AsNoTracking()
                .AnyAsync(ts => ts.ServicioId == servicioId
                             && ts.Turno != null
                             && !ts.Turno.IsDeleted
                             && ts.Turno.FechaHoraInicio >= desdeUtc
                             && (ts.Turno.EstadoId == (int)EstadoTurnoEnum.Pendiente
                                 || ts.Turno.EstadoId == (int)EstadoTurnoEnum.Confirmado
                                 || ts.Turno.EstadoId == (int)EstadoTurnoEnum.EnProceso), ct);
        }

        public Task UpdateAsync(Servicio servicio, CancellationToken ct = default)
        {
            Update(servicio);
            return Task.CompletedTask;
        }

        public async Task DeleteAsync(int id, CancellationToken ct = default)
        {
            var servicio = await _dbSet.FirstOrDefaultAsync(s => s.Id == id, ct);
            if (servicio != null)
            {
                servicio.Activo = false;
                Delete(servicio);
            }
        }
    }
}
