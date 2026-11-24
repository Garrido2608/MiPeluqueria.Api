using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.Models.Stock;

namespace MiPeluqueria.Api.Repositories.Interfaces
{
    public interface IOrdenCompraRepository : IGenericRepository<OrdenCompra>
    {
        Task<IReadOnlyList<OrdenCompra>> GetAllAsync(int? proveedorId, EstadoOrdenCompraEnum? estado = null, CancellationToken ct = default);
        Task<(IReadOnlyList<OrdenCompra> Items, int TotalRecords)> GetPagedAsync(PaginationFilterDto filter, int? proveedorId = null, EstadoOrdenCompraEnum? estado = null, CancellationToken ct = default);
        Task<OrdenCompra?> GetByIdCompletaAsync(int id, CancellationToken ct = default);
    }
}
