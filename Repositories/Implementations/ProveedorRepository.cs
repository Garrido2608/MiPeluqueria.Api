using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MiPeluqueria.Api.Data.Context;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.Models.Stock;
using MiPeluqueria.Api.Repositories.Interfaces;

namespace MiPeluqueria.Api.Repositories.Implementations
{
    public class ProveedorRepository : GenericRepository<Proveedor>, IProveedorRepository
    {
        public ProveedorRepository(ApplicationDbContext context) : base(context)
        {
        }

        public override async Task<IReadOnlyList<Proveedor>> GetAllAsync(CancellationToken ct = default)
        {
            return await _dbSet
                .AsNoTracking()
                .OrderBy(p => p.RazonSocial)
                .ToListAsync(ct);
        }

        public async Task<(IReadOnlyList<Proveedor> Items, int TotalRecords)> GetPagedAsync(
            PaginationFilterDto filter,
            CancellationToken ct = default)
        {
            var query = _dbSet.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim().ToLower();
                query = query.Where(p =>
                    p.RazonSocial.ToLower().Contains(term) ||
                    (p.Cuit != null && p.Cuit.ToLower().Contains(term)) ||
                    (p.Contacto != null && p.Contacto.ToLower().Contains(term)) ||
                    (p.Email != null && p.Email.ToLower().Contains(term)));
            }

            int totalRecords = await query.CountAsync(ct);

            var items = await query
                .OrderBy(p => p.RazonSocial)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync(ct);

            return (items, totalRecords);
        }

        public async Task<bool> ExistsByRazonSocialAsync(string razonSocial, int? excludeId = null, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(razonSocial))
                return false;

            var normalized = razonSocial.Trim().ToLower();
            return await _dbSet
                .AnyAsync(p => p.RazonSocial.ToLower() == normalized && (!excludeId.HasValue || p.Id != excludeId.Value), ct);
        }

        public async Task<bool> ExistsByCuitAsync(string cuit, int? excludeId = null, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(cuit))
                return false;

            var normalized = cuit.Trim().ToLower();
            return await _dbSet
                .AnyAsync(p => p.Cuit != null && p.Cuit.ToLower() == normalized && (!excludeId.HasValue || p.Id != excludeId.Value), ct);
        }

        public async Task<Dictionary<int, int>> GetCantidadProductosPorProveedorAsync(
            IEnumerable<int>? proveedorIds = null,
            CancellationToken ct = default)
        {
            var query = _context.Productos
                .AsNoTracking()
                .Where(p => p.ProveedorId.HasValue);

            if (proveedorIds != null)
            {
                var ids = proveedorIds.Distinct().ToList();
                if (ids.Count == 0)
                    return new Dictionary<int, int>();

                query = query.Where(p => ids.Contains(p.ProveedorId!.Value));
            }

            return await query
                .GroupBy(p => p.ProveedorId!.Value)
                .Select(g => new { ProveedorId = g.Key, Cantidad = g.Count() })
                .ToDictionaryAsync(x => x.ProveedorId, x => x.Cantidad, ct);
        }

        public async Task<int> CountProductosAsociadosAsync(int proveedorId, CancellationToken ct = default)
        {
            return await _context.Productos
                .AsNoTracking()
                .CountAsync(p => p.ProveedorId == proveedorId, ct);
        }

        public async Task<bool> TieneProductosAsociadosAsync(int proveedorId, CancellationToken ct = default)
        {
            return await _context.Productos
                .AsNoTracking()
                .AnyAsync(p => p.ProveedorId == proveedorId, ct);
        }
    }
}
