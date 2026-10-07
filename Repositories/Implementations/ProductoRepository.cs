using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MiPeluqueria.Api.Data.Context;
using MiPeluqueria.Api.Models.Stock;
using MiPeluqueria.Api.Repositories.Interfaces;

namespace MiPeluqueria.Api.Repositories.Implementations
{
    public class ProductoRepository : IProductoRepository
    {
        private readonly ApplicationDbContext _context;

        public ProductoRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Producto>> GetAllAsync() =>
            await _context.Productos
                .AsNoTracking()
                .Include(p => p.Categoria)
                .Include(p => p.Proveedor)
                .ToListAsync();

        public async Task<Producto?> GetByIdAsync(int id) =>
            await _context.Productos
                .Include(p => p.Categoria)
                .FirstOrDefaultAsync(p => p.Id == id);

        public async Task<Producto> AddAsync(Producto producto)
        {
            await _context.Productos.AddAsync(producto);
            return producto; // Sin SaveChangesAsync: lo confirma IUnitOfWork
        }

        public Task UpdateAsync(Producto producto)
        {
            _context.Productos.Update(producto);
            return Task.CompletedTask;
        }

        public async Task RegistrarMovimientoAsync(MovimientoStock movimiento)
        {
            await _context.MovimientosStock.AddAsync(movimiento);
        }
    }
}