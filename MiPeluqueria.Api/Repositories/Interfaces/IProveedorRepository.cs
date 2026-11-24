using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.Models.Stock;

namespace MiPeluqueria.Api.Repositories.Interfaces
{
    public interface IProveedorRepository : IGenericRepository<Proveedor>
    {
        Task<(IReadOnlyList<Proveedor> Items, int TotalRecords)> GetPagedAsync(PaginationFilterDto filter, CancellationToken ct = default);
        Task<bool> ExistsByRazonSocialAsync(string razonSocial, int? excludeId = null, CancellationToken ct = default);
        Task<bool> ExistsByCuitAsync(string cuit, int? excludeId = null, CancellationToken ct = default);
        Task<Dictionary<int, int>> GetCantidadProductosPorProveedorAsync(IEnumerable<int>? proveedorIds = null, CancellationToken ct = default);
        Task<int> CountProductosAsociadosAsync(int proveedorId, CancellationToken ct = default);
        Task<bool> TieneProductosAsociadosAsync(int proveedorId, CancellationToken ct = default);
    }
}
