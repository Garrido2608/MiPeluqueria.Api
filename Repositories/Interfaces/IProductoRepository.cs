using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.Models.Stock;

namespace MiPeluqueria.Api.Repositories.Interfaces
{
    public interface IProductoRepository : IGenericRepository<Producto>
    {
        Task<IReadOnlyList<Producto>> GetAllWithDetailsAsync(bool soloBajoStock = false, CancellationToken ct = default);
        Task<(IReadOnlyList<Producto> Items, int TotalRecords)> GetPagedAsync(PaginationFilterDto filter, bool soloBajoStock = false, int? categoriaId = null, int? proveedorId = null, CancellationToken ct = default);
        Task<IReadOnlyList<Producto>> GetStockCriticoAsync(CancellationToken ct = default);
        Task<Producto?> GetByIdWithDetailsAsync(int id, bool tracking = false, CancellationToken ct = default);
        Task<bool> ExistsCodigoBarraAsync(string codigoBarra, int? excludeId = null, CancellationToken ct = default);
        Task RegistrarMovimientoAsync(MovimientoStock movimiento, CancellationToken ct = default);
        Task<(IReadOnlyList<MovimientoStock> Items, int TotalRecords)> GetKardexPagedAsync(int productoId, PaginationFilterDto filter, CancellationToken ct = default);
        Task<IReadOnlyList<MovimientoStock>> GetKardexByProductoIdAsync(int productoId, CancellationToken ct = default);
        Task AddHistorialPrecioAsync(HistorialPrecioProducto historial, CancellationToken ct = default);
        Task<IReadOnlyList<HistorialPrecioProducto>> GetHistorialPreciosAsync(int productoId, CancellationToken ct = default);
        Task<bool> ExisteConsumoGabinetePorTurnoAsync(int turnoId, CancellationToken ct = default);
        Task UpdateAsync(Producto producto, CancellationToken ct = default);
        Task DeleteAsync(int id, CancellationToken ct = default);
    }
}
