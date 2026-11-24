using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MiPeluqueria.Api.Models.Turnos;

namespace MiPeluqueria.Api.Repositories.Interfaces
{
    public interface IBloqueoHorarioRepository : IGenericRepository<BloqueoHorario>
    {
        Task<IReadOnlyList<BloqueoHorario>> GetByPeluqueroIdAsync(int peluqueroId, DateTime? desde = null, CancellationToken cancellationToken = default);
        Task<bool> ExisteSolapamientoAsync(int peluqueroId, DateTime fechaInicio, DateTime fechaFin, int? excludeBloqueoId = null, CancellationToken cancellationToken = default);
    }
}
