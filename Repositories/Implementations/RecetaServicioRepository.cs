using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MiPeluqueria.Api.Data.Context;
using MiPeluqueria.Api.Models.Stock;
using MiPeluqueria.Api.Models.Turnos;
using MiPeluqueria.Api.Repositories.Interfaces;

namespace MiPeluqueria.Api.Repositories.Implementations
{
    public class RecetaServicioRepository : GenericRepository<ServicioInsumoReceta>, IRecetaServicioRepository
    {
        public RecetaServicioRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IReadOnlyList<ServicioInsumoReceta>> GetByServicioIdWithDetailsAsync(
            int servicioId,
            bool tracking = false,
            CancellationToken ct = default)
        {
            var query = tracking ? _dbSet.AsQueryable() : _dbSet.AsNoTracking();

            return await query
                .Include(r => r.Servicio)
                .Include(r => r.Producto)
                .Where(r => r.ServicioId == servicioId)
                .OrderBy(r => r.Id)
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<ServicioInsumoReceta>> GetByServiciosIdsAsync(
            IEnumerable<int> serviciosIds,
            bool soloDescuentoAutomatico = true,
            CancellationToken ct = default)
        {
            if (serviciosIds == null)
                return Array.Empty<ServicioInsumoReceta>();

            var ids = serviciosIds.Distinct().ToList();
            if (ids.Count == 0)
                return Array.Empty<ServicioInsumoReceta>();

            var query = _dbSet
                .AsNoTracking()
                .Include(r => r.Servicio)
                .Include(r => r.Producto)
                .Where(r => ids.Contains(r.ServicioId));

            if (soloDescuentoAutomatico)
                query = query.Where(r => r.DescuentoAutomaticoAlCompletarTurno);

            return await query.ToListAsync(ct);
        }

        public async Task AddRangeAsync(IEnumerable<ServicioInsumoReceta> items, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            var list = items.ToList();
            foreach (var item in list)
            {
                item.CreatedAt = now;
            }

            if (list.Count > 0)
                await _dbSet.AddRangeAsync(list, ct);
        }

        public async Task<bool> ExisteConsumoGabinetePorTurnoAsync(int turnoId, CancellationToken ct = default)
        {
            string marcadorEspacio = $"Turno #{turnoId} ";
            string marcadorParentesis = $"Turno #{turnoId})";
            string marcadorExacto = $"Turno #{turnoId}";

            return await _context.MovimientosStock
                .AnyAsync(m => m.TipoMovimiento == TipoMovimientoStockEnum.ConsumoInternoSalon
                            && m.Motivo != null
                            && (m.Motivo.Contains(marcadorEspacio)
                                || m.Motivo.Contains(marcadorParentesis)
                                || m.Motivo.EndsWith(marcadorExacto)), ct);
        }
    }
}
