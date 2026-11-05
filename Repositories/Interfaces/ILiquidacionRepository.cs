using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.Models.Ventas;

namespace MiPeluqueria.Api.Repositories.Interfaces
{
    public interface ILiquidacionRepository : IGenericRepository<Liquidacion>
    {
        Task<Liquidacion?> GetByIdWithDetallesAsync(int id, bool tracking = true, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Liquidacion>> GetAllWithPeluqueroAsync(CancellationToken cancellationToken = default);
        Task<(IReadOnlyList<Liquidacion> Items, int TotalRecords)> GetPagedAsync(PaginationFilterDto filter, int? peluqueroId = null, EstadoLiquidacionEnum? estado = null, CancellationToken ct = default);
        Task<IReadOnlyList<Liquidacion>> GetByPeluqueroIdAsync(int peluqueroId, CancellationToken cancellationToken = default);
        Task<bool> ExisteLiquidacionEnPeriodoAsync(int peluqueroId, DateTime fechaDesde, DateTime fechaHasta, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<DetalleVenta>> GetDetallesVentaPendientesLiquidacionAsync(int peluqueroId, DateTime fechaDesde, DateTime fechaHasta, CancellationToken ct = default);
        Task<Dictionary<int, DetalleVenta>> GetDetallesVentaByIdsDictionaryAsync(IEnumerable<int> ids, CancellationToken ct = default);
        void AnularDetallesLiquidacion(IEnumerable<DetalleLiquidacion> detalles);
    }
}
