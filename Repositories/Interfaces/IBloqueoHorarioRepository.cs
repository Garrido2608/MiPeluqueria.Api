using MiPeluqueria.Api.Models.Turnos;

namespace MiPeluqueria.Api.Repositories.Interfaces
{
    public interface IBloqueoHorarioRepository
    {
        Task<BloqueoHorario?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<BloqueoHorario>> GetByPeluqueroIdAsync(int peluqueroId, DateTime? desde = null, CancellationToken cancellationToken = default);
        Task<bool> ExisteSolapamientoAsync(int peluqueroId, DateTime fechaInicio, DateTime fechaFin, int? excludeBloqueoId = null, CancellationToken cancellationToken = default);
        Task AddAsync(BloqueoHorario bloqueo, CancellationToken cancellationToken = default);
        void Delete(BloqueoHorario bloqueo);
    }
}