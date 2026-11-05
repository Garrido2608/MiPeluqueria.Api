using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MiPeluqueria.Api.Data.Context;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.Models.Clientes;
using MiPeluqueria.Api.Repositories.Interfaces;

namespace MiPeluqueria.Api.Repositories.Implementations
{
    public class ClienteRepository : GenericRepository<Cliente>, IClienteRepository
    {
        public ClienteRepository(ApplicationDbContext context) : base(context)
        {
        }

        public IQueryable<Cliente> GetAllQueryable() =>
            _dbSet
                .AsNoTracking()
                .Include(c => c.TipoCabello)
                .Include(c => c.TipoRostro);

        public override async Task<IReadOnlyList<Cliente>> GetAllAsync(CancellationToken ct = default) =>
            await GetAllQueryable()
                .OrderBy(c => c.Apellido)
                .ThenBy(c => c.Nombre)
                .ToListAsync(ct);

        public override async Task<Cliente?> GetByIdAsync(int id, CancellationToken ct = default) =>
            await _dbSet
                .Include(c => c.TipoCabello)
                .Include(c => c.TipoRostro)
                .FirstOrDefaultAsync(c => c.Id == id, ct);

        public async Task<Cliente?> GetByIdWithDetailsAsync(int id, bool tracking = false, CancellationToken ct = default)
        {
            var query = tracking ? _dbSet.AsQueryable() : _dbSet.AsNoTracking();
            return await query
                .Include(c => c.TipoCabello)
                .Include(c => c.TipoRostro)
                .FirstOrDefaultAsync(c => c.Id == id, ct);
        }

        public async Task<(IReadOnlyList<Cliente> Items, int TotalRecords)> GetPagedAsync(
            PaginationFilterDto filter,
            CancellationToken ct = default)
        {
            var query = GetAllQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim().ToLower();
                query = query.Where(c =>
                    c.Nombre.ToLower().Contains(term) ||
                    c.Apellido.ToLower().Contains(term) ||
                    c.Telefono.Contains(term) ||
                    (c.Email != null && c.Email.ToLower().Contains(term)));
            }

            if (filter.FechaDesde.HasValue)
                query = query.Where(c => c.FechaRegistro >= filter.FechaDesde.Value);

            if (filter.FechaHasta.HasValue)
                query = query.Where(c => c.FechaRegistro <= filter.FechaHasta.Value);

            int totalRecords = await query.CountAsync(ct);

            var items = await query
                .OrderBy(c => c.Apellido)
                .ThenBy(c => c.Nombre)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync(ct);

            return (items, totalRecords);
        }

        public Task UpdateAsync(Cliente cliente, CancellationToken ct = default)
        {
            Update(cliente);
            return Task.CompletedTask;
        }

        public async Task DeleteAsync(int id, CancellationToken ct = default)
        {
            var cliente = await _dbSet.FirstOrDefaultAsync(c => c.Id == id, ct);
            if (cliente != null)
            {
                Delete(cliente);
            }
        }

        public async Task<bool> ExistsEmailAsync(string email, int? excludeId = null, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            var norm = email.Trim().ToLower();
            return await _dbSet.AnyAsync(
                c => c.Email != null && c.Email.ToLower() == norm && (!excludeId.HasValue || c.Id != excludeId.Value),
                ct);
        }
    }
}
