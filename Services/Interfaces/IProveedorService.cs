using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Stock;

namespace MiPeluqueria.Api.Services.Interfaces
{
    public interface IProveedorService
    {
        Task<ApiResponse<IEnumerable<ProveedorDto>>> GetAllAsync();
        Task<ApiResponse<ProveedorDto>> GetByIdAsync(int id);
        Task<ApiResponse<ProveedorDto>> CreateAsync(CrearProveedorDto dto);
        Task<ApiResponse<ProveedorDto>> UpdateAsync(int id, CrearProveedorDto dto);
        Task<ApiResponse<bool>> DeleteAsync(int id);
    }
}