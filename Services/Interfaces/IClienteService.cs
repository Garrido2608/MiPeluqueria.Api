using System.Threading;
using System.Threading.Tasks;
using MiPeluqueria.Api.DTOs.Clientes;
using MiPeluqueria.Api.DTOs.Common;

namespace MiPeluqueria.Api.Services.Interfaces
{
    public interface IClienteService
    {
        Task<ApiResponse<PagedResult<ClienteResponseDto>>> ObtenerTodosAsync(int pageNumber = 1, int pageSize = 15, string? search = null);
        Task<ApiResponse<PagedResult<ClienteResponseDto>>> ObtenerTodosAsync(PaginationFilterDto filter, CancellationToken ct = default);
        Task<ApiResponse<ClienteResponseDto>> ObtenerPorIdAsync(int id);
        Task<ApiResponse<ClienteResponseDto>> CrearClienteAsync(ClienteRequestDto dto);
        Task<ApiResponse<ClienteResponseDto>> ActualizarClienteAsync(int id, ClienteRequestDto dto);
        Task<ApiResponse<bool>> EliminarClienteAsync(int id);
    }
}
