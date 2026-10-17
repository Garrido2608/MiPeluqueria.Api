using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Ventas;
using MiPeluqueria.Api.Models.Ventas;

namespace MiPeluqueria.Api.Services.Interfaces
{
    public interface ICajaService
    {
        Task<ApiResponse<SesionCaja?>> ObtenerEstadoCajaActualAsync(CancellationToken ct = default);
        Task<ApiResponse<IReadOnlyList<SesionCaja>>> ObtenerHistorialCajasAsync(DateTime? desde = null, DateTime? hasta = null, CancellationToken ct = default);
        Task<ApiResponse<SesionCaja>> AbrirCajaAsync(AperturaCajaDto dto, CancellationToken ct = default);
        Task<ApiResponse<MovimientoCaja>> RegistrarIngresoCajaAsync(int sesionCajaId, decimal monto, string concepto, string tipo = "IngresoManual", CancellationToken ct = default);
        Task<ApiResponse<MovimientoCaja>> RegistrarEgresoCajaAsync(int sesionCajaId, decimal monto, string concepto, string tipo = "EgresoMenor", CancellationToken ct = default);
        Task<ApiResponse<SesionCaja>> CerrarCajaAsync(CierreCajaDto dto, CancellationToken ct = default);
    }
}