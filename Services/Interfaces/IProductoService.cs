using System.Collections.Generic;
using System.Threading.Tasks;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Stock;

namespace MiPeluqueria.Api.Services.Interfaces
{
    public interface IProductoService
    {
        Task<ApiResponse<List<ProductoDto>>> GetAllAsync();
        Task<ApiResponse<List<ProductoDto>>> GetStockCriticoAsync();
        Task<ApiResponse<ProductoDto>> GetByIdAsync(int id);
        Task<ApiResponse<ProductoDto>> CreateAsync(CrearProductoDto dto, int usuarioId);
        Task<ApiResponse<bool>> RegistrarMovimientoAsync(MovimientoStockDto dto, int usuarioId);
    }
}