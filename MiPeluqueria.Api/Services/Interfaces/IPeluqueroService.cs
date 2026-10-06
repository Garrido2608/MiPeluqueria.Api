using System.Collections.Generic;
using System.Threading.Tasks;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Turnos;

namespace MiPeluqueria.Api.Services.Interfaces
{
    public interface IPeluqueroService
    {
        Task<ApiResponse<List<PeluqueroDto>>> GetAllAsync();
        Task<ApiResponse<PeluqueroDto>> GetByIdAsync(int id);
        Task<ApiResponse<PeluqueroDto>> CreateAsync(CrearPeluqueroDto dto);
    }
}