using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Turnos;

namespace MiPeluqueria.Api.Services.Interfaces
{
    public interface IRecetaServicioService
    {
        Task<ApiResponse<IReadOnlyList<RecetaItemResponseDto>>> ObtenerRecetaPorServicioAsync(int servicioId, CancellationToken ct = default);
        Task<ApiResponse<bool>> GuardarRecetaServicioAsync(ConfigurarRecetaServicioDto dto, int usuarioId, CancellationToken ct = default);
        Task<ApiResponse<int>> AplicarConsumoGabinetePorTurnoAsync(int turnoId, int usuarioId, CancellationToken ct = default);
    }
}