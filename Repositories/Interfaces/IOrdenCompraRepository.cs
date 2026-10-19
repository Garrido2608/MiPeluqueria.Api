using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MiPeluqueria.Api.Models.Stock;

namespace MiPeluqueria.Api.Repositories.Interfaces
{
    public interface IOrdenCompraRepository
    {
        Task<IReadOnlyList<OrdenCompra>> GetAllAsync(int? proveedorId = null, EstadoOrdenCompraEnum? estado = null, CancellationToken ct = default);
        Task<OrdenCompra?> GetByIdCompletaAsync(int id, CancellationToken ct = default);
        Task AddAsync(OrdenCompra orden, CancellationToken ct = default);
        void Update(OrdenCompra orden);
    }
}