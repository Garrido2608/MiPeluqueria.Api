using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MiPeluqueria.Api.Data.Context;
using MiPeluqueria.Api.Models.Common;
using MiPeluqueria.Api.Repositories.Interfaces;

namespace MiPeluqueria.Api.Repositories.Implementations
{
    public class ConfiguracionSalonRepository : IConfiguracionSalonRepository
    {
        private readonly ApplicationDbContext _context;

        public ConfiguracionSalonRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ConfiguracionSalon?> GetConfiguracionActualAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Set<ConfiguracionSalon>()
                .OrderBy(c => c.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task AddAsync(ConfiguracionSalon configuracion, CancellationToken cancellationToken = default)
        {
            configuracion.CreatedAt = DateTime.UtcNow;
            await _context.Set<ConfiguracionSalon>().AddAsync(configuracion, cancellationToken);
        }

        public void Update(ConfiguracionSalon configuracion)
        {
            configuracion.UpdatedAt = DateTime.UtcNow;
            _context.Set<ConfiguracionSalon>().Update(configuracion);
        }
    }
}