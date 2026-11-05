using System;
using System.Threading;
using System.Threading.Tasks;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Ventas;

namespace MiPeluqueria.Api.Services.Interfaces
{
    public interface ICajaService
    {
        Task<ApiResponse<SesionCajaResponseDto?>> ObtenerEstadoCajaActualAsync(CancellationToken ct = default);
        Task<ApiResponse<SesionCajaResponseDto>> ObtenerPorIdAsync(int id, CancellationToken ct = default);
        Task<ApiResponse<PagedResult<SesionCajaResponseDto>>> ObtenerHistorialCajasAsync(PaginationFilterDto filter, CancellationToken ct = default);
        Task<ApiResponse<PagedResult<SesionCajaResponseDto>>> ObtenerHistorialCajasAsync(DateTime? desde = null, DateTime? hasta = null, CancellationToken ct = default);
        Task<ApiResponse<SesionCajaResponseDto>> AbrirCajaAsync(AperturaCajaDto dto, int? usuarioEjecutorId = null, CancellationToken ct = default);
        Task<ApiResponse<MovimientoCajaResponseDto>> RegistrarIngresoCajaAsync(int sesionCajaId, decimal monto, string concepto, string tipo = "IngresoManual", int? usuarioEjecutorId = null, CancellationToken ct = default);
        Task<ApiResponse<MovimientoCajaResponseDto>> RegistrarEgresoCajaAsync(int sesionCajaId, decimal monto, string concepto, string tipo = "EgresoMenor", int? usuarioEjecutorId = null, CancellationToken ct = default);
        Task<ApiResponse<SesionCajaResponseDto>> CerrarCajaAsync(CierreCajaDto dto, int? usuarioEjecutorId = null, CancellationToken ct = default);
    }
}
