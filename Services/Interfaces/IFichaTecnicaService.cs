using MiPeluqueria.Api.DTOs.Clientes;
using MiPeluqueria.Api.DTOs.Common;

namespace MiPeluqueria.Api.Services.Interfaces
{
    public interface IFichaTecnicaService
    {
        Task<ApiResponse<FichaTecnicaResponseDto>> CrearFichaAsync(CrearFichaTecnicaDto dto);
        Task<ApiResponse<IEnumerable<FichaTecnicaResponseDto>>> ObtenerHistorialClienteAsync(int clienteId);
        Task<ApiResponse<FichaTecnicaResponseDto>> ActualizarFichaAsync(int id, CrearFichaTecnicaDto dto);
        Task<ApiResponse<bool>> EliminarFichaAsync(int id);
    }
}