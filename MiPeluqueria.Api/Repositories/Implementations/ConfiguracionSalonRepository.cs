using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MiPeluqueria.Api.Data.Context;
using MiPeluqueria.Api.Models.Common;
using MiPeluqueria.Api.Repositories.Interfaces;

namespace MiPeluqueria.Api.Repositories.Implementations
{
    public class ConfiguracionSalonRepository : GenericRepository<ConfiguracionSalon>, IConfiguracionSalonRepository
    {
        public ConfiguracionSalonRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<ConfiguracionSalon?> GetConfiguracionActualAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .OrderBy(c => c.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}
