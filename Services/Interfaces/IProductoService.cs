using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Stock;

namespace MiPeluqueria.Api.Services.Interfaces
{
    public interface IProductoService
    {
        Task<ApiResponse<PagedResult<ProductoDto>>> GetAllAsync(
            PaginationFilterDto filter,
            bool soloBajoStock = false,
            int? categoriaId = null,
            int? proveedorId = null,
            CancellationToken ct = default);

        Task<ApiResponse<PagedResult<ProductoDto>>> GetAllAsync(
            bool soloBajoStock = false,
            CancellationToken ct = default);

        Task<ApiResponse<IReadOnlyList<ProductoDto>>> GetStockCriticoAsync(CancellationToken ct = default);

        Task<ApiResponse<ProductoDto>> GetByIdAsync(int id, CancellationToken ct = default);

        Task<ApiResponse<ProductoDto>> CreateAsync(
            CrearProductoDto dto,
            int usuarioId = 1,
            CancellationToken ct = default);

        Task<ApiResponse<ProductoDto>> UpdateAsync(
            int id,
            ActualizarProductoDto dto,
            int? usuarioModificacionId = null,
            CancellationToken ct = default);

        Task<ApiResponse<ProductoDto>> UpdateAsync(int id, CrearProductoDto dto);

        Task<ApiResponse<bool>> DeleteAsync(int id, CancellationToken ct = default);

        Task<ApiResponse<bool>> RegistrarMovimientoStockAsync(
            MovimientoStockDto dto,
            int usuarioId = 1,
            CancellationToken ct = default);

        Task<ApiResponse<PagedResult<KardexItemResponseDto>>> ObtenerKardexProductoAsync(
            int productoId,
            PaginationFilterDto filter,
            CancellationToken ct = default);

        Task<ApiResponse<PagedResult<KardexItemResponseDto>>> ObtenerKardexProductoAsync(
            int productoId,
            CancellationToken ct = default);
    }
}
