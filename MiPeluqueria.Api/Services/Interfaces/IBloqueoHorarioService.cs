using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Turnos;

namespace MiPeluqueria.Api.Services.Interfaces
{
    public interface IBloqueoHorarioService
    {
        Task<ApiResponse<IReadOnlyList<BloqueoHorarioResponseDto>>> ObtenerPorPeluqueroAsync(int peluqueroId, CancellationToken ct = default);
        Task<ApiResponse<BloqueoHorarioResponseDto>> CrearBloqueoAsync(BloqueoHorarioDto dto, CancellationToken ct = default);
        Task<ApiResponse<bool>> EliminarBloqueoAsync(int id, CancellationToken ct = default);
    }
}
