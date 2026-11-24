using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MiPeluqueria.Api.Data.Context;
using MiPeluqueria.Api.Models.Ventas;
using MiPeluqueria.Api.Repositories.Interfaces;

namespace MiPeluqueria.Api.Repositories.Implementations
{
    public class ComisionConfigRepository : GenericRepository<ComisionesConfig>, IComisionConfigRepository
    {
        public ComisionConfigRepository(ApplicationDbContext context) : base(context)
        {
        }

        public override async Task<ComisionesConfig?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            return await GetByIdWithDetailsAsync(id, true, ct);
        }

        public async Task<ComisionesConfig?> GetByIdWithDetailsAsync(int id, bool tracking = false, CancellationToken cancellationToken = default)
        {
            var query = tracking ? _dbSet.AsQueryable() : _dbSet.AsNoTracking();
            return await query
                .Include(c => c.Peluquero)
                .Include(c => c.Servicio)
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        }

        public async Task<IReadOnlyList<ComisionesConfig>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(c => c.Peluquero)
                .Include(c => c.Servicio)
                .OrderBy(c => c.PeluqueroId)
                .ThenBy(c => c.ServicioId)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<ComisionesConfig>> GetByPeluqueroIdWithDetailsAsync(int peluqueroId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(c => c.Peluquero)
                .Include(c => c.Servicio)
                .Where(c => c.PeluqueroId == peluqueroId && !c.IsDeleted)
                .OrderBy(c => c.ServicioId)
                .ToListAsync(cancellationToken);
        }

        public async Task<ComisionesConfig?> GetExactaByPeluqueroYServicioAsync(
            int peluqueroId,
            int? servicioId,
            bool tracking = false,
            CancellationToken cancellationToken = default)
        {
            var query = tracking ? _dbSet.AsQueryable() : _dbSet.AsNoTracking();
            return await query
                .Include(c => c.Peluquero)
                .Include(c => c.Servicio)
                .FirstOrDefaultAsync(c => c.PeluqueroId == peluqueroId && c.ServicioId == servicioId && !c.IsDeleted, cancellationToken);
        }

        public async Task<ComisionesConfig?> GetVigenteByPeluqueroYServicioAsync(
            int peluqueroId,
            int? servicioId = null,
            CancellationToken cancellationToken = default)
        {
            var configs = await _dbSet
                .AsNoTracking()
                .Include(c => c.Peluquero)
                .Include(c => c.Servicio)
                .Where(c => c.PeluqueroId == peluqueroId && !c.IsDeleted)
                .ToListAsync(cancellationToken);

            if (servicioId.HasValue)
            {
                var especifica = configs.FirstOrDefault(c => c.ServicioId == servicioId.Value);
                if (especifica != null)
                    return especifica;
            }

            return configs.FirstOrDefault(c => c.ServicioId == null);
        }
    }
}
