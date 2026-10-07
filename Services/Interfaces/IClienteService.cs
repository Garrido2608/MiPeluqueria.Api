using System.Threading.Tasks;
using MiPeluqueria.Api.DTOs.Clientes;
using MiPeluqueria.Api.DTOs.Common;

namespace MiPeluqueria.Api.Services.Interfaces
{
    public interface IClienteService
    {
        Task<ApiResponse<PagedResult<ClienteResponseDto>>> ObtenerTodosAsync(int pageNumber = 1, int pageSize = 10, string? search = null);
        Task<ApiResponse<ClienteResponseDto>> ObtenerPorIdAsync(int id);
        Task<ApiResponse<ClienteResponseDto>> CrearClienteAsync(ClienteRequestDto dto);
        Task<ApiResponse<ClienteResponseDto>> ActualizarClienteAsync(int id, ClienteRequestDto dto);
        Task<ApiResponse<bool>> EliminarClienteAsync(int id);
    }
}