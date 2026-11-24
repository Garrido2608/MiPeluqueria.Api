using System.Threading;
using System.Threading.Tasks;
using MiPeluqueria.Api.Models.Common;

namespace MiPeluqueria.Api.Repositories.Interfaces
{
    public interface IConfiguracionSalonRepository : IGenericRepository<ConfiguracionSalon>
    {
        Task<ConfiguracionSalon?> GetConfiguracionActualAsync(CancellationToken cancellationToken = default);
    }
}
