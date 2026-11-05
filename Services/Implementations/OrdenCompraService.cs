using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Stock;
using MiPeluqueria.Api.Exceptions;
using MiPeluqueria.Api.Models.Stock;
using MiPeluqueria.Api.Models.Ventas;
using MiPeluqueria.Api.Repositories.Interfaces;
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Services.Implementations
{
    public class OrdenCompraService : IOrdenCompraService
    {
        private readonly IUnitOfWork _uow;

        public OrdenCompraService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<ApiResponse<PagedResult<OrdenCompraResponseDto>>> ObtenerTodasAsync(
            PaginationFilterDto filter,
            int? proveedorId = null,
            EstadoOrdenCompraEnum? estado = null,
            CancellationToken ct = default)
        {
            var effectiveFilter = filter ?? new PaginationFilterDto();
            var (items, totalRecords) = await _uow.OrdenesCompra.GetPagedAsync(effectiveFilter, proveedorId, estado, ct);

            var dtos = items.Select(MapToDto).ToList();

            var paged = new PagedResult<OrdenCompraResponseDto>
            {
                Data = dtos,
                TotalRecords = totalRecords,
                PageNumber = effectiveFilter.PageNumber,
                PageSize = effectiveFilter.PageSize
            };

            return ApiResponse<PagedResult<OrdenCompraResponseDto>>.Exito(paged);
        }

        public Task<ApiResponse<PagedResult<OrdenCompraResponseDto>>> ObtenerTodasAsync(
            int? proveedorId = null,
            EstadoOrdenCompraEnum? estado = null,
            CancellationToken ct = default)
        {
            return ObtenerTodasAsync(new PaginationFilterDto(), proveedorId, estado, ct);
        }

        public async Task<ApiResponse<OrdenCompraResponseDto>> ObtenerPorIdAsync(int id, CancellationToken ct = default)
        {
            var orden = await _uow.OrdenesCompra.GetByIdCompletaAsync(id, ct);
            if (orden == null)
                return ApiResponse<OrdenCompraResponseDto>.Falla($"La orden de compra #{id} no existe.");

            return ApiResponse<OrdenCompraResponseDto>.Exito(MapToDto(orden));
        }

        public async Task<ApiResponse<OrdenCompraResponseDto>> CrearOrdenAsync(
            CrearOrdenCompraDto dto,
            int usuarioId,
            CancellationToken ct = default)
        {
            if (dto.Detalles == null || dto.Detalles.Count == 0)
                return ApiResponse<OrdenCompraResponseDto>.Falla("La orden de compra debe incluir al menos un ítem.");

            var proveedor = await _uow.Proveedores.GetByIdAsync(dto.ProveedorId, ct);
            if (proveedor == null)
                return ApiResponse<OrdenCompraResponseDto>.Falla("El proveedor especificado no existe.");

            var productosIds = dto.Detalles.Select(d => d.ProductoId).Distinct().ToList();
            var productosDict = await _uow.Productos.GetByIdsDictionaryAsync(productosIds, tracking: false, ct);

            var orden = new OrdenCompra
            {
                ProveedorId = dto.ProveedorId,
                NumeroComprobanteProveedor = string.IsNullOrWhiteSpace(dto.NumeroComprobanteProveedor)
                    ? null
                    : Truncar(dto.NumeroComprobanteProveedor.Trim(), 50),
                Observaciones = string.IsNullOrWhiteSpace(dto.Observaciones)
                    ? null
                    : Truncar(dto.Observaciones.Trim(), 500),
                FechaEmision = DateTime.UtcNow,
                Estado = EstadoOrdenCompraEnum.Emitida,
                UsuarioCreacionId = usuarioId,
                CreatedByUserId = usuarioId,
                Detalles = new List<DetalleOrdenCompra>()
            };

            decimal total = 0m;
            foreach (var item in dto.Detalles)
            {
                if (!productosDict.ContainsKey(item.ProductoId))
                    return ApiResponse<OrdenCompraResponseDto>.Falla($"El producto con ID {item.ProductoId} no existe.");

                if (item.CantidadPedida <= 0)
                    return ApiResponse<OrdenCompraResponseDto>.Falla("La cantidad pedida de cada ítem debe ser mayor a cero.");

                if (item.PrecioUnitarioCosto < 0m)
                    return ApiResponse<OrdenCompraResponseDto>.Falla("El costo unitario no puede ser negativo.");

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

            await _uow.OrdenesCompra.AddAsync(orden, ct);
            await _uow.SaveChangesAsync(ct);

            var creada = await _uow.OrdenesCompra.GetByIdCompletaAsync(orden.Id, ct);
            return ApiResponse<OrdenCompraResponseDto>.Exito(
                MapToDto(creada ?? orden),
                "Orden de compra emitida correctamente.");
        }

        public async Task<ApiResponse<OrdenCompraResponseDto>> RecepcionarOrdenAsync(
            int id,
            RecepcionarOrdenCompraDto dto,
            int usuarioId,
            CancellationToken ct = default)
        {
            var ordenPrevia = await _uow.OrdenesCompra.GetByIdCompletaAsync(id, ct);
            if (ordenPrevia == null)
                return ApiResponse<OrdenCompraResponseDto>.Falla("La orden de compra solicitada no existe.");

            if (ordenPrevia.Estado != EstadoOrdenCompraEnum.Emitida &&
                ordenPrevia.Estado != EstadoOrdenCompraEnum.Borrador)
            {
                return ApiResponse<OrdenCompraResponseDto>.Falla(
                    $"No se puede recepcionar una orden de compra en estado {ordenPrevia.Estado}.");
            }

            try
            {
                await _uow.ExecuteInTransactionAsync(async token =>
                {
                    var orden = await _uow.OrdenesCompra.GetByIdCompletaAsync(id, token)
                        ?? throw new BusinessRuleException("La orden de compra solicitada no existe.");

                    if (orden.Estado != EstadoOrdenCompraEnum.Emitida &&
                        orden.Estado != EstadoOrdenCompraEnum.Borrador)
                    {
                        throw new BusinessRuleException(
                            $"No se puede recepcionar una orden de compra en estado {orden.Estado}.");
                    }

                    var productosIds = orden.Detalles.Select(d => d.ProductoId).Distinct().ToList();
                    var productosDict = await _uow.Productos.GetByIdsDictionaryAsync(productosIds, tracking: true, token);

                    decimal totalReal = 0m;

                    foreach (var detalle in orden.Detalles)
                    {
                        var overrideItem = dto.CantidadesRecibidas?
                            .FirstOrDefault(x => x.ProductoId == detalle.ProductoId);

                        int cantRecibida = overrideItem?.CantidadRecibida ?? detalle.CantidadPedida;
                        if (cantRecibida < 0)
                            throw new BusinessRuleException("La cantidad recibida no puede ser negativa.");

                        detalle.CantidadRecibida = cantRecibida;
                        detalle.Subtotal = cantRecibida * detalle.PrecioUnitarioCosto;
                        detalle.UpdatedByUserId = usuarioId;
                        totalReal += detalle.Subtotal;

                        if (cantRecibida > 0 && productosDict.TryGetValue(detalle.ProductoId, out var producto))
                        {
                            decimal nuevoPrecioCosto = dto.ActualizarCostoProductos
                                ? detalle.PrecioUnitarioCosto
                                : producto.PrecioCosto;

                            decimal nuevoPrecioVenta = dto.ActualizarCostoProductos &&
                                                       overrideItem?.NuevoPrecioVentaSugerido is > 0m
                                ? overrideItem.NuevoPrecioVentaSugerido.Value
                                : producto.PrecioVenta;

                            if (producto.PrecioCosto != nuevoPrecioCosto || producto.PrecioVenta != nuevoPrecioVenta)
                            {
                                await _uow.Productos.AddHistorialPrecioAsync(new HistorialPrecioProducto
                                {
                                    ProductoId = producto.Id,
                                    PrecioCostoAnterior = producto.PrecioCosto,
                                    PrecioCostoNuevo = nuevoPrecioCosto,
                                    PrecioVentaAnterior = producto.PrecioVenta,
                                    PrecioVentaNuevo = nuevoPrecioVenta,
                                    FechaCambio = DateTime.UtcNow,
                                    UsuarioModificacionId = usuarioId,
                                    Motivo = $"Recepción Orden de Compra #{orden.Id}",
                                    CreatedByUserId = usuarioId
                                }, token);

                                producto.PrecioCosto = nuevoPrecioCosto;
                                producto.PrecioVenta = nuevoPrecioVenta;
                            }

                            producto.StockActual += cantRecibida;
                            producto.UpdatedByUserId = usuarioId;
                            _uow.Productos.Update(producto);

                            string comprobante = !string.IsNullOrWhiteSpace(dto.NumeroComprobanteProveedor)
                                ? dto.NumeroComprobanteProveedor.Trim()
                                : (orden.NumeroComprobanteProveedor ?? "S/N");

                            await _uow.Productos.RegistrarMovimientoAsync(new MovimientoStock
                            {
                                ProductoId = producto.Id,
                                TipoMovimiento = TipoMovimientoStockEnum.IngresoCompra,
                                Cantidad = cantRecibida,
                                SaldoResultante = producto.StockActual,
                                Fecha = DateTime.UtcNow,
                                UsuarioId = usuarioId,
                                Motivo = Truncar($"Ingreso por Orden de Compra #{orden.Id} ({comprobante})", 300),
                                CreatedByUserId = usuarioId
                            }, token);
                        }
                    }

                    orden.Total = totalReal;
                    orden.Estado = EstadoOrdenCompraEnum.Recibida;
                    orden.FechaRecepcion = DateTime.UtcNow;
                    orden.UsuarioRecepcionId = usuarioId;
                    orden.UpdatedByUserId = usuarioId;

                    if (!string.IsNullOrWhiteSpace(dto.NumeroComprobanteProveedor))
                        orden.NumeroComprobanteProveedor = Truncar(dto.NumeroComprobanteProveedor.Trim(), 50);

                    if (!string.IsNullOrWhiteSpace(dto.ObservacionesRecepcion))
                    {
                        string prefijo = string.IsNullOrWhiteSpace(orden.Observaciones)
                            ? string.Empty
                            : $"{orden.Observaciones} | ";
                        orden.Observaciones = Truncar($"{prefijo}Recepción: {dto.ObservacionesRecepcion.Trim()}", 500);
                    }

                    if (dto.ImputarEgresoEnCajaAbierta && totalReal > 0m)
                    {
                        var cajaAbierta = await _uow.SesionesCaja.GetCajaAbiertaActualAsync(token)
                            ?? throw new BusinessRuleException(
                                "No hay una sesión de caja abierta para imputar el egreso del pago al proveedor.");

                        cajaAbierta.TotalEgresosManuales += totalReal;
                        cajaAbierta.UpdatedByUserId = usuarioId;
                        _uow.SesionesCaja.ActualizarSesionCaja(cajaAbierta);

                        await _uow.SesionesCaja.RegistrarMovimientoCajaAsync(new MovimientoCaja
                        {
                            SesionCajaId = cajaAbierta.Id,
                            Tipo = "PagoProveedor",
                            Monto = totalReal,
                            Concepto = Truncar(
                                $"Pago Orden de Compra #{orden.Id} - Prov: {orden.Proveedor?.RazonSocial ?? orden.ProveedorId.ToString()}",
                                250),
                            Fecha = DateTime.UtcNow,
                            CreatedByUserId = usuarioId
                        }, token);
                    }

                    _uow.OrdenesCompra.Update(orden);
                    await _uow.SaveChangesAsync(token);
                }, ct: ct);
            }
            catch (BusinessRuleException ex)
            {
                return ApiResponse<OrdenCompraResponseDto>.Falla(ex.Message);
            }

            var recepcionada = await _uow.OrdenesCompra.GetByIdCompletaAsync(id, ct);
            return ApiResponse<OrdenCompraResponseDto>.Exito(
                MapToDto(recepcionada ?? ordenPrevia),
                "Mercadería recepcionada y stock actualizado exitosamente.");
        }

        public async Task<ApiResponse<bool>> CancelarOrdenAsync(
            int id,
            string motivo,
            int usuarioId,
            CancellationToken ct = default)
        {
            var orden = await _uow.OrdenesCompra.GetByIdCompletaAsync(id, ct);
            if (orden == null)
                return ApiResponse<bool>.Falla("La orden de compra solicitada no existe.");

            if (orden.Estado == EstadoOrdenCompraEnum.Recibida)
                return ApiResponse<bool>.Falla("No se puede cancelar una orden que ya fue recepcionada en stock.");

            if (orden.Estado == EstadoOrdenCompraEnum.Cancelada)
                return ApiResponse<bool>.Falla("La orden de compra ya se encuentra cancelada.");

            orden.Estado = EstadoOrdenCompraEnum.Cancelada;
            orden.UpdatedByUserId = usuarioId;

            string detalleCancelacion = string.IsNullOrWhiteSpace(motivo)
                ? "Cancelación administrativa"
                : motivo.Trim();
            string prefijo = string.IsNullOrWhiteSpace(orden.Observaciones)
                ? string.Empty
                : $"{orden.Observaciones} | ";
            orden.Observaciones = Truncar($"{prefijo}Cancelada: {detalleCancelacion}", 500);

            _uow.OrdenesCompra.Update(orden);
            await _uow.SaveChangesAsync(ct);

            return ApiResponse<bool>.Exito(true, "Orden de compra cancelada correctamente.");
        }

        private static OrdenCompraResponseDto MapToDto(OrdenCompra o) => new()
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

        private static string Truncar(string valor, int maxLen) =>
            valor.Length <= maxLen ? valor : valor.Substring(0, maxLen);
    }
}
