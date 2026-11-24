using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MiPeluqueria.Api.Models.Turnos;
using MiPeluqueria.Api.Models.Ventas;

namespace MiPeluqueria.Api.Repositories.Interfaces
{
    public interface IDashboardRepository : IGenericRepository<Venta>
    {
        Task<IReadOnlyList<Venta>> GetVentasEnRangoAsync(DateTime fechaDesde, DateTime fechaHasta, CancellationToken ct = default);
        Task<IReadOnlyList<Turno>> GetTurnosEnRangoAsync(DateTime fechaDesde, DateTime fechaHasta, CancellationToken ct = default);
        Task<int> CountTurnosPendientesEnRangoAsync(DateTime inicioUtc, DateTime finUtc, CancellationToken ct = default);
        Task<int> CountNuevosClientesEnRangoAsync(DateTime fechaDesde, DateTime fechaHasta, CancellationToken ct = default);
        Task<int> CountProductosStockCriticoAsync(CancellationToken ct = default);
    }
}
