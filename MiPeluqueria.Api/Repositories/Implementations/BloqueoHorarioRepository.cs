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
    public class BloqueoHorarioRepository : GenericRepository<BloqueoHorario>, IBloqueoHorarioRepository
    {
        public BloqueoHorarioRepository(ApplicationDbContext context) : base(context)
        {
        }

        public override async Task<BloqueoHorario?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            return await _dbSet
                .Include(b => b.Peluquero)
                .FirstOrDefaultAsync(b => b.Id == id, ct);
        }

        public async Task<IReadOnlyList<BloqueoHorario>> GetByPeluqueroIdAsync(
            int peluqueroId,
            DateTime? desde = null,
            CancellationToken cancellationToken = default)
        {
            var query = _dbSet
                .AsNoTracking()
                .Include(b => b.Peluquero)
                .Where(b => b.PeluqueroId == peluqueroId);

            if (desde.HasValue)
                query = query.Where(b => b.FechaFin >= desde.Value);

            return await query
                .OrderBy(b => b.FechaInicio)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> ExisteSolapamientoAsync(
            int peluqueroId,
            DateTime fechaInicio,
            DateTime fechaFin,
            int? excludeBloqueoId = null,
            CancellationToken cancellationToken = default)
        {
            var query = _dbSet
                .AsNoTracking()
                .Where(b => b.PeluqueroId == peluqueroId && b.FechaInicio < fechaFin && b.FechaFin > fechaInicio);

            if (excludeBloqueoId.HasValue)
                query = query.Where(b => b.Id != excludeBloqueoId.Value);

            return await query.AnyAsync(cancellationToken);
        }
    }
}
