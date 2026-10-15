using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Ventas;

namespace MiPeluqueria.Api.Services.Interfaces
{
    public interface ICajaVentaService
    {
        Task<ApiResponse<int>> AbrirCajaAsync(AperturaCajaDto dto);
        Task<ApiResponse<int>> RegistrarVentaAsync(CrearVentaDto dto, int usuarioId);
        Task<ApiResponse<bool>> RegistrarEgresoCajaAsync(int sesionCajaId, decimal monto, string concepto, string tipo = "EgresoMenor");
        Task<ApiResponse<bool>> RegistrarIngresoCajaAsync(int sesionCajaId, decimal monto, string concepto, string tipo = "IngresoManual");
        Task<ApiResponse<decimal>> CerrarCajaAsync(CierreCajaDto dto);
        Task<ApiResponse<object>> ObtenerEstadoCajaActualAsync();
        Task<ApiResponse<IEnumerable<object>>> ObtenerHistorialCajasAsync(DateTime? fechaDesde = null, DateTime? fechaHasta = null);
        Task<ApiResponse<VentaResponseDto>> ObtenerVentaPorIdAsync(int id);
        Task<ApiResponse<IEnumerable<VentaResponseDto>>> ObtenerVentasPorSesionAsync(int sesionCajaId);
        Task<ApiResponse<bool>> AnularVentaAsync(int ventaId, string motivo, int usuarioId);
    }
}