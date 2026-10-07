using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Turnos;

namespace MiPeluqueria.Api.Services.Interfaces
{
    public interface ITurnoService
    {
        Task<ApiResponse<TurnoResponseDto>> ReservarTurnoAsync(CrearTurnoDto dto);
        Task<ApiResponse<bool>> CambiarEstadoAsync(int turnoId, CambiarEstadoTurnoDto dto, int usuarioId);
        Task<ApiResponse<List<TurnoResponseDto>>> ObtenerAgendaPeluqueroFechaAsync(int peluqueroId, DateTime fecha);
    }
}