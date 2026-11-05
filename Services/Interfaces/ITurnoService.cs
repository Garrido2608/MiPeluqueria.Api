using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Turnos;

namespace MiPeluqueria.Api.Services.Interfaces
{
    public interface ITurnoService
    {
        Task<ApiResponse<PagedResult<TurnoResponseDto>>> GetAllAsync(
            PaginationFilterDto filter,
            DateTime? fecha = null,
            int? peluqueroId = null,
            int? estadoId = null,
            int? clienteId = null,
            CancellationToken ct = default);

        Task<ApiResponse<PagedResult<TurnoResponseDto>>> GetAllAsync(
            DateTime? fecha = null,
            int? peluqueroId = null,
            CancellationToken ct = default);

        Task<ApiResponse<TurnoResponseDto>> GetByIdAsync(int id, CancellationToken ct = default);

        Task<ApiResponse<TurnoResponseDto>> CreateAsync(
            CrearTurnoDto dto,
            int usuarioId = 1,
            CancellationToken ct = default);

        Task<ApiResponse<TurnoResponseDto>> ReprogramarAsync(
            int id,
            ReprogramarTurnoDto dto,
            int usuarioId = 1,
            string? rolEjecutor = null,
            CancellationToken ct = default);

        Task<ApiResponse<bool>> CambiarEstadoAsync(
            int id,
            CambiarEstadoTurnoDto dto,
            int usuarioId = 1,
            string? rolEjecutor = null,
            CancellationToken ct = default);

        Task<ApiResponse<bool>> ProcesarPagoSenaWebhookAsync(
            int turnoId,
            WebhookPagoNotificacionDto payload,
            string proveedor = "MercadoPago",
            CancellationToken ct = default);

        Task<ApiResponse<DisponibilidadHorarioResponseDto>> ObtenerHorariosDisponiblesAsync(
            int peluqueroId,
            DateTime fecha,
            List<int>? serviciosIds = null,
            CancellationToken ct = default);

        Task<ApiResponse<IReadOnlyList<TurnoResponseDto>>> ObtenerTurnosPorClienteAsync(
            int clienteId,
            CancellationToken ct = default);

        Task<ApiResponse<IReadOnlyList<HistorialEstadoTurnoResponseDto>>> ObtenerHistorialEstadoTurnoAsync(
            int turnoId,
            CancellationToken ct = default);
    }
}
