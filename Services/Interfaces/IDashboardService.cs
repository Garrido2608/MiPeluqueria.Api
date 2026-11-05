using System;
using System.Threading;
using System.Threading.Tasks;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Dashboard;

namespace MiPeluqueria.Api.Services.Interfaces
{
    public interface IDashboardService
    {
        Task<ApiResponse<DashboardKpiResponseDto>> ObtenerMetricasMesActualAsync(CancellationToken ct = default);
        Task<ApiResponse<DashboardKpiResponseDto>> ObtenerKpisAsync(DateTime? fechaDesde = null, DateTime? fechaHasta = null, CancellationToken ct = default);
    }
}
