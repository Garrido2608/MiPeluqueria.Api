using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Turnos;

namespace MiPeluqueria.Api.Services.Interfaces
{
    public interface ITurnoService
    {
        Task<ApiResponse<IEnumerable<TurnoResponseDto>>> GetAllAsync(DateTime? fecha = null, int? peluqueroId = null);
        Task<ApiResponse<TurnoResponseDto>> GetByIdAsync(int id);
        Task<ApiResponse<TurnoResponseDto>> CreateAsync(CrearTurnoDto dto, int usuarioId = 1);
        Task<ApiResponse<bool>> CambiarEstadoAsync(int id, CambiarEstadoTurnoDto dto, int usuarioId = 1);
        Task<ApiResponse<IEnumerable<string>>> ObtenerHorariosDisponiblesAsync(int peluqueroId, DateTime fecha, List<int>? serviciosIds = null);
        Task<ApiResponse<IEnumerable<TurnoResponseDto>>> ObtenerTurnosPorClienteAsync(int clienteId);
        Task<ApiResponse<IEnumerable<object>>> ObtenerHistorialEstadoTurnoAsync(int turnoId);
    }
}