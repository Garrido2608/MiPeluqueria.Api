using MiPeluqueria.Api.DTOs.Common;

namespace MiPeluqueria.Api.Services.Interfaces
{
    public interface ICatalogoService
    {
        Task<ApiResponse<CatalogosMaestrosResponseDto>> ObtenerTodosLosCatalogosAsync();
        Task<ApiResponse<IEnumerable<CatalogoItemDto>>> ObtenerTiposCabelloAsync();
        Task<ApiResponse<IEnumerable<CatalogoItemDto>>> ObtenerTiposRostroAsync();
        Task<ApiResponse<IEnumerable<CatalogoItemDto>>> ObtenerCategoriasServicioAsync();
        Task<ApiResponse<IEnumerable<CatalogoItemDto>>> ObtenerEspecialidadesAsync();
        Task<ApiResponse<IEnumerable<CatalogoItemDto>>> ObtenerEstadosTurnoAsync();
        Task<ApiResponse<IEnumerable<CatalogoItemDto>>> ObtenerMotivosCancelacionAsync();
        Task<ApiResponse<IEnumerable<CatalogoItemDto>>> ObtenerCategoriasProductoAsync();
        Task<ApiResponse<IEnumerable<CatalogoItemDto>>> ObtenerMediosPagoAsync();
        Task<ApiResponse<IEnumerable<CatalogoItemDto>>> ObtenerRolesAsync();

        Task<ApiResponse<CatalogoItemDto>> CrearCategoriaServicioAsync(CrearCatalogoItemDto dto);
        Task<ApiResponse<CatalogoItemDto>> CrearEspecialidadAsync(CrearCatalogoItemDto dto);
        Task<ApiResponse<CatalogoItemDto>> CrearCategoriaProductoAsync(CrearCatalogoItemDto dto);
        Task<ApiResponse<CatalogoItemDto>> CrearMedioPagoAsync(CrearCatalogoItemDto dto);
        Task<ApiResponse<CatalogoItemDto>> CrearMotivoCancelacionAsync(CrearCatalogoItemDto dto);
        Task<ApiResponse<bool>> EliminarEspecialidadAsync(int id);
    }
}