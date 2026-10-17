using MiPeluqueria.Api.Models.Clientes;
using MiPeluqueria.Api.Models.Seguridad;
using MiPeluqueria.Api.Models.Stock;
using MiPeluqueria.Api.Models.Turnos;
using MiPeluqueria.Api.Models.Ventas;

namespace MiPeluqueria.Api.Repositories.Interfaces
{
    public interface ICatalogoRepository
    {
        Task<IReadOnlyList<TipoCabello>> GetTiposCabelloAsync(CancellationToken ct = default);
        Task<IReadOnlyList<TipoRostro>> GetTiposRostroAsync(CancellationToken ct = default);
        Task<IReadOnlyList<CategoriaProducto>> GetCategoriasProductoAsync(CancellationToken ct = default);
        Task<IReadOnlyList<CategoriaServicio>> GetCategoriasServicioAsync(CancellationToken ct = default);
        Task<IReadOnlyList<Especialidad>> GetEspecialidadesAsync(CancellationToken ct = default);
        Task<IReadOnlyList<EstadoTurno>> GetEstadosTurnoAsync(CancellationToken ct = default);
        Task<IReadOnlyList<MotivoCancelacion>> GetMotivosCancelacionAsync(CancellationToken ct = default);
        Task<IReadOnlyList<MedioPago>> GetMediosPagoAsync(CancellationToken ct = default);
        Task<IReadOnlyList<Rol>> GetRolesAsync(CancellationToken ct = default);

        Task AddEntityAsync<TEntity>(TEntity entity, CancellationToken ct = default) where TEntity : class;
    }
}