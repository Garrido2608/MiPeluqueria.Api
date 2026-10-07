using System;
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
        Task<ApiResponse<bool>> RegistrarEgresoCajaAsync(int sesionCajaId, decimal monto, string concepto, string tipo);
    }

    public class CajaVentaService : ICajaVentaService
    {
        private readonly ISesionCajaRepository _cajaRepo;
        private readonly IVentaRepository _ventaRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ApplicationDbContext _context;

        public CajaVentaService(
            ISesionCajaRepository cajaRepo,
            IVentaRepository ventaRepo,
            IUnitOfWork unitOfWork,
            ApplicationDbContext context)
        {
            _cajaRepo = cajaRepo;
            _ventaRepo = ventaRepo;
            _unitOfWork = unitOfWork;
            _context = context;
        }

        public async Task<ApiResponse<int>> AbrirCajaAsync(AperturaCajaDto dto)
        {
            var cajaAbierta = await _cajaRepo.GetCajaAbiertaActualAsync();
            if (cajaAbierta != null)
                return ApiResponse<int>.Falla($"Ya existe una sesión de caja abierta (ID: {cajaAbierta.Id}). Debe cerrarla antes de abrir una nueva.");

            var nuevaCaja = new SesionCaja
            {
                UsuarioAperturaId = dto.UsuarioId,
                FechaApertura = DateTime.UtcNow,
                MontoInicial = dto.MontoInicial,
                EstadoAbierta = true
            };

            await _cajaRepo.AbrirCajaAsync(nuevaCaja);
            await _unitOfWork.SaveChangesAsync();

            return ApiResponse<int>.Exito(nuevaCaja.Id, "Sesión de caja abierta exitosamente.");
        }

        public async Task<ApiResponse<int>> RegistrarVentaAsync(CrearVentaDto dto, int usuarioId)
        {
            var caja = await _cajaRepo.GetCajaAbiertaActualAsync();
            if (caja == null || caja.Id != dto.SesionCajaId)
                return ApiResponse<int>.Falla("No se pueden registrar ventas sin una sesión de caja abierta y válida.");

            await _unitOfWork.BeginTransactionAsync();
            try
            {
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
                        if (serv == null || !serv.Activo)
                            throw new InvalidOperationException($"El servicio con ID {d.ItemId} no existe o está inactivo.");

                        precio = serv.Precio;
                    }
                    else if (d.TipoItem == TipoItemVentaEnum.Producto)
                    {
                        var prod = await _context.Productos.FindAsync(d.ItemId);
                        if (prod == null)
                            throw new InvalidOperationException($"El producto con ID {d.ItemId} no existe.");

                        if (prod.StockActual < d.Cantidad)
                            throw new InvalidOperationException($"Stock insuficiente para el producto '{prod.Nombre}' (Disponible: {prod.StockActual}, Solicitado: {d.Cantidad}).");

                        prod.StockActual -= d.Cantidad;

                        await _context.MovimientosStock.AddAsync(new MovimientoStock
                        {
                            ProductoId = prod.Id,
                            TipoMovimiento = TipoMovimientoStockEnum.EgresoVentaMostrador,
                            Cantidad = d.Cantidad,
                            Fecha = DateTime.UtcNow,
                            UsuarioId = usuarioId,
                            Motivo = "Egreso automático por venta en mostrador."
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

                decimal totalPagado = dto.Pagos.Sum(p => p.Importe);
                if (totalPagado != totalCalculado)
                    throw new InvalidOperationException($"El total abonado (${totalPagado:N2}) no coincide con el total de la venta (${totalCalculado:N2}).");

                foreach (var p in dto.Pagos)
                {
                    nuevaVenta.Pagos.Add(new Pago
                    {
                        MedioPagoId = p.MedioPagoId,
                        Importe = p.Importe
                    });
                }

                if (dto.TurnoId.HasValue)
                {
                    var turno = await _context.Turnos.FindAsync(dto.TurnoId.Value);
                    if (turno != null && turno.EstadoId != (int)EstadoTurnoEnum.Completado)
                    {
                        int estadoAnterior = turno.EstadoId;
                        turno.EstadoId = (int)EstadoTurnoEnum.Completado;

                        await _context.HistorialEstadoTurnos.AddAsync(new HistorialEstadoTurno
                        {
                            TurnoId = turno.Id,
                            EstadoAnteriorId = estadoAnterior,
                            EstadoNuevoId = (int)EstadoTurnoEnum.Completado,
                            FechaCambio = DateTime.UtcNow,
                            UsuarioId = usuarioId,
                            Observaciones = "Turno completado automáticamente al registrar su cobro en caja."
                        });
                    }
                }

                await _ventaRepo.RegistrarVentaAsync(nuevaVenta);
                await _unitOfWork.CommitTransactionAsync();

                return ApiResponse<int>.Exito(nuevaVenta.Id, "Venta registrada, stock descontado y cobro asentado exitosamente.");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return ApiResponse<int>.Falla(ex.Message);
            }
        }

        public async Task<ApiResponse<bool>> RegistrarEgresoCajaAsync(int sesionCajaId, decimal monto, string concepto, string tipo)
        {
            var caja = await _cajaRepo.GetCajaAbiertaActualAsync();
            if (caja == null || caja.Id != sesionCajaId)
                return ApiResponse<bool>.Falla("No existe una sesión de caja activa coincidente.");

            if (monto <= 0)
                return ApiResponse<bool>.Falla("El monto del movimiento de caja debe ser mayor a cero.");

            await _cajaRepo.RegistrarMovimientoCajaAsync(new MovimientoCaja
            {
                SesionCajaId = sesionCajaId,
                Tipo = string.IsNullOrWhiteSpace(tipo) ? "EgresoMenor" : tipo,
                Monto = monto,
                Concepto = concepto,
                Fecha = DateTime.UtcNow
            });

            await _unitOfWork.SaveChangesAsync();
            return ApiResponse<bool>.Exito(true, "Movimiento de egreso registrado en la caja actual.");
        }

        public async Task<ApiResponse<decimal>> CerrarCajaAsync(CierreCajaDto dto)
        {
            var caja = await _cajaRepo.GetCajaAbiertaActualAsync();
            if (caja == null)
                return ApiResponse<decimal>.Falla("No hay ninguna sesión de caja abierta para cerrar.");

            // Arqueo ciego sobre efectivo físico (MedioPagoId == 1 corresponde a Efectivo según Seed Data)
            decimal totalVentasEfectivo = caja.Ventas
                .SelectMany(v => v.Pagos)
                .Where(p => p.MedioPagoId == 1)
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
            await _unitOfWork.SaveChangesAsync();

            return ApiResponse<decimal>.Exito(
                caja.Diferencia.Value,
                $"Caja cerrada correctamente. Esperado en efectivo: ${esperado:N2} | Contado: ${dto.MontoFinalReal:N2} | Diferencia: ${caja.Diferencia.Value:N2}");
        }
    }
}