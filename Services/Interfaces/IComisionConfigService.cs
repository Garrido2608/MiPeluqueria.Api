using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Ventas;

namespace MiPeluqueria.Api.Services.Interfaces
{
    public interface IComisionConfigService
    {
        Task<ApiResponse<IEnumerable<ComisionConfigDto>>> ObtenerTodasAsync();
        Task<ApiResponse<IEnumerable<ComisionConfigDto>>> ObtenerPorPeluqueroAsync(int peluqueroId);
        Task<ApiResponse<ComisionConfigDto>> GuardarConfiguracionAsync(GuardarComisionConfigDto dto);
        Task<ApiResponse<ComisionConfigDto>> ActualizarConfiguracionAsync(int id, GuardarComisionConfigDto dto);
        Task<ApiResponse<bool>> EliminarConfiguracionAsync(int id);
    }
}