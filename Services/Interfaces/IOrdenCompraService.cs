using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Stock;
using MiPeluqueria.Api.Models.Stock;

namespace MiPeluqueria.Api.Services.Interfaces
{
    public interface IOrdenCompraService
    {
        Task<ApiResponse<IReadOnlyList<OrdenCompraResponseDto>>> ObtenerTodasAsync(int? proveedorId = null, EstadoOrdenCompraEnum? estado = null, CancellationToken ct = default);
        Task<ApiResponse<OrdenCompraResponseDto>> ObtenerPorIdAsync(int id, CancellationToken ct = default);
        Task<ApiResponse<OrdenCompraResponseDto>> CrearOrdenAsync(CrearOrdenCompraDto dto, int usuarioId, CancellationToken ct = default);
        Task<ApiResponse<OrdenCompraResponseDto>> RecepcionarOrdenAsync(int id, RecepcionarOrdenCompraDto dto, int usuarioId, CancellationToken ct = default);
        Task<ApiResponse<bool>> CancelarOrdenAsync(int id, string motivo, int usuarioId, CancellationToken ct = default);
    }
}