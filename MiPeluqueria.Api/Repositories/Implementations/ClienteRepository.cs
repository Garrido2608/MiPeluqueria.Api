using System;
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
            int pageNumber = Math.Max(1, filter.PageNumber);
            int pageSize = Math.Clamp(filter.PageSize, 1, 100);

            var baseQuery = _dbSet.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim();
                baseQuery = baseQuery.Where(c =>
                    c.Nombre.Contains(term) ||
                    c.Apellido.Contains(term) ||
                    c.Telefono.Contains(term) ||
                    (c.Email != null && c.Email.Contains(term)));
            }

            if (filter.FechaDesde.HasValue)
                baseQuery = baseQuery.Where(c => c.FechaRegistro >= filter.FechaDesde.Value);

            if (filter.FechaHasta.HasValue)
                baseQuery = baseQuery.Where(c => c.FechaRegistro <= filter.FechaHasta.Value);

            var items = await baseQuery
                .Include(c => c.TipoCabello)
                .Include(c => c.TipoRostro)
                .OrderBy(c => c.Apellido)
                .ThenBy(c => c.Nombre)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            int totalRecords = (pageNumber == 1 && items.Count < pageSize)
                ? items.Count
                : await baseQuery.CountAsync(ct);

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

            var norm = email.Trim();
            return await _dbSet
                .AsNoTracking()
                .AnyAsync(
                    c => c.Email != null && c.Email == norm && (!excludeId.HasValue || c.Id != excludeId.Value),
                    ct);
        }
    }
}
