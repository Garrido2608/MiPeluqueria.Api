using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Stock;

namespace MiPeluqueria.Api.Services.Interfaces
{
    public interface IProductoService
    {
        Task<ApiResponse<IEnumerable<ProductoDto>>> GetAllAsync(bool soloBajoStock = false);
        Task<ApiResponse<IEnumerable<ProductoDto>>> GetStockCriticoAsync();
        Task<ApiResponse<ProductoDto>> GetByIdAsync(int id);
        Task<ApiResponse<ProductoDto>> CreateAsync(CrearProductoDto dto, int usuarioId = 1);
        Task<ApiResponse<ProductoDto>> UpdateAsync(int id, CrearProductoDto dto);
        Task<ApiResponse<bool>> DeleteAsync(int id);
        Task<ApiResponse<bool>> RegistrarMovimientoStockAsync(MovimientoStockDto dto, int usuarioId = 1);
        Task<ApiResponse<IEnumerable<object>>> ObtenerKardexProductoAsync(int productoId);
    }
}