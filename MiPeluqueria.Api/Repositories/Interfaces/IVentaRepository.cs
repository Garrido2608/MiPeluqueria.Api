using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.Models.Ventas;

namespace MiPeluqueria.Api.Repositories.Interfaces
{
    public interface IVentaRepository : IGenericRepository<Venta>
    {
        Task<Venta?> GetByIdCompletaAsync(int ventaId, CancellationToken cancellationToken = default);
        Task<Venta?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);
        Task<bool> ExisteVentaActivaConSenaPorTurnoAsync(int turnoId, int? excludeVentaId = null, CancellationToken cancellationToken = default);
        Task<bool> TieneRenglonesEnLiquidacionActivaAsync(int ventaId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Venta>> GetBySesionCajaIdAsync(int sesionCajaId, CancellationToken cancellationToken = default);
        Task<(IReadOnlyList<Venta> Items, int TotalRecords)> GetBySesionCajaIdPagedAsync(int sesionCajaId, PaginationFilterDto filter, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Venta>> GetVentasParaLiquidacionAsync(int peluqueroId, DateTime fechaDesde, DateTime fechaHasta, CancellationToken cancellationToken = default);
        Task<(string Nombre, decimal Precio)?> GetDatosServicioAsync(int servicioId, CancellationToken cancellationToken = default);
        Task<string?> GetNombrePeluqueroAsync(int peluqueroId, CancellationToken cancellationToken = default);
    }
}
