using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MiPeluqueria.Api.Data.Context;
using MiPeluqueria.Api.Models.Stock;
using MiPeluqueria.Api.Repositories.Interfaces;

namespace MiPeluqueria.Api.Repositories.Implementations
{
    public class OrdenCompraRepository : IOrdenCompraRepository
    {
        private readonly ApplicationDbContext _context;

        public OrdenCompraRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<OrdenCompra>> GetAllAsync(
            int? proveedorId = null,
            EstadoOrdenCompraEnum? estado = null,
            CancellationToken ct = default)
        {
            var query = _context.Set<OrdenCompra>()
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
                .ToListAsync(ct);
        }

        public async Task<OrdenCompra?> GetByIdCompletaAsync(int id, CancellationToken ct = default)
        {
            return await _context.Set<OrdenCompra>()
                .Include(o => o.Proveedor)
                .Include(o => o.Detalles)
                    .ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(o => o.Id == id, ct);
        }

        public async Task AddAsync(OrdenCompra orden, CancellationToken ct = default)
        {
            await _context.Set<OrdenCompra>().AddAsync(orden, ct);
        }

        public void Update(OrdenCompra orden)
        {
            _context.Set<OrdenCompra>().Update(orden);
        }
    }
}