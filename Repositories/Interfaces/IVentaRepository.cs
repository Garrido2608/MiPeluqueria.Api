using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MiPeluqueria.Api.Models.Ventas;

namespace MiPeluqueria.Api.Repositories.Interfaces
{
    public interface IVentaRepository
    {
        Task<Venta?> GetByIdCompletaAsync(int ventaId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Venta>> GetBySesionCajaIdAsync(int sesionCajaId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Venta>> GetVentasParaLiquidacionAsync(int peluqueroId, DateTime fechaDesde, DateTime fechaHasta, CancellationToken cancellationToken = default);
        Task<(string Nombre, decimal Precio)?> GetDatosServicioAsync(int servicioId, CancellationToken cancellationToken = default);
        Task<string?> GetNombrePeluqueroAsync(int peluqueroId, CancellationToken cancellationToken = default);
        Task AddAsync(Venta venta, CancellationToken cancellationToken = default);
        void Update(Venta venta);
    }
}