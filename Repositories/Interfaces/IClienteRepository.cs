using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.Models.Clientes;

namespace MiPeluqueria.Api.Repositories.Interfaces
{
    public interface IClienteRepository : IGenericRepository<Cliente>
    {
        IQueryable<Cliente> GetAllQueryable();
        Task<(IReadOnlyList<Cliente> Items, int TotalRecords)> GetPagedAsync(PaginationFilterDto filter, CancellationToken ct = default);
        Task<Cliente?> GetByIdWithDetailsAsync(int id, bool tracking = false, CancellationToken ct = default);
        Task<bool> ExistsEmailAsync(string email, int? excludeId = null, CancellationToken ct = default);
        Task UpdateAsync(Cliente cliente, CancellationToken ct = default);
        Task DeleteAsync(int id, CancellationToken ct = default);
    }
}
