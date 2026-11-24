using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MiPeluqueria.Api.Data.Context;
using MiPeluqueria.Api.Models.Clientes;
using MiPeluqueria.Api.Models.Common;
using MiPeluqueria.Api.Models.Seguridad;
using MiPeluqueria.Api.Models.Stock;
using MiPeluqueria.Api.Models.Turnos;
using MiPeluqueria.Api.Models.Ventas;
using MiPeluqueria.Api.Repositories.Interfaces;

namespace MiPeluqueria.Api.Repositories.Implementations
{
    public class CatalogoRepository : GenericRepository<Especialidad>, ICatalogoRepository
    {
        public CatalogoRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IReadOnlyList<TipoCabello>> GetTiposCabelloAsync(CancellationToken ct = default) =>
            await _context.TiposCabello.AsNoTracking().OrderBy(x => x.Nombre).ToListAsync(ct);

        public async Task<IReadOnlyList<TipoRostro>> GetTiposRostroAsync(CancellationToken ct = default) =>
            await _context.TiposRostro.AsNoTracking().OrderBy(x => x.Nombre).ToListAsync(ct);

        public async Task<IReadOnlyList<CategoriaProducto>> GetCategoriasProductoAsync(CancellationToken ct = default) =>
            await _context.CategoriasProducto.AsNoTracking().OrderBy(x => x.Nombre).ToListAsync(ct);

        public async Task<IReadOnlyList<CategoriaServicio>> GetCategoriasServicioAsync(CancellationToken ct = default) =>
            await _context.CategoriasServicio.AsNoTracking().OrderBy(x => x.Nombre).ToListAsync(ct);

        public async Task<IReadOnlyList<Especialidad>> GetEspecialidadesAsync(CancellationToken ct = default) =>
            await _context.Especialidades.AsNoTracking().OrderBy(x => x.Nombre).ToListAsync(ct);

        public async Task<IReadOnlyList<EstadoTurno>> GetEstadosTurnoAsync(CancellationToken ct = default) =>
            await _context.EstadosTurno.AsNoTracking().OrderBy(x => x.Id).ToListAsync(ct);

        public async Task<IReadOnlyList<MotivoCancelacion>> GetMotivosCancelacionAsync(CancellationToken ct = default) =>
            await _context.MotivosCancelacion.AsNoTracking().OrderBy(x => x.Descripcion).ToListAsync(ct);

        public async Task<IReadOnlyList<MedioPago>> GetMediosPagoAsync(CancellationToken ct = default) =>
            await _context.MediosPago.AsNoTracking().OrderBy(x => x.Nombre).ToListAsync(ct);

        public async Task<IReadOnlyList<Rol>> GetRolesAsync(CancellationToken ct = default) =>
            await _context.Roles.AsNoTracking().OrderBy(x => x.Nombre).ToListAsync(ct);

        public async Task<TipoCabello?> GetTipoCabelloByIdAsync(int id, CancellationToken ct = default) =>
            await _context.TiposCabello.FirstOrDefaultAsync(x => x.Id == id, ct);

        public async Task<TipoRostro?> GetTipoRostroByIdAsync(int id, CancellationToken ct = default) =>
            await _context.TiposRostro.FirstOrDefaultAsync(x => x.Id == id, ct);

        public async Task<CategoriaProducto?> GetCategoriaProductoByIdAsync(int id, CancellationToken ct = default) =>
            await _context.CategoriasProducto.FirstOrDefaultAsync(x => x.Id == id, ct);

        public async Task<CategoriaServicio?> GetCategoriaServicioByIdAsync(int id, CancellationToken ct = default) =>
            await _context.CategoriasServicio.FirstOrDefaultAsync(x => x.Id == id, ct);

        public async Task<Especialidad?> GetEspecialidadByIdAsync(int id, CancellationToken ct = default) =>
            await _context.Especialidades.FirstOrDefaultAsync(x => x.Id == id, ct);

        public async Task<MotivoCancelacion?> GetMotivoCancelacionByIdAsync(int id, CancellationToken ct = default) =>
            await _context.MotivosCancelacion.FirstOrDefaultAsync(x => x.Id == id, ct);

        public async Task<MedioPago?> GetMedioPagoByIdAsync(int id, CancellationToken ct = default) =>
            await _context.MediosPago.FirstOrDefaultAsync(x => x.Id == id, ct);

        public async Task<TEntity?> GetCatalogEntityByIdAsync<TEntity>(int id, CancellationToken ct = default)
            where TEntity : AuditableEntity
        {
            return await _context.Set<TEntity>().FirstOrDefaultAsync(e => e.Id == id, ct);
        }

        public async Task AddEntityAsync<TEntity>(TEntity entity, CancellationToken ct = default) where TEntity : class
        {
            if (entity is AuditableEntity auditable)
                auditable.CreatedAt = DateTime.UtcNow;

            await _context.Set<TEntity>().AddAsync(entity, ct);
        }

        public void UpdateEntity<TEntity>(TEntity entity) where TEntity : AuditableEntity
        {
            entity.UpdatedAt = DateTime.UtcNow;
            _context.Set<TEntity>().Update(entity);
        }

        public void DeleteEntity<TEntity>(TEntity entity) where TEntity : AuditableEntity
        {
            entity.IsDeleted = true;
            entity.DeletedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;
            _context.Set<TEntity>().Update(entity);
        }
    }
}
