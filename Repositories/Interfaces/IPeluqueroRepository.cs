using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MiPeluqueria.Api.Models.Turnos;

namespace MiPeluqueria.Api.Repositories.Interfaces
{
    public interface IPeluqueroRepository : IGenericRepository<Peluquero>
    {
        Task<IReadOnlyList<Peluquero>> GetAllWithDetailsAsync(bool soloActivos = false, CancellationToken ct = default);
        Task<Peluquero?> GetByIdWithDetailsAsync(int id, bool tracking = false, CancellationToken ct = default);
        Task<bool> ExistsEmailAsync(string email, int? excludeId = null, CancellationToken ct = default);
        Task<int> CountActivosAsync(CancellationToken ct = default);
        Task ReemplazarEspecialidadesAsync(int peluqueroId, IEnumerable<int> especialidadesIds, CancellationToken ct = default);
        Task<IReadOnlyList<HorarioLaboral>> GetHorariosByPeluqueroIdAsync(int peluqueroId, CancellationToken ct = default);
        Task<HorarioLaboral?> GetHorarioDiaAsync(int peluqueroId, DayOfWeek diaSemana, CancellationToken ct = default);
        Task ReemplazarHorariosLaboralesAsync(int peluqueroId, IEnumerable<HorarioLaboral> horarios, CancellationToken ct = default);
        Task<bool> TieneTurnosFuturosActivosAsync(int peluqueroId, DateTime desdeUtc, CancellationToken ct = default);
        Task<bool> TieneLiquidacionesPendientesAsync(int peluqueroId, CancellationToken ct = default);
        Task UpdateAsync(Peluquero peluquero, CancellationToken ct = default);
        Task DeleteAsync(int id, CancellationToken ct = default);
    }
}
