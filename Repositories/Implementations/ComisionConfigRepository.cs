using Microsoft.EntityFrameworkCore;
using MiPeluqueria.Api.Data.Context;
using MiPeluqueria.Api.Models.Ventas;
using MiPeluqueria.Api.Repositories.Interfaces;

namespace MiPeluqueria.Api.Repositories.Implementations
{
    public class ComisionConfigRepository : IComisionConfigRepository
    {
        private readonly ApplicationDbContext _context;

        public ComisionConfigRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ComisionesConfig?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Set<ComisionesConfig>()
                .Include(c => c.Peluquero)
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        }

        public async Task<IReadOnlyList<ComisionesConfig>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Set<ComisionesConfig>()
                .AsNoTracking()
                .Include(c => c.Peluquero)
                .OrderBy(c => c.PeluqueroId)
                .ToListAsync(cancellationToken);
        }

        public async Task<ComisionesConfig?> GetVigenteByPeluqueroYServicioAsync(int peluqueroId, int? servicioId = null, CancellationToken cancellationToken = default)
        {
            var configs = await _context.Set<ComisionesConfig>()
                .AsNoTracking()
                .Where(c => c.PeluqueroId == peluqueroId && !c.IsDeleted)
                .ToListAsync(cancellationToken);

            return configs.FirstOrDefault();
        }

        public async Task AddAsync(ComisionesConfig config, CancellationToken cancellationToken = default)
        {
            await _context.Set<ComisionesConfig>().AddAsync(config, cancellationToken);
        }

        public void Update(ComisionesConfig config)
        {
            _context.Set<ComisionesConfig>().Update(config);
        }

        public void Delete(ComisionesConfig config)
        {
            config.IsDeleted = true;
            _context.Set<ComisionesConfig>().Update(config);
        }
    }
}