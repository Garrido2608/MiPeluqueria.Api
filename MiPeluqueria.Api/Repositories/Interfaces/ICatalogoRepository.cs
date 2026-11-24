using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MiPeluqueria.Api.Models.Clientes;
using MiPeluqueria.Api.Models.Common;
using MiPeluqueria.Api.Models.Seguridad;
using MiPeluqueria.Api.Models.Stock;
using MiPeluqueria.Api.Models.Turnos;
using MiPeluqueria.Api.Models.Ventas;

namespace MiPeluqueria.Api.Repositories.Interfaces
{
    public interface ICatalogoRepository : IGenericRepository<Especialidad>
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

        Task<TipoCabello?> GetTipoCabelloByIdAsync(int id, CancellationToken ct = default);
        Task<TipoRostro?> GetTipoRostroByIdAsync(int id, CancellationToken ct = default);
        Task<CategoriaProducto?> GetCategoriaProductoByIdAsync(int id, CancellationToken ct = default);
        Task<CategoriaServicio?> GetCategoriaServicioByIdAsync(int id, CancellationToken ct = default);
        Task<Especialidad?> GetEspecialidadByIdAsync(int id, CancellationToken ct = default);
        Task<MotivoCancelacion?> GetMotivoCancelacionByIdAsync(int id, CancellationToken ct = default);
        Task<MedioPago?> GetMedioPagoByIdAsync(int id, CancellationToken ct = default);

        Task<TEntity?> GetCatalogEntityByIdAsync<TEntity>(int id, CancellationToken ct = default) where TEntity : AuditableEntity;
        Task AddEntityAsync<TEntity>(TEntity entity, CancellationToken ct = default) where TEntity : class;
        void UpdateEntity<TEntity>(TEntity entity) where TEntity : AuditableEntity;
        void DeleteEntity<TEntity>(TEntity entity) where TEntity : AuditableEntity;
    }
}
