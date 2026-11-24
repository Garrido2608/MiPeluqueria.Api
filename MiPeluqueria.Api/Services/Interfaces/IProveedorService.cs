using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Stock;

namespace MiPeluqueria.Api.Services.Interfaces
{
    public interface IProveedorService
    {
        Task<ApiResponse<PagedResult<ProveedorDto>>> GetPagedAsync(
            PaginationFilterDto filter,
            CancellationToken ct = default);

        Task<ApiResponse<IEnumerable<ProveedorDto>>> GetAllAsync(CancellationToken ct = default);

        Task<ApiResponse<ProveedorDto>> GetByIdAsync(int id, CancellationToken ct = default);

        Task<ApiResponse<ProveedorDto>> CreateAsync(CrearProveedorDto dto, CancellationToken ct = default);

        Task<ApiResponse<ProveedorDto>> UpdateAsync(
            int id,
            ActualizarProveedorDto dto,
            CancellationToken ct = default);

        Task<ApiResponse<ProveedorDto>> UpdateAsync(int id, CrearProveedorDto dto);

        Task<ApiResponse<bool>> DeleteAsync(int id, CancellationToken ct = default);
    }
}
