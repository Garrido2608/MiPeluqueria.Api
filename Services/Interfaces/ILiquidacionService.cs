using System.Collections.Generic;
using System.Threading.Tasks;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Ventas;

namespace MiPeluqueria.Api.Services.Interfaces
{
    public interface ILiquidacionService
    {
        Task<ApiResponse<LiquidacionResponseDto>> LiquidarPeriodoAsync(GenerarLiquidacionDto dto);
        Task<ApiResponse<bool>> RegistrarPagoLiquidacionAsync(int liquidacionId);
        Task<ApiResponse<List<LiquidacionResponseDto>>> ObtenerPorPeluqueroAsync(int peluqueroId);
    }
}