using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MiPeluqueria.Api.Models.Ventas;

namespace MiPeluqueria.Api.Repositories.Interfaces
{
    public interface IComisionConfigRepository : IGenericRepository<ComisionesConfig>
    {
        Task<IReadOnlyList<ComisionesConfig>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ComisionesConfig>> GetByPeluqueroIdWithDetailsAsync(int peluqueroId, CancellationToken cancellationToken = default);
        Task<ComisionesConfig?> GetByIdWithDetailsAsync(int id, bool tracking = false, CancellationToken cancellationToken = default);
        Task<ComisionesConfig?> GetExactaByPeluqueroYServicioAsync(int peluqueroId, int? servicioId, bool tracking = false, CancellationToken cancellationToken = default);
        Task<ComisionesConfig?> GetVigenteByPeluqueroYServicioAsync(int peluqueroId, int? servicioId = null, CancellationToken cancellationToken = default);
    }
}
