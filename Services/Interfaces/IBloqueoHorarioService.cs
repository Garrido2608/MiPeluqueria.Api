using System.Collections.Generic;
using System.Threading.Tasks;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Turnos;

namespace MiPeluqueria.Api.Services.Interfaces
{
    public interface IBloqueoHorarioService
    {
        Task<ApiResponse<IEnumerable<object>>> ObtenerPorPeluqueroAsync(int peluqueroId);
        Task<ApiResponse<int>> CrearBloqueoAsync(BloqueoHorarioDto dto);
        Task<ApiResponse<bool>> EliminarBloqueoAsync(int id);
    }
}