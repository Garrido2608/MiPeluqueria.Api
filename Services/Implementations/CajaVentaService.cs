using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MiPeluqueria.Api.Data.Context;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Ventas;
using MiPeluqueria.Api.Models.Stock;
using MiPeluqueria.Api.Models.Turnos;
using MiPeluqueria.Api.Models.Ventas;
using MiPeluqueria.Api.Repositories.Interfaces;

namespace MiPeluqueria.Api.Services.Implementations
{
    public interface ICajaVentaService
    {
        Task<ApiResponse<int>> AbrirCajaAsync(AperturaCajaDto dto);
        Task<ApiResponse<decimal>> CerrarCajaAsync(CierreCajaDto dto);
        Task<ApiResponse<int>> RegistrarVentaAsync(CrearVentaDto dto, int usuarioId);
    }

    public class CajaVentaService : ICajaVentaService
    {
        private readonly ISesionCajaRepository _cajaRepo;
        private readonly IVentaRepository _ventaRepo;
        private readonly ApplicationDbContext _context;

        public CajaVentaService(ISesionCajaRepository cajaRepo, IVentaRepository ventaRepo, ApplicationDbContext context)
        {
            _cajaRepo = cajaRepo;
            _ventaRepo = ventaRepo;
            _context = context;
        }

        public async Task<ApiResponse<int>> AbrirCajaAsync(AperturaCajaDto dto)
        {
            var cajaAbierta = await _cajaRepo.GetCajaAbiertaActualAsync();
            if (cajaAbierta != null)
                return ApiResponse<int>.Falla("Ya existe una caja abierta en el sistema.");

            var nuevaCaja = new SesionCaja
            {
                UsuarioAperturaId = dto.UsuarioId,
                FechaApertura = DateTime.UtcNow,
                MontoInicial = dto.MontoInicial,
                EstadoAbierta = true
            };

            await _cajaRepo.AbrirCajaAsync(nuevaCaja);
            return ApiResponse<int>.Exito(nuevaCaja.Id, "Caja abierta con éxito.");
        }

        public async Task<ApiResponse<int>> RegistrarVentaAsync(CrearVentaDto dto, int usuarioId)
        {
            var caja = await _cajaRepo.GetCajaAbiertaActualAsync();
            if (caja == null || caja.Id != dto.SesionCajaId)
                return ApiResponse<int>.Falla("No se pueden registrar ventas sin una caja abierta.");

            // 1. Validar que la suma de los pagos coincida con el total de los items
            decimal totalCalculado = 0;
            var nuevaVenta = new Venta
            {
                SesionCajaId = dto.SesionCajaId,
                ClienteId = dto.ClienteId,
                TurnoId = dto.TurnoId,
                Fecha = DateTime.UtcNow,
                UsuarioId = usuarioId
            };

            foreach (var d in dto.Detalles)
            {
                decimal precio = 0;

                if (d.TipoItem == TipoItemVentaEnum.Servicio)
                {
                    var serv = await _context.Servicios.FindAsync(d.ItemId);
                    if (serv == null) return ApiResponse<int>.Falla($"Servicio con ID {d.ItemId} no existe.");
                    precio = serv.Precio;
                }
                else if (d.TipoItem == TipoItemVentaEnum.Producto)
                {
                    var prod = await _context.Productos.FindAsync(d.ItemId);
                    if (prod == null) return ApiResponse<int>.Falla($"Producto con ID {d.ItemId} no existe.");

                    if (prod.StockActual < d.Cantidad)
                        return ApiResponse<int>.Falla($"Stock insuficiente para {prod.Nombre}.");

                    // Descuento automático de inventario
                    prod.StockActual -= d.Cantidad;
                    _context.MovimientosStock.Add(new MovimientoStock
                    {
                        ProductoId = prod.Id,
                        TipoMovimiento = TipoMovimientoStockEnum.EgresoVentaMostrador,
                        Cantidad = d.Cantidad,
                        Fecha = DateTime.UtcNow,
                        UsuarioId = usuarioId,
                        Motivo = $"Venta en mostrador."
                    });

                    precio = prod.PrecioVenta;
                }

                decimal subtotal = precio * d.Cantidad;
                totalCalculado += subtotal;

                nuevaVenta.DetallesVenta.Add(new DetalleVenta
                {
                    TipoItem = d.TipoItem,
                    ItemId = d.ItemId,
                    Cantidad = d.Cantidad,
                    PrecioUnitario = precio,
                    Subtotal = subtotal,
                    PeluqueroId = d.PeluqueroId
                });
            }

            nuevaVenta.Total = totalCalculado;

            // 2. Validar que el total pagado cubra el total vendido (Cobro Mixto)
            decimal totalPagado = dto.Pagos.Sum(p => p.Importe);
            if (totalPagado != totalCalculado)
                return ApiResponse<int>.Falla($"El total pagado (${totalPagado}) no coincide con el total de la venta (${totalCalculado}).");

            foreach (var p in dto.Pagos)
            {
                nuevaVenta.Pagos.Add(new Pago
                {
                    MedioPagoId = p.MedioPagoId,
                    Importe = p.Importe
                });
            }

            // 3. Si venía de un turno, marcar el turno como Completado
            if (dto.TurnoId.HasValue)
            {
                var turno = await _context.Turnos.FindAsync(dto.TurnoId.Value);
                if (turno != null)
                {
                    turno.EstadoId = (int)EstadoTurnoEnum.Completado;
                }
            }

            await _ventaRepo.RegistrarVentaAsync(nuevaVenta);
            return ApiResponse<int>.Exito(nuevaVenta.Id, "Venta registrada y cobrada exitosamente.");
        }

        public async Task<ApiResponse<decimal>> CerrarCajaAsync(CierreCajaDto dto)
        {
            var caja = await _cajaRepo.GetCajaAbiertaActualAsync();
            if (caja == null)
                return ApiResponse<decimal>.Falla("No hay ninguna caja abierta para cerrar.");

            // Calcular dinero esperado en efectivo: MontoInicial + Ventas en Efectivo - Egresos
            decimal totalVentasEfectivo = caja.Ventas
                .SelectMany(v => v.Pagos)
                .Where(p => p.MedioPagoId == 1) // 1 = Efectivo
                .Sum(p => p.Importe);

            decimal totalEgresos = caja.MovimientosCaja.Sum(m => m.Monto);

            decimal esperado = caja.MontoInicial + totalVentasEfectivo - totalEgresos;

            caja.UsuarioCierreId = dto.UsuarioId;
            caja.FechaCierre = DateTime.UtcNow;
            caja.MontoFinalReal = dto.MontoFinalReal;
            caja.MontoFinalEsperado = esperado;
            caja.Diferencia = dto.MontoFinalReal - esperado;
            caja.EstadoAbierta = false;

            await _cajaRepo.CerrarCajaAsync(caja);

            return ApiResponse<decimal>.Exito(caja.Diferencia.Value, $"Caja cerrada. Diferencia registrada: ${caja.Diferencia.Value}");
        }
    }
}