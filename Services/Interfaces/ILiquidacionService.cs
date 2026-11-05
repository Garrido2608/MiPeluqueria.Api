using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Ventas;
using MiPeluqueria.Api.Models.Ventas;

namespace MiPeluqueria.Api.Services.Interfaces
{
    public interface ILiquidacionService
    {
        Task<ApiResponse<LiquidacionResponseDto>> LiquidarPeriodoAsync(GenerarLiquidacionDto dto, int? usuarioLiquidacionId = null, CancellationToken ct = default);
        Task<ApiResponse<bool>> RegistrarPagoLiquidacionAsync(int liquidacionId, int? sesionCajaId = null, int? usuarioId = null, CancellationToken ct = default);
        Task<ApiResponse<LiquidacionResponseDto>> AnularLiquidacionAsync(int liquidacionId, string? motivo = null, int? usuarioId = null, CancellationToken ct = default);
        Task<ApiResponse<IReadOnlyList<LiquidacionResponseDto>>> ObtenerPorPeluqueroAsync(int peluqueroId, CancellationToken ct = default);
        Task<ApiResponse<PagedResult<LiquidacionResponseDto>>> ObtenerTodasAsync(PaginationFilterDto filter, int? peluqueroId = null, EstadoLiquidacionEnum? estado = null, CancellationToken ct = default);
        Task<ApiResponse<PagedResult<LiquidacionResponseDto>>> ObtenerTodasAsync(int? peluqueroId = null, DateTime? desde = null, DateTime? hasta = null, CancellationToken ct = default);
        Task<ApiResponse<LiquidacionResponseDto>> ObtenerPorIdAsync(int id, CancellationToken ct = default);
    }
}
