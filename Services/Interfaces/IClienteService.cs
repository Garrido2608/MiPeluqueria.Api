using System.Collections.Generic;
using System.Threading.Tasks;
using MiPeluqueria.Api.DTOs.Clientes;
using MiPeluqueria.Api.DTOs.Common;

namespace MiPeluqueria.Api.Services.Interfaces
{
    public interface IClienteService
    {
        Task<ApiResponse<List<ClienteResponseDto>>> ObtenerTodosAsync();
        Task<ApiResponse<ClienteResponseDto>> ObtenerPorIdAsync(int id);
        Task<ApiResponse<ClienteResponseDto>> CrearClienteAsync(ClienteRequestDto dto);
        Task<ApiResponse<ClienteResponseDto>> ActualizarClienteAsync(int id, ClienteRequestDto dto);
        Task<ApiResponse<bool>> EliminarClienteAsync(int id);
    }
}