using System.Collections.Generic;
using System.Threading.Tasks;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Turnos;
using MiPeluqueria.Api.Models.Turnos;

namespace MiPeluqueria.Api.Services.Interfaces
{
    public interface IBloqueoHorarioService
    {
        Task<ApiResponse<int>> CrearBloqueoAsync(BloqueoHorarioDto dto);
        Task<ApiResponse<List<BloqueoHorario>>> ObtenerPorPeluqueroAsync(int peluqueroId);
        Task<ApiResponse<bool>> EliminarBloqueoAsync(int bloqueoId);
    }
}