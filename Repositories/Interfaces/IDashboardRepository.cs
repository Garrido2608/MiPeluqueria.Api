using MiPeluqueria.Api.Models.Turnos;
using MiPeluqueria.Api.Models.Ventas;

namespace MiPeluqueria.Api.Repositories.Interfaces
{
    public interface IDashboardRepository
    {
        Task<IReadOnlyList<Venta>> GetVentasEnRangoAsync(DateTime fechaDesde, DateTime fechaHasta, CancellationToken ct = default);
        Task<IReadOnlyList<Turno>> GetTurnosEnRangoAsync(DateTime fechaDesde, DateTime fechaHasta, CancellationToken ct = default);
        Task<int> CountNuevosClientesEnRangoAsync(DateTime fechaDesde, DateTime fechaHasta, CancellationToken ct = default);
        Task<int> CountProductosStockCriticoAsync(CancellationToken ct = default);
    }
}