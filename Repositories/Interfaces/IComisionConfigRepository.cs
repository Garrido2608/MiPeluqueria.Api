using MiPeluqueria.Api.Models.Ventas;

namespace MiPeluqueria.Api.Repositories.Interfaces
{
    public interface IComisionConfigRepository
    {
        Task<ComisionesConfig?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ComisionesConfig>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default);
        Task<ComisionesConfig?> GetVigenteByPeluqueroYServicioAsync(int peluqueroId, int? servicioId = null, CancellationToken cancellationToken = default);
        Task AddAsync(ComisionesConfig config, CancellationToken cancellationToken = default);
        void Update(ComisionesConfig config);
        void Delete(ComisionesConfig config);
    }
}