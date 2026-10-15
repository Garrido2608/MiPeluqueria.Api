using System;
using System.Threading.Tasks;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Dashboard;

namespace MiPeluqueria.Api.Services.Interfaces
{
    public interface IDashboardService
    {
        Task<ApiResponse<DashboardKpiResponseDto>> ObtenerMetricasMesActualAsync();
        Task<ApiResponse<DashboardKpiResponseDto>> ObtenerKpisAsync(DateTime? fechaDesde = null, DateTime? fechaHasta = null);
    }
}