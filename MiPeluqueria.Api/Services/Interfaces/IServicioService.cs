using System.Collections.Generic;
using System.Threading.Tasks;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Turnos;

namespace MiPeluqueria.Api.Services.Interfaces
{
    public interface IServicioService
    {
        Task<ApiResponse<List<ServicioDto>>> GetAllAsync();
        Task<ApiResponse<ServicioDto>> GetByIdAsync(int id);
        Task<ApiResponse<ServicioDto>> CreateAsync(CrearServicioDto dto);
    }
}