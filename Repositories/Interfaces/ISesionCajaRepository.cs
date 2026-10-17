using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MiPeluqueria.Api.Models.Ventas;

namespace MiPeluqueria.Api.Repositories.Interfaces
{
    public interface ISesionCajaRepository
    {
        Task<SesionCaja?> GetCajaAbiertaActualAsync(CancellationToken cancellationToken = default);
        Task<SesionCaja?> GetByIdWithMovimientosYVentasAsync(int sesionCajaId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<SesionCaja>> GetHistorialCajasAsync(DateTime? desde = null, DateTime? hasta = null, CancellationToken cancellationToken = default);
        Task AbrirSesionCajaAsync(SesionCaja sesionCaja, CancellationToken cancellationToken = default);
        void ActualizarSesionCaja(SesionCaja sesionCaja);
        Task RegistrarMovimientoCajaAsync(MovimientoCaja movimiento, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<MovimientoCaja>> GetMovimientosBySesionIdAsync(int sesionCajaId, CancellationToken cancellationToken = default);
    }
}