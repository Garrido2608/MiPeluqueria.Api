using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MiPeluqueria.Api.Data.Context;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Stock;
using MiPeluqueria.Api.Models.Stock;
using MiPeluqueria.Api.Models.Ventas;
using MiPeluqueria.Api.Repositories.Interfaces;
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Services.Implementations
{
    public class OrdenCompraService : IOrdenCompraService
    {
        private readonly IOrdenCompraRepository _ordenCompraRepo;
        private readonly IUnitOfWork _uow;
        private readonly ApplicationDbContext _context;

        public OrdenCompraService(
            IOrdenCompraRepository ordenCompraRepo,
            IUnitOfWork uow,
            ApplicationDbContext context)
        {
            _ordenCompraRepo = ordenCompraRepo;
            _uow = uow;
            _context = context;
        }

        public async Task<ApiResponse<IReadOnlyList<OrdenCompraResponseDto>>> ObtenerTodasAsync(
            int? proveedorId = null,
            EstadoOrdenCompraEnum? estado = null,
            CancellationToken ct = default)
        {
            var ordenes = await _ordenCompraRepo.GetAllAsync(proveedorId, estado, ct);
            return ApiResponse<IReadOnlyList<OrdenCompraResponseDto>>.Exito(ordenes.Select(MapToDto).ToList());
        }

        public async Task<ApiResponse<OrdenCompraResponseDto>> ObtenerPorIdAsync(int id, CancellationToken ct = default)
        {
            var orden = await _ordenCompraRepo.GetByIdCompletaAsync(id, ct);
            if (orden == null)
                return ApiResponse<OrdenCompraResponseDto>.Falla($"La orden de compra #{id} no existe.");

            return ApiResponse<OrdenCompraResponseDto>.Exito(MapToDto(orden));
        }

        public async Task<ApiResponse<OrdenCompraResponseDto>> CrearOrdenAsync(
            CrearOrdenCompraDto dto,
            int usuarioId,
            CancellationToken ct = default)
        {
            if (dto.Detalles == null || !dto.Detalles.Any())
                return ApiResponse<OrdenCompraResponseDto>.Falla("La orden de compra debe tener al menos un ítem.");

            var orden = new OrdenCompra
            {
                ProveedorId = dto.ProveedorId,
                NumeroComprobanteProveedor = dto.NumeroComprobanteProveedor,
                Observaciones = dto.Observaciones,
                FechaEmision = DateTime.UtcNow,
                Estado = EstadoOrdenCompraEnum.Emitida,
                UsuarioCreacionId = usuarioId,
                CreatedByUserId = usuarioId
            };

            decimal total = 0;
            foreach (var item in dto.Detalles)
            {
                var producto = await _uow.Productos.GetByIdAsync(item.ProductoId);
                if (producto == null)
                    return ApiResponse<OrdenCompraResponseDto>.Falla($"El producto con ID {item.ProductoId} no existe.");

                decimal subtotal = item.CantidadPedida * item.PrecioUnitarioCosto;
                total += subtotal;

                orden.Detalles.Add(new DetalleOrdenCompra
                {
                    ProductoId = item.ProductoId,
                    CantidadPedida = item.CantidadPedida,
                    CantidadRecibida = 0,
                    PrecioUnitarioCosto = item.PrecioUnitarioCosto,
                    Subtotal = subtotal,
                    CreatedByUserId = usuarioId
                });
            }

            orden.Total = total;

            await _ordenCompraRepo.AddAsync(orden, ct);
            await _uow.SaveChangesAsync(ct);

            var creada = await _ordenCompraRepo.GetByIdCompletaAsync(orden.Id, ct);
            return ApiResponse<OrdenCompraResponseDto>.Exito(MapToDto(creada ?? orden), "Orden de compra emitida correctamente.");
        }

        public async Task<ApiResponse<OrdenCompraResponseDto>> RecepcionarOrdenAsync(
            int id,
            RecepcionarOrdenCompraDto dto,
            int usuarioId,
            CancellationToken ct = default)
        {
            var orden = await _ordenCompraRepo.GetByIdCompletaAsync(id, ct);
            if (orden == null)
                return ApiResponse<OrdenCompraResponseDto>.Falla("La orden de compra no existe.");

            if (orden.Estado != EstadoOrdenCompraEnum.Emitida && orden.Estado != EstadoOrdenCompraEnum.Borrador)
                return ApiResponse<OrdenCompraResponseDto>.Falla($"No se puede recepcionar una orden en estado {orden.Estado}.");

            await _uow.BeginTransactionAsync(ct);
            try
            {
                decimal totalReal = 0;

                foreach (var detalle in orden.Detalles)
                {
                    var overrideItem = dto.CantidadesRecibidas?.FirstOrDefault(x => x.ProductoId == detalle.ProductoId);
                    int cantRecibida = overrideItem?.CantidadRecibida ?? detalle.CantidadPedida;

                    detalle.CantidadRecibida = cantRecibida;
                    detalle.Subtotal = cantRecibida * detalle.PrecioUnitarioCosto;
                    totalReal += detalle.Subtotal;

                    if (cantRecibida > 0)
                    {
                        var producto = await _uow.Productos.GetByIdAsync(detalle.ProductoId);
                        if (producto != null)
                        {
                            // Trazabilidad histórica de precios si cambió el costo
                            if (dto.ActualizarCostoProductos && producto.PrecioCosto != detalle.PrecioUnitarioCosto)
                            {
                                decimal nuevoPrecioVenta = overrideItem?.NuevoPrecioVentaSugerido ?? producto.PrecioVenta;

                                await _context.HistorialPreciosProducto.AddAsync(new HistorialPrecioProducto
                                {
                                    ProductoId = producto.Id,
                                    PrecioCostoAnterior = producto.PrecioCosto,
                                    PrecioCostoNuevo = detalle.PrecioUnitarioCosto,
                                    PrecioVentaAnterior = producto.PrecioVenta,
                                    PrecioVentaNuevo = nuevoPrecioVenta,
                                    FechaCambio = DateTime.UtcNow,
                                    UsuarioModificacionId = usuarioId,
                                    Motivo = $"Recepción Orden de Compra #{orden.Id}"
                                }, ct);

                                producto.PrecioCosto = detalle.PrecioUnitarioCosto;
                                producto.PrecioVenta = nuevoPrecioVenta;
                            }

                            producto.StockActual += cantRecibida;
                            await _uow.Productos.UpdateAsync(producto);

                            // Registro en Kardex (MovimientoStock)
                            await _uow.Productos.RegistrarMovimientoAsync(new MovimientoStock
                            {
                                ProductoId = producto.Id,
                                TipoMovimiento = TipoMovimientoStockEnum.IngresoCompra,
                                Cantidad = cantRecibida,
                                Fecha = DateTime.UtcNow,
                                UsuarioId = usuarioId,
                                Motivo = $"Ingreso por Orden de Compra #{orden.Id} ({dto.NumeroComprobanteProveedor ?? orden.NumeroComprobanteProveedor ?? "S/N"})"
                            });
                        }
                    }
                }

                orden.Total = totalReal;
                orden.Estado = EstadoOrdenCompraEnum.Recibida;
                orden.FechaRecepcion = DateTime.UtcNow;
                orden.UsuarioRecepcionId = usuarioId;
                if (!string.IsNullOrWhiteSpace(dto.NumeroComprobanteProveedor))
                    orden.NumeroComprobanteProveedor = dto.NumeroComprobanteProveedor;

                // Impacto opcional en Caja Abierta
                if (dto.ImputarEgresoEnCajaAbierta && totalReal > 0)
                {
                    var cajaAbierta = await _uow.SesionesCaja.GetCajaAbiertaActualAsync(ct);
                    if (cajaAbierta == null)
                    {
                        await _uow.RollbackTransactionAsync(ct);
                        return ApiResponse<OrdenCompraResponseDto>.Falla("No hay una sesión de caja abierta para imputar el egreso del pago al proveedor.");
                    }

                    await _uow.SesionesCaja.RegistrarMovimientoCajaAsync(new MovimientoCaja
                    {
                        SesionCajaId = cajaAbierta.Id,
                        Tipo = "PagoProveedor",
                        Monto = totalReal,
                        Concepto = $"Pago Orden de Compra #{orden.Id} - Prov: {orden.Proveedor?.RazonSocial}",
                        Fecha = DateTime.UtcNow,
                        CreatedByUserId = usuarioId
                    }, ct);
                }

                _ordenCompraRepo.Update(orden);
                await _uow.CommitTransactionAsync(ct);

                return ApiResponse<OrdenCompraResponseDto>.Exito(MapToDto(orden), "Mercadería recepcionada y stock actualizado exitosamente.");
            }
            catch (Exception ex)
            {
                await _uow.RollbackTransactionAsync(ct);
                return ApiResponse<OrdenCompraResponseDto>.Falla($"Error al recepcionar la orden de compra: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> CancelarOrdenAsync(int id, string motivo, int usuarioId, CancellationToken ct = default)
        {
            var orden = await _ordenCompraRepo.GetByIdCompletaAsync(id, ct);
            if (orden == null)
                return ApiResponse<bool>.Falla("La orden de compra no existe.");

            if (orden.Estado == EstadoOrdenCompraEnum.Recibida)
                return ApiResponse<bool>.Falla("No se puede cancelar una orden que ya fue recepcionada en stock.");

            orden.Estado = EstadoOrdenCompraEnum.Cancelada;
            orden.Observaciones = $"{orden.Observaciones} | Cancelada: {motivo}".Trim();
            _ordenCompraRepo.Update(orden);
            await _uow.SaveChangesAsync(ct);

            return ApiResponse<bool>.Exito(true, "Orden de compra cancelada correctamente.");
        }

        private static OrdenCompraResponseDto MapToDto(OrdenCompra o)
        {
            return new OrdenCompraResponseDto
            {
                Id = o.Id,
                ProveedorId = o.ProveedorId,
                ProveedorRazonSocial = o.Proveedor?.RazonSocial ?? string.Empty,
                NumeroComprobanteProveedor = o.NumeroComprobanteProveedor,
                FechaEmision = o.FechaEmision,
                FechaRecepcion = o.FechaRecepcion,
                Estado = o.Estado,
                EstadoNombre = o.Estado.ToString(),
                Total = o.Total,
                Observaciones = o.Observaciones,
                Detalles = o.Detalles.Select(d => new DetalleOrdenCompraResponseDto
                {
                    Id = d.Id,
                    ProductoId = d.ProductoId,
                    ProductoNombre = d.Producto?.Nombre ?? string.Empty,
                    CantidadPedida = d.CantidadPedida,
                    CantidadRecibida = d.CantidadRecibida,
                    PrecioUnitarioCosto = d.PrecioUnitarioCosto,
                    Subtotal = d.Subtotal
                }).ToList()
            };
        }
    }
}