using Microsoft.EntityFrameworkCore;
using MiPeluqueria.Api.Data.Context;
using MiPeluqueria.Api.Models.Stock;
using MiPeluqueria.Api.Repositories.Interfaces;

namespace MiPeluqueria.Api.Repositories.Implementations
{
    public class ProveedorRepository : IProveedorRepository
    {
        private readonly ApplicationDbContext _context;

        public ProveedorRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Proveedor>> GetAllAsync()
        {
            return await _context.Proveedores
                .OrderBy(p => p.RazonSocial)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Proveedor?> GetByIdAsync(int id)
        {
            return await _context.Proveedores
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<bool> ExistsByRazonSocialAsync(string razonSocial, int? excludeId = null)
        {
            var normalized = razonSocial.Trim().ToLower();
            return await _context.Proveedores
                .AnyAsync(p => p.RazonSocial.ToLower() == normalized && (!excludeId.HasValue || p.Id != excludeId.Value));
        }

        public async Task AddAsync(Proveedor proveedor)
        {
            await _context.Proveedores.AddAsync(proveedor);
        }

        public void Update(Proveedor proveedor)
        {
            _context.Proveedores.Update(proveedor);
        }

        public void Delete(Proveedor proveedor)
        {
            _context.Proveedores.Remove(proveedor);
        }
    }
}