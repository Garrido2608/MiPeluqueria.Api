using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using MiPeluqueria.Api.Models.Common;

namespace MiPeluqueria.Api.Repositories.Interfaces
{
    public interface IGenericRepository<T> where T : AuditableEntity
    {
        Task<T?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default);
        Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
        Task<Dictionary<int, T>> GetByIdsDictionaryAsync(IEnumerable<int> ids, bool tracking = false, CancellationToken ct = default);
        Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
        Task AddAsync(T entity, CancellationToken ct = default);
        void Update(T entity);
        void Delete(T entity);
        IQueryable<T> AsQueryable();
    }
}