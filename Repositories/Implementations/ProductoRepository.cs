using System;
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
    public class ProductoRepository : GenericRepository<Producto>, IProductoRepository
    {
        public ProductoRepository(ApplicationDbContext context) : base(context)
        {
        }

        public override async Task<IReadOnlyList<Producto>> GetAllAsync(CancellationToken ct = default)
        {
            return await GetAllWithDetailsAsync(false, ct);
        }

        public async Task<IReadOnlyList<Producto>> GetAllWithDetailsAsync(bool soloBajoStock = false, CancellationToken ct = default)
        {
            var query = _dbSet
                .AsNoTracking()
                .Include(p => p.Categoria)
                .Include(p => p.Proveedor)
                .AsQueryable();

            if (soloBajoStock)
                query = query.Where(p => p.StockActual <= p.StockMinimoAlerta);

            return await query
                .OrderBy(p => p.Nombre)
                .ToListAsync(ct);
        }

        public async Task<(IReadOnlyList<Producto> Items, int TotalRecords)> GetPagedAsync(
            PaginationFilterDto filter,
            bool soloBajoStock = false,
            int? categoriaId = null,
            int? proveedorId = null,
            CancellationToken ct = default)
        {
            var query = _dbSet
                .AsNoTracking()
                .Include(p => p.Categoria)
                .Include(p => p.Proveedor)
                .AsQueryable();

            if (soloBajoStock)
                query = query.Where(p => p.StockActual <= p.StockMinimoAlerta);

            if (categoriaId.HasValue)
                query = query.Where(p => p.CategoriaId == categoriaId.Value);

            if (proveedorId.HasValue)
                query = query.Where(p => p.ProveedorId == proveedorId.Value);

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim().ToLower();
                query = query.Where(p =>
                    p.Nombre.ToLower().Contains(term) ||
                    (p.CodigoBarras != null && p.CodigoBarras.ToLower().Contains(term)) ||
                    (p.Categoria != null && p.Categoria.Nombre.ToLower().Contains(term)));
            }

            int totalRecords = await query.CountAsync(ct);

            var items = await query
                .OrderBy(p => p.Nombre)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync(ct);

            return (items, totalRecords);
        }

        public async Task<IReadOnlyList<Producto>> GetStockCriticoAsync(CancellationToken ct = default)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(p => p.Categoria)
                .Include(p => p.Proveedor)
                .Where(p => p.StockActual <= p.StockMinimoAlerta)
                .OrderBy(p => p.StockActual)
                .ThenBy(p => p.Nombre)
                .ToListAsync(ct);
        }

        public override async Task<Producto?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            return await GetByIdWithDetailsAsync(id, true, ct);
        }

        public async Task<Producto?> GetByIdWithDetailsAsync(int id, bool tracking = false, CancellationToken ct = default)
        {
            var query = tracking ? _dbSet.AsQueryable() : _dbSet.AsNoTracking();
            return await query
                .Include(p => p.Categoria)
                .Include(p => p.Proveedor)
                .FirstOrDefaultAsync(p => p.Id == id, ct);
        }

        public async Task<bool> ExistsCodigoBarraAsync(string codigoBarra, int? excludeId = null, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(codigoBarra))
                return false;

            var norm = codigoBarra.Trim().ToLower();
            return await _dbSet.AnyAsync(
                p => p.CodigoBarras != null && p.CodigoBarras.ToLower() == norm && (!excludeId.HasValue || p.Id != excludeId.Value),
                ct);
        }

        public async Task RegistrarMovimientoAsync(MovimientoStock movimiento, CancellationToken ct = default)
        {
            movimiento.CreatedAt = DateTime.UtcNow;
            if (movimiento.Fecha == default)
                movimiento.Fecha = DateTime.UtcNow;

            await _context.MovimientosStock.AddAsync(movimiento, ct);
        }

        public async Task<(IReadOnlyList<MovimientoStock> Items, int TotalRecords)> GetKardexPagedAsync(
            int productoId,
            PaginationFilterDto filter,
            CancellationToken ct = default)
        {
            var query = _context.MovimientosStock
                .AsNoTracking()
                .Include(m => m.Producto)
                .Where(m => m.ProductoId == productoId);

            if (filter.FechaDesde.HasValue)
                query = query.Where(m => m.Fecha >= filter.FechaDesde.Value);

            if (filter.FechaHasta.HasValue)
                query = query.Where(m => m.Fecha <= filter.FechaHasta.Value);

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim().ToLower();
                query = query.Where(m => m.Motivo != null && m.Motivo.ToLower().Contains(term));
            }

            int totalRecords = await query.CountAsync(ct);

            var items = await query
                .OrderByDescending(m => m.Fecha)
                .ThenByDescending(m => m.Id)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync(ct);

            return (items, totalRecords);
        }

        public async Task<IReadOnlyList<MovimientoStock>> GetKardexByProductoIdAsync(int productoId, CancellationToken ct = default)
        {
            return await _context.MovimientosStock
                .AsNoTracking()
                .Include(m => m.Producto)
                .Where(m => m.ProductoId == productoId)
                .OrderByDescending(m => m.Fecha)
                .ThenByDescending(m => m.Id)
                .ToListAsync(ct);
        }

        public async Task AddHistorialPrecioAsync(HistorialPrecioProducto historial, CancellationToken ct = default)
        {
            historial.CreatedAt = DateTime.UtcNow;
            await _context.HistorialPreciosProducto.AddAsync(historial, ct);
        }

        public async Task<IReadOnlyList<HistorialPrecioProducto>> GetHistorialPreciosAsync(int productoId, CancellationToken ct = default)
        {
            return await _context.HistorialPreciosProducto
                .AsNoTracking()
                .Where(h => h.ProductoId == productoId)
                .OrderByDescending(h => h.FechaCambio)
                .ThenByDescending(h => h.Id)
                .ToListAsync(ct);
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

        public Task UpdateAsync(Producto producto, CancellationToken ct = default)
        {
            Update(producto);
            return Task.CompletedTask;
        }

        public async Task DeleteAsync(int id, CancellationToken ct = default)
        {
            var producto = await _dbSet.FirstOrDefaultAsync(p => p.Id == id, ct);
            if (producto != null)
            {
                Delete(producto);
            }
        }
    }
}
