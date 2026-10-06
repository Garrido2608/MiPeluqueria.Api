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
            await _context.Productos.AsNoTracking().Include(p => p.Categoria).ToListAsync();

        public async Task<Producto?> GetByIdAsync(int id) =>
            await _context.Productos.FirstOrDefaultAsync(p => p.Id == id);

        public async Task<Producto> AddAsync(Producto producto)
        {
            _context.Productos.Add(producto);
            await _context.SaveChangesAsync();
            return producto;
        }

        public async Task UpdateAsync(Producto producto)
        {
            _context.Productos.Update(producto);
            await _context.SaveChangesAsync();
        }

        public async Task RegistrarMovimientoAsync(MovimientoStock movimiento)
        {
            _context.MovimientosStock.Add(movimiento);
            await _context.SaveChangesAsync();
        }
    }
}