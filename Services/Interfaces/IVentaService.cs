using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Ventas;

namespace MiPeluqueria.Api.Services.Interfaces
{
    public interface IVentaService
    {
        Task<ApiResponse<VentaResponseDto>> ObtenerVentaPorIdAsync(int id, CancellationToken ct = default);
        Task<ApiResponse<IReadOnlyList<VentaResponseDto>>> ObtenerVentasPorSesionAsync(int sesionCajaId, CancellationToken ct = default);
        Task<ApiResponse<VentaResponseDto>> RegistrarVentaAsync(CrearVentaDto dto, int usuarioId, CancellationToken ct = default);
        Task<ApiResponse<VentaResponseDto>> AnularVentaAsync(int ventaId, string motivo, int usuarioId, CancellationToken ct = default);
    }
}