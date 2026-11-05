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
    public class OrdenCompraRepository : GenericRepository<OrdenCompra>, IOrdenCompraRepository
    {
        public OrdenCompraRepository(ApplicationDbContext context) : base(context)
        {
        }

        public override async Task<IReadOnlyList<OrdenCompra>> GetAllAsync(CancellationToken ct = default)
        {
            return await GetAllAsync(null, null, ct);
        }

        public async Task<IReadOnlyList<OrdenCompra>> GetAllAsync(
            int? proveedorId,
            EstadoOrdenCompraEnum? estado = null,
            CancellationToken ct = default)
        {
            var query = _dbSet
                .Include(o => o.Proveedor)
                .Include(o => o.Detalles)
                    .ThenInclude(d => d.Producto)
                .AsNoTracking()
                .AsQueryable();

            if (proveedorId.HasValue)
                query = query.Where(o => o.ProveedorId == proveedorId.Value);

            if (estado.HasValue)
                query = query.Where(o => o.Estado == estado.Value);

            return await query
                .OrderByDescending(o => o.FechaEmision)
                .ThenByDescending(o => o.Id)
                .ToListAsync(ct);
        }

        public async Task<(IReadOnlyList<OrdenCompra> Items, int TotalRecords)> GetPagedAsync(
            PaginationFilterDto filter,
            int? proveedorId = null,
            EstadoOrdenCompraEnum? estado = null,
            CancellationToken ct = default)
        {
            var query = _dbSet
                .Include(o => o.Proveedor)
                .Include(o => o.Detalles)
                    .ThenInclude(d => d.Producto)
                .AsNoTracking()
                .AsQueryable();

            if (proveedorId.HasValue)
                query = query.Where(o => o.ProveedorId == proveedorId.Value);

            if (estado.HasValue)
                query = query.Where(o => o.Estado == estado.Value);

            if (filter.FechaDesde.HasValue)
                query = query.Where(o => o.FechaEmision >= filter.FechaDesde.Value);

            if (filter.FechaHasta.HasValue)
                query = query.Where(o => o.FechaEmision <= filter.FechaHasta.Value);

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim().ToLower();
                query = query.Where(o =>
                    (o.NumeroComprobanteProveedor != null && o.NumeroComprobanteProveedor.ToLower().Contains(term)) ||
                    (o.Proveedor != null && o.Proveedor.RazonSocial.ToLower().Contains(term)) ||
                    (o.Observaciones != null && o.Observaciones.ToLower().Contains(term)));
            }

            int totalRecords = await query.CountAsync(ct);

            var items = await query
                .OrderByDescending(o => o.FechaEmision)
                .ThenByDescending(o => o.Id)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync(ct);

            return (items, totalRecords);
        }

        public async Task<OrdenCompra?> GetByIdCompletaAsync(int id, CancellationToken ct = default)
        {
            return await _dbSet
                .Include(o => o.Proveedor)
                .Include(o => o.Detalles)
                    .ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(o => o.Id == id, ct);
        }
    }
}
