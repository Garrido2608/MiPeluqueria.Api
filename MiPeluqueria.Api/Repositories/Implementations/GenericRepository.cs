using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MiPeluqueria.Api.Data.Context;
using MiPeluqueria.Api.Models.Common;
using MiPeluqueria.Api.Repositories.Interfaces;

namespace MiPeluqueria.Api.Repositories.Implementations
{
    public class GenericRepository<T> : IGenericRepository<T> where T : AuditableEntity
    {
        protected readonly ApplicationDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public GenericRepository(ApplicationDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public virtual async Task<T?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            return await _dbSet.FirstOrDefaultAsync(e => e.Id == id, ct);
        }

        public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default)
        {
            return await _dbSet.AsNoTracking().ToListAsync(ct);
        }

        public virtual async Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        {
            return await _dbSet.AsNoTracking().Where(predicate).ToListAsync(ct);
        }

        public virtual async Task<Dictionary<int, T>> GetByIdsDictionaryAsync(
            IEnumerable<int> ids,
            bool tracking = false,
            CancellationToken ct = default)
        {
            if (ids == null)
                return new Dictionary<int, T>();

            var distinctIds = ids.Distinct().ToList();
            if (distinctIds.Count == 0)
                return new Dictionary<int, T>();

            var query = tracking ? _dbSet.AsQueryable() : _dbSet.AsNoTracking();
            return await query
                .Where(e => distinctIds.Contains(e.Id))
                .ToDictionaryAsync(e => e.Id, ct);
        }

        public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        {
            return await _dbSet.AnyAsync(predicate, ct);
        }

        public virtual async Task AddAsync(T entity, CancellationToken ct = default)
        {
            entity.CreatedAt = DateTime.UtcNow;
            await _dbSet.AddAsync(entity, ct);
        }

        public virtual void Update(T entity)
        {
            entity.UpdatedAt = DateTime.UtcNow;
            _dbSet.Update(entity);
        }

        public virtual void Delete(T entity)
        {
            entity.IsDeleted = true;
            entity.DeletedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;
            _dbSet.Update(entity);
        }

        public virtual IQueryable<T> AsQueryable()
        {
            return _dbSet.AsQueryable();
        }
    }
}