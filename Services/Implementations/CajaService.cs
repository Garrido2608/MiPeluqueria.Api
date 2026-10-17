using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Ventas;
using MiPeluqueria.Api.Models.Ventas;
using MiPeluqueria.Api.Repositories.Interfaces;
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Services.Implementations
{
    public class CajaService : ICajaService
    {
        private readonly IUnitOfWork _unitOfWork;

        public CajaService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ApiResponse<SesionCaja?>> ObtenerEstadoCajaActualAsync(CancellationToken ct = default)
        {
            var caja = await _unitOfWork.SesionesCaja.GetCajaAbiertaActualAsync(ct);
            return ApiResponse<SesionCaja?>.Ok(caja, caja != null ? "Caja abierta encontrada." : "No hay ninguna caja abierta actualmente.");
        }

        public async Task<ApiResponse<IReadOnlyList<SesionCaja>>> ObtenerHistorialCajasAsync(DateTime? desde = null, DateTime? hasta = null, CancellationToken ct = default)
        {
            var historial = await _unitOfWork.SesionesCaja.GetHistorialCajasAsync(desde, hasta, ct);
            return ApiResponse<IReadOnlyList<SesionCaja>>.Ok(historial);
        }

        public async Task<ApiResponse<SesionCaja>> AbrirCajaAsync(AperturaCajaDto dto, CancellationToken ct = default)
        {
            var cajaAbierta = await _unitOfWork.SesionesCaja.GetCajaAbiertaActualAsync(ct);
            if (cajaAbierta != null)
                return ApiResponse<SesionCaja>.Fail($"Ya existe una sesión de caja abierta (ID #{cajaAbierta.Id}). Debe cerrarla antes de abrir una nueva.");

            var nuevaSesion = new SesionCaja
            {
                UsuarioAperturaId = dto.UsuarioId,
                FechaApertura = DateTime.UtcNow,
                MontoInicial = dto.MontoInicial,
                EstadoAbierta = true
            };

            await _unitOfWork.SesionesCaja.AbrirSesionCajaAsync(nuevaSesion, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return ApiResponse<SesionCaja>.Ok(nuevaSesion, "Caja abierta exitosamente.");
        }

        public async Task<ApiResponse<MovimientoCaja>> RegistrarIngresoCajaAsync(int sesionCajaId, decimal monto, string concepto, string tipo = "IngresoManual", CancellationToken ct = default)
        {
            var caja = await _unitOfWork.SesionesCaja.GetCajaAbiertaActualAsync(ct);
            if (caja == null || (sesionCajaId > 0 && caja.Id != sesionCajaId))
                return ApiResponse<MovimientoCaja>.Fail("No hay una sesión de caja abierta válida para registrar el ingreso.");

            var movimiento = new MovimientoCaja
            {
                SesionCajaId = caja.Id,
                Tipo = string.IsNullOrWhiteSpace(tipo) ? "IngresoManual" : tipo.Trim(),
                Monto = monto,
                Concepto = concepto.Trim(),
                Fecha = DateTime.UtcNow
            };

            await _unitOfWork.SesionesCaja.RegistrarMovimientoCajaAsync(movimiento, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return ApiResponse<MovimientoCaja>.Ok(movimiento, "Ingreso manual registrado correctamente.");
        }

        public async Task<ApiResponse<MovimientoCaja>> RegistrarEgresoCajaAsync(int sesionCajaId, decimal monto, string concepto, string tipo = "EgresoMenor", CancellationToken ct = default)
        {
            var caja = await _unitOfWork.SesionesCaja.GetCajaAbiertaActualAsync(ct);
            if (caja == null || (sesionCajaId > 0 && caja.Id != sesionCajaId))
                return ApiResponse<MovimientoCaja>.Fail("No hay una sesión de caja abierta válida para registrar el egreso.");

            var movimiento = new MovimientoCaja
            {
                SesionCajaId = caja.Id,
                Tipo = string.IsNullOrWhiteSpace(tipo) ? "EgresoMenor" : tipo.Trim(),
                Monto = monto,
                Concepto = concepto.Trim(),
                Fecha = DateTime.UtcNow
            };

            await _unitOfWork.SesionesCaja.RegistrarMovimientoCajaAsync(movimiento, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return ApiResponse<MovimientoCaja>.Ok(movimiento, "Egreso de caja registrado correctamente.");
        }

        public async Task<ApiResponse<SesionCaja>> CerrarCajaAsync(CierreCajaDto dto, CancellationToken ct = default)
        {
            var caja = await _unitOfWork.SesionesCaja.GetCajaAbiertaActualAsync(ct);
            if (caja == null)
                return ApiResponse<SesionCaja>.Fail("No existe ninguna sesión de caja abierta para cerrar.");

            decimal totalVentas = caja.Ventas.Where(v => !v.IsDeleted).Sum(v => v.Total);
            decimal totalIngresosManuales = caja.MovimientosCaja
                .Where(m => !m.IsDeleted && m.Tipo.StartsWith("Ingreso", StringComparison.OrdinalIgnoreCase))
                .Sum(m => m.Monto);
            decimal totalEgresos = caja.MovimientosCaja
                .Where(m => !m.IsDeleted && !m.Tipo.StartsWith("Ingreso", StringComparison.OrdinalIgnoreCase))
                .Sum(m => m.Monto);

            decimal esperado = caja.MontoInicial + totalVentas + totalIngresosManuales - totalEgresos;

            caja.UsuarioCierreId = dto.UsuarioId;
            caja.FechaCierre = DateTime.UtcNow;
            caja.MontoFinalReal = dto.MontoFinalReal;
            caja.MontoFinalEsperado = esperado;
            caja.Diferencia = dto.MontoFinalReal - esperado;
            caja.EstadoAbierta = false;

            _unitOfWork.SesionesCaja.ActualizarSesionCaja(caja);
            await _unitOfWork.SaveChangesAsync(ct);

            return ApiResponse<SesionCaja>.Ok(caja, "Caja cerrada y arqueada exitosamente.");
        }
    }
}