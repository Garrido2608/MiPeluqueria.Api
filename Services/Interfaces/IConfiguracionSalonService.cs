using MiPeluqueria.Api.DTOs.Common;

namespace MiPeluqueria.Api.Services.Interfaces
{
    public interface IConfiguracionSalonService
    {
        Task<ConfiguracionSalonDto> ObtenerConfiguracionAsync(CancellationToken cancellationToken = default);
        Task<ConfiguracionSalonDto> ActualizarConfiguracionAsync(ConfiguracionSalonDto dto, CancellationToken cancellationToken = default);
    }
}