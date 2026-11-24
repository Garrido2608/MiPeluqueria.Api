using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.SaaS;

namespace MiPeluqueria.Api.Services.Interfaces
{
    public interface ISaaSService
    {
        Task<ApiResponse<IReadOnlyList<SucursalResponseDto>>> ObtenerSucursalesAsync(CancellationToken ct = default);
        Task<ApiResponse<SucursalResponseDto>> CrearSucursalAsync(CrearSucursalDto dto, CancellationToken ct = default);
        Task<ApiResponse<IReadOnlyList<PlanSuscripcionResponseDto>>> ObtenerPlanesActivosAsync(CancellationToken ct = default);
        Task<ApiResponse<SuscripcionEstadoResponseDto>> ObtenerMiSuscripcionAsync(CancellationToken ct = default);
        Task<ApiResponse<SuscripcionEstadoResponseDto>> CambiarPlanAsync(CambiarPlanSuscripcionDto dto, CancellationToken ct = default);
    }
}
