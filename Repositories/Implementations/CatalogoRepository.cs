using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MiPeluqueria.Api.Data.Context;
using MiPeluqueria.Api.Models.Clientes;
using MiPeluqueria.Api.Models.Seguridad;
using MiPeluqueria.Api.Models.Stock;
using MiPeluqueria.Api.Models.Turnos;
using MiPeluqueria.Api.Models.Ventas;
using MiPeluqueria.Api.Repositories.Interfaces;

namespace MiPeluqueria.Api.Repositories.Implementations
{
    public class CatalogoRepository : ICatalogoRepository
    {
        private readonly ApplicationDbContext _context;

        public CatalogoRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<TipoCabello>> GetTiposCabelloAsync(CancellationToken ct = default) =>
            await _context.Set<TipoCabello>().AsNoTracking().OrderBy(x => x.Nombre).ToListAsync(ct);

        public async Task<IReadOnlyList<TipoRostro>> GetTiposRostroAsync(CancellationToken ct = default) =>
            await _context.Set<TipoRostro>().AsNoTracking().OrderBy(x => x.Nombre).ToListAsync(ct);

        public async Task<IReadOnlyList<CategoriaProducto>> GetCategoriasProductoAsync(CancellationToken ct = default) =>
            await _context.Set<CategoriaProducto>().AsNoTracking().OrderBy(x => x.Nombre).ToListAsync(ct);

        public async Task<IReadOnlyList<CategoriaServicio>> GetCategoriasServicioAsync(CancellationToken ct = default) =>
            await _context.Set<CategoriaServicio>().AsNoTracking().OrderBy(x => x.Nombre).ToListAsync(ct);

        public async Task<IReadOnlyList<Especialidad>> GetEspecialidadesAsync(CancellationToken ct = default) =>
            await _context.Set<Especialidad>().AsNoTracking().OrderBy(x => x.Nombre).ToListAsync(ct);

        public async Task<IReadOnlyList<EstadoTurno>> GetEstadosTurnoAsync(CancellationToken ct = default) =>
            await _context.Set<EstadoTurno>().AsNoTracking().OrderBy(x => x.Id).ToListAsync(ct);

        public async Task<IReadOnlyList<MotivoCancelacion>> GetMotivosCancelacionAsync(CancellationToken ct = default) =>
            await _context.Set<MotivoCancelacion>().AsNoTracking().OrderBy(x => x.Descripcion).ToListAsync(ct);

        public async Task<IReadOnlyList<MedioPago>> GetMediosPagoAsync(CancellationToken ct = default) =>
            await _context.Set<MedioPago>().AsNoTracking().OrderBy(x => x.Nombre).ToListAsync(ct);

        public async Task<IReadOnlyList<Rol>> GetRolesAsync(CancellationToken ct = default) =>
            await _context.Set<Rol>().AsNoTracking().OrderBy(x => x.Nombre).ToListAsync(ct);

        public async Task AddEntityAsync<TEntity>(TEntity entity, CancellationToken ct = default) where TEntity : class
        {
            await _context.Set<TEntity>().AddAsync(entity, ct);
        }
    }
}