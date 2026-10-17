using Microsoft.EntityFrameworkCore;
using MiPeluqueria.Api.Data.Context;
using MiPeluqueria.Api.Models.Turnos;
using MiPeluqueria.Api.Repositories.Interfaces;

namespace MiPeluqueria.Api.Repositories.Implementations
{
    public class BloqueoHorarioRepository : IBloqueoHorarioRepository
    {
        private readonly ApplicationDbContext _context;

        public BloqueoHorarioRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<BloqueoHorario?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Set<BloqueoHorario>()
                .Include(b => b.Peluquero)
                .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        }

        public async Task<IReadOnlyList<BloqueoHorario>> GetByPeluqueroIdAsync(int peluqueroId, DateTime? desde = null, CancellationToken cancellationToken = default)
        {
            var query = _context.Set<BloqueoHorario>()
                .AsNoTracking()
                .Include(b => b.Peluquero)
                .Where(b => b.PeluqueroId == peluqueroId);

            if (desde.HasValue)
                query = query.Where(b => b.FechaFin >= desde.Value);

            return await query
                .OrderBy(b => b.FechaInicio)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> ExisteSolapamientoAsync(int peluqueroId, DateTime fechaInicio, DateTime fechaFin, int? excludeBloqueoId = null, CancellationToken cancellationToken = default)
        {
            var query = _context.Set<BloqueoHorario>()
                .AsNoTracking()
                .Where(b => b.PeluqueroId == peluqueroId && b.FechaInicio < fechaFin && b.FechaFin > fechaInicio);

            if (excludeBloqueoId.HasValue)
                query = query.Where(b => b.Id != excludeBloqueoId.Value);

            return await query.AnyAsync(cancellationToken);
        }

        public async Task AddAsync(BloqueoHorario bloqueo, CancellationToken cancellationToken = default)
        {
            await _context.Set<BloqueoHorario>().AddAsync(bloqueo, cancellationToken);
        }

        public void Delete(BloqueoHorario bloqueo)
        {
            bloqueo.IsDeleted = true;
            _context.Set<BloqueoHorario>().Update(bloqueo);
        }
    }
}