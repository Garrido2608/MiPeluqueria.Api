using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MiPeluqueria.Api.DTOs.Clientes;
using MiPeluqueria.Api.DTOs.Common;

namespace MiPeluqueria.Api.Services.Interfaces
{
    public interface IFichaTecnicaService
    {
        Task<ApiResponse<FichaTecnicaResponseDto>> ObtenerPorIdAsync(int id, CancellationToken ct = default);
        Task<ApiResponse<IEnumerable<FichaTecnicaResponseDto>>> ObtenerHistorialClienteAsync(int clienteId);
        Task<ApiResponse<FichaTecnicaResponseDto>> CrearFichaAsync(CrearFichaTecnicaDto dto);
        Task<ApiResponse<FichaTecnicaResponseDto>> ActualizarFichaAsync(int id, ActualizarFichaTecnicaDto dto, CancellationToken ct = default);
        Task<ApiResponse<FichaTecnicaResponseDto>> ActualizarFichaAsync(int id, CrearFichaTecnicaDto dto);
        Task<ApiResponse<bool>> EliminarFichaAsync(int id);
    }
}
