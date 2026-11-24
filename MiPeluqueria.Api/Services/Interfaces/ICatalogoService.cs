using System.Collections.Generic;
using System.Threading.Tasks;
using MiPeluqueria.Api.DTOs.Common;

namespace MiPeluqueria.Api.Services.Interfaces
{
    public interface ICatalogoService
    {
        Task<ApiResponse<CatalogosMaestrosResponseDto>> ObtenerTodosLosCatalogosAsync();

        // 1. Tipos de Cabello (CRUD Completo)
        Task<ApiResponse<IEnumerable<CatalogoItemDto>>> ObtenerTiposCabelloAsync();
        Task<ApiResponse<CatalogoItemDto>> CrearTipoCabelloAsync(CrearCatalogoItemDto dto);
        Task<ApiResponse<CatalogoItemDto>> ActualizarTipoCabelloAsync(int id, ActualizarCatalogoItemDto dto);
        Task<ApiResponse<bool>> EliminarTipoCabelloAsync(int id);

        // 2. Tipos de Rostro (CRUD Completo)
        Task<ApiResponse<IEnumerable<CatalogoItemDto>>> ObtenerTiposRostroAsync();
        Task<ApiResponse<CatalogoItemDto>> CrearTipoRostroAsync(CrearCatalogoItemDto dto);
        Task<ApiResponse<CatalogoItemDto>> ActualizarTipoRostroAsync(int id, ActualizarCatalogoItemDto dto);
        Task<ApiResponse<bool>> EliminarTipoRostroAsync(int id);

        // 3. Categorías de Servicio (CRUD Completo)
        Task<ApiResponse<IEnumerable<CatalogoItemDto>>> ObtenerCategoriasServicioAsync();
        Task<ApiResponse<CatalogoItemDto>> CrearCategoriaServicioAsync(CrearCatalogoItemDto dto);
        Task<ApiResponse<CatalogoItemDto>> ActualizarCategoriaServicioAsync(int id, ActualizarCatalogoItemDto dto);
        Task<ApiResponse<bool>> EliminarCategoriaServicioAsync(int id);

        // 4. Especialidades (CRUD Completo)
        Task<ApiResponse<IEnumerable<CatalogoItemDto>>> ObtenerEspecialidadesAsync();
        Task<ApiResponse<CatalogoItemDto>> CrearEspecialidadAsync(CrearCatalogoItemDto dto);
        Task<ApiResponse<CatalogoItemDto>> ActualizarEspecialidadAsync(int id, ActualizarCatalogoItemDto dto);
        Task<ApiResponse<bool>> EliminarEspecialidadAsync(int id);

        // 5. Estados de Turno (Lectura Global)
        Task<ApiResponse<IEnumerable<CatalogoItemDto>>> ObtenerEstadosTurnoAsync();

        // 6. Motivos de Cancelación (CRUD Completo)
        Task<ApiResponse<IEnumerable<CatalogoItemDto>>> ObtenerMotivosCancelacionAsync();
        Task<ApiResponse<CatalogoItemDto>> CrearMotivoCancelacionAsync(CrearCatalogoItemDto dto);
        Task<ApiResponse<CatalogoItemDto>> ActualizarMotivoCancelacionAsync(int id, ActualizarCatalogoItemDto dto);
        Task<ApiResponse<bool>> EliminarMotivoCancelacionAsync(int id);

        // 7. Categorías de Producto (CRUD Completo)
        Task<ApiResponse<IEnumerable<CatalogoItemDto>>> ObtenerCategoriasProductoAsync();
        Task<ApiResponse<CatalogoItemDto>> CrearCategoriaProductoAsync(CrearCatalogoItemDto dto);
        Task<ApiResponse<CatalogoItemDto>> ActualizarCategoriaProductoAsync(int id, ActualizarCatalogoItemDto dto);
        Task<ApiResponse<bool>> EliminarCategoriaProductoAsync(int id);

        // 8. Medios de Pago (CRUD Completo con EsEfectivoFisico)
        Task<ApiResponse<IEnumerable<CatalogoItemDto>>> ObtenerMediosPagoAsync();
        Task<ApiResponse<CatalogoItemDto>> CrearMedioPagoAsync(CrearCatalogoItemDto dto);
        Task<ApiResponse<CatalogoItemDto>> ActualizarMedioPagoAsync(int id, ActualizarCatalogoItemDto dto);
        Task<ApiResponse<bool>> EliminarMedioPagoAsync(int id);

        // 9. Roles del Sistema (Lectura Global)
        Task<ApiResponse<IEnumerable<CatalogoItemDto>>> ObtenerRolesAsync();
    }
}
