using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MiPeluqueria.Api.Models.Turnos;

namespace MiPeluqueria.Api.Repositories.Interfaces
{
    public interface IRecetaServicioRepository : IGenericRepository<ServicioInsumoReceta>
    {
        Task<IReadOnlyList<ServicioInsumoReceta>> GetByServicioIdWithDetailsAsync(int servicioId, bool tracking = false, CancellationToken ct = default);
        Task<IReadOnlyList<ServicioInsumoReceta>> GetByServiciosIdsAsync(IEnumerable<int> serviciosIds, bool soloDescuentoAutomatico = true, CancellationToken ct = default);
        Task AddRangeAsync(IEnumerable<ServicioInsumoReceta> items, CancellationToken ct = default);
        Task<bool> ExisteConsumoGabinetePorTurnoAsync(int turnoId, CancellationToken ct = default);
    }
}
