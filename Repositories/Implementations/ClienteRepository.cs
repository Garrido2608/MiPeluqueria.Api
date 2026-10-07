using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MiPeluqueria.Api.Data.Context;
using MiPeluqueria.Api.Models.Clientes;
using MiPeluqueria.Api.Repositories.Interfaces;

namespace MiPeluqueria.Api.Repositories.Implementations
{
    public class ClienteRepository : IClienteRepository
    {
        private readonly ApplicationDbContext _context;

        public ClienteRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public IQueryable<Cliente> GetAllQueryable() =>
            _context.Clientes
                .AsNoTracking()
                .Include(c => c.TipoCabello)
                .Include(c => c.TipoRostro);

        public async Task<List<Cliente>> GetAllAsync() =>
            await GetAllQueryable().ToListAsync();

        public async Task<Cliente?> GetByIdAsync(int id) =>
            await _context.Clientes
                .Include(c => c.TipoCabello)
                .Include(c => c.TipoRostro)
                .FirstOrDefaultAsync(c => c.Id == id);

        public async Task<Cliente> AddAsync(Cliente cliente)
        {
            await _context.Clientes.AddAsync(cliente);
            return cliente; // Sin SaveChangesAsync: lo confirma IUnitOfWork
        }

        public Task UpdateAsync(Cliente cliente)
        {
            _context.Clientes.Update(cliente);
            return Task.CompletedTask; // Sin SaveChangesAsync
        }

        public async Task DeleteAsync(int id)
        {
            var cliente = await _context.Clientes.FindAsync(id);
            if (cliente != null)
            {
                _context.Clientes.Remove(cliente); // Interceptado como Soft Delete en SaveChangesAsync
            }
        }

        public async Task<bool> ExistsEmailAsync(string email) =>
            await _context.Clientes.AnyAsync(c => c.Email == email);
    }
}