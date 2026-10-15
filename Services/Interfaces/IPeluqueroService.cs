using System.Collections.Generic;
using System.Threading.Tasks;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Turnos;

namespace MiPeluqueria.Api.Services.Interfaces
{
    public interface IPeluqueroService
    {
        Task<ApiResponse<IEnumerable<PeluqueroDto>>> GetAllAsync();
        Task<ApiResponse<PeluqueroDto>> GetByIdAsync(int id);
        Task<ApiResponse<PeluqueroDto>> CreateAsync(CrearPeluqueroDto dto);
        Task<ApiResponse<PeluqueroDto>> UpdateAsync(int id, CrearPeluqueroDto dto);
        Task<ApiResponse<bool>> DeleteAsync(int id);
        Task<ApiResponse<IEnumerable<HorarioLaboralDto>>> ObtenerHorariosAsync(int peluqueroId);
        Task<ApiResponse<bool>> ConfigurarHorariosAsync(int peluqueroId, GuardarHorariosPeluqueroDto dto);
    }
}