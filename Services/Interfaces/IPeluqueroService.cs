using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Turnos;

namespace MiPeluqueria.Api.Services.Interfaces
{
    public interface IPeluqueroService
    {
        Task<ApiResponse<IEnumerable<PeluqueroDto>>> GetAllAsync(bool soloActivos = false, CancellationToken ct = default);
        Task<ApiResponse<PeluqueroDto>> GetByIdAsync(int id, CancellationToken ct = default);
        Task<ApiResponse<PeluqueroDto>> CreateAsync(CrearPeluqueroDto dto, CancellationToken ct = default);
        Task<ApiResponse<PeluqueroDto>> UpdateAsync(int id, ActualizarPeluqueroDto dto, CancellationToken ct = default);
        Task<ApiResponse<PeluqueroDto>> UpdateAsync(int id, CrearPeluqueroDto dto);
        Task<ApiResponse<bool>> DeleteAsync(int id, CancellationToken ct = default);
        Task<ApiResponse<IEnumerable<HorarioLaboralDto>>> ObtenerHorariosAsync(int peluqueroId, CancellationToken ct = default);
        Task<ApiResponse<bool>> ConfigurarHorariosAsync(int peluqueroId, GuardarHorariosPeluqueroDto dto, CancellationToken ct = default);
    }
}
