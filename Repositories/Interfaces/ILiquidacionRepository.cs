using MiPeluqueria.Api.Models.Ventas;

namespace MiPeluqueria.Api.Repositories.Interfaces
{
    public interface ILiquidacionRepository
    {
        Task<Liquidacion?> GetByIdWithDetallesAsync(int id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Liquidacion>> GetAllWithPeluqueroAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Liquidacion>> GetByPeluqueroIdAsync(int peluqueroId, CancellationToken cancellationToken = default);
        Task<bool> ExisteLiquidacionEnPeriodoAsync(int peluqueroId, DateTime fechaDesde, DateTime fechaHasta, CancellationToken cancellationToken = default);
        Task AddAsync(Liquidacion liquidacion, CancellationToken cancellationToken = default);
        void Update(Liquidacion liquidacion);
    }
}