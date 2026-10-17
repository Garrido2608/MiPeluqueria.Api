using System.Threading;
using System.Threading.Tasks;
using MiPeluqueria.Api.Models.Common;

namespace MiPeluqueria.Api.Repositories.Interfaces
{
    public interface IConfiguracionSalonRepository
    {
        Task<ConfiguracionSalon?> GetConfiguracionActualAsync(CancellationToken cancellationToken = default);
        Task AddAsync(ConfiguracionSalon configuracion, CancellationToken cancellationToken = default);
        void Update(ConfiguracionSalon configuracion);
    }
}