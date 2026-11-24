using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Ventas;

namespace MiPeluqueria.Api.Services.Interfaces
{
    public interface IComisionConfigService
    {
        Task<ApiResponse<IReadOnlyList<ComisionConfigDto>>> ObtenerTodasAsync(CancellationToken ct = default);
        Task<ApiResponse<IReadOnlyList<ComisionConfigDto>>> ObtenerPorPeluqueroAsync(int peluqueroId, CancellationToken ct = default);
        Task<ApiResponse<ComisionConfigDto>> GuardarConfiguracionAsync(GuardarComisionConfigDto dto, CancellationToken ct = default);
        Task<ApiResponse<ComisionConfigDto>> ActualizarConfiguracionAsync(int id, GuardarComisionConfigDto dto, CancellationToken ct = default);
        Task<ApiResponse<bool>> EliminarConfiguracionAsync(int id, CancellationToken ct = default);
    }
}
