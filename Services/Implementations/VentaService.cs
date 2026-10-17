using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Ventas;
using MiPeluqueria.Api.Models.Stock;
using MiPeluqueria.Api.Models.Ventas;
using MiPeluqueria.Api.Repositories.Interfaces;
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Services.Implementations
{
    public class VentaService : IVentaService
    {
        private readonly IUnitOfWork _unitOfWork;

        public VentaService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ApiResponse<VentaResponseDto>> ObtenerVentaPorIdAsync(int id, CancellationToken ct = default)
        {
            var venta = await _unitOfWork.Ventas.GetByIdCompletaAsync(id, ct);
            if (venta == null)
                return ApiResponse<VentaResponseDto>.Fail($"No se encontró la venta con ID {id}.");

            var dto = await MapToResponseDtoAsync(venta, ct);
            return ApiResponse<VentaResponseDto>.Ok(dto);
        }

        public async Task<ApiResponse<IReadOnlyList<VentaResponseDto>>> ObtenerVentasPorSesionAsync(int sesionCajaId, CancellationToken ct = default)
        {
            var ventas = await _unitOfWork.Ventas.GetBySesionCajaIdAsync(sesionCajaId, ct);
            var lista = new List<VentaResponseDto>(ventas.Count);

            foreach (var v in ventas)
            {
                lista.Add(await MapToResponseDtoAsync(v, ct));
            }

            return ApiResponse<IReadOnlyList<VentaResponseDto>>.Ok(lista);
        }

        public async Task<ApiResponse<VentaResponseDto>> RegistrarVentaAsync(CrearVentaDto dto, int usuarioId, CancellationToken ct = default)
        {
            var cajaAbierta = await _unitOfWork.SesionesCaja.GetCajaAbiertaActualAsync(ct);
            if (cajaAbierta == null)
                return ApiResponse<VentaResponseDto>.Fail("Debe abrir una sesión de caja antes de registrar ventas.");

            int sesionId = dto.SesionCajaId > 0 ? dto.SesionCajaId : cajaAbierta.Id;

            await _unitOfWork.BeginTransactionAsync(ct);
            try
            {
                var venta = new Venta
                {
                    SesionCajaId = sesionId,
                    ClienteId = dto.ClienteId,
                    TurnoId = dto.TurnoId,
                    UsuarioId = usuarioId,
                    Fecha = DateTime.UtcNow,
                    DetallesVenta = new List<DetalleVenta>(),
                    Pagos = new List<Pago>()
                };

                decimal totalVenta = 0m;

                foreach (var item in dto.Detalles)
                {
                    decimal precioUnitario = 0m;

                    if (item.TipoItem == TipoItemVentaEnum.Producto)
                    {
                        var producto = await _unitOfWork.Productos.GetByIdAsync(item.ItemId);
                        if (producto == null)
                        {
                            await _unitOfWork.RollbackTransactionAsync(ct);
                            return ApiResponse<VentaResponseDto>.Fail($"El producto con ID {item.ItemId} no existe.");
                        }

                        if (producto.StockActual < item.Cantidad)
                        {
                            await _unitOfWork.RollbackTransactionAsync(ct);
                            return ApiResponse<VentaResponseDto>.Fail($"Stock insuficiente para '{producto.Nombre}'. Disponible: {producto.StockActual}.");
                        }

                        precioUnitario = producto.PrecioVenta;
                        producto.StockActual -= item.Cantidad;

                        await _unitOfWork.Productos.RegistrarMovimientoAsync(new MovimientoStock
                        {
                            ProductoId = producto.Id,
                            TipoMovimiento = TipoMovimientoStockEnum.EgresoVentaMostrador,
                            Cantidad = item.Cantidad,
                            Motivo = $"Venta en caja #{sesionId}"
                        });
                    }
                    else
                    {
                        var datosServicio = await _unitOfWork.Ventas.GetDatosServicioAsync(item.ItemId, ct);
                        if (datosServicio == null)
                        {
                            await _unitOfWork.RollbackTransactionAsync(ct);
                            return ApiResponse<VentaResponseDto>.Fail($"El servicio con ID {item.ItemId} no existe.");
                        }

                        precioUnitario = datosServicio.Value.Precio;
                    }

                    decimal subtotalItem = precioUnitario * item.Cantidad;
                    totalVenta += subtotalItem;

                    venta.DetallesVenta.Add(new DetalleVenta
                    {
                        TipoItem = item.TipoItem,
                        ItemId = item.ItemId,
                        Cantidad = item.Cantidad,
                        PrecioUnitario = precioUnitario,
                        Subtotal = subtotalItem,
                        PeluqueroId = item.PeluqueroId
                    });
                }

                venta.Total = totalVenta;

                decimal sumaPagos = dto.Pagos.Sum(p => p.Importe);
                if (Math.Abs(sumaPagos - venta.Total) > 0.01m)
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return ApiResponse<VentaResponseDto>.Fail($"La suma de los pagos ({sumaPagos:C}) no coincide con el total de la venta ({venta.Total:C}).");
                }

                foreach (var pagoDto in dto.Pagos)
                {
                    venta.Pagos.Add(new Pago
                    {
                        MedioPagoId = pagoDto.MedioPagoId,
                        Importe = pagoDto.Importe
                    });
                }

                await _unitOfWork.Ventas.AddAsync(venta, ct);
                await _unitOfWork.CommitTransactionAsync(ct);

                var ventaCompleta = await _unitOfWork.Ventas.GetByIdCompletaAsync(venta.Id, ct);
                var responseDto = await MapToResponseDtoAsync(ventaCompleta!, ct);

                return ApiResponse<VentaResponseDto>.Ok(responseDto, "Venta registrada exitosamente.");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                return ApiResponse<VentaResponseDto>.Fail($"Error al procesar la venta: {ex.Message}");
            }
        }

        public async Task<ApiResponse<VentaResponseDto>> AnularVentaAsync(int ventaId, string motivo, int usuarioId, CancellationToken ct = default)
        {
            var venta = await _unitOfWork.Ventas.GetByIdCompletaAsync(ventaId, ct);
            if (venta == null)
                return ApiResponse<VentaResponseDto>.Fail($"No se encontró la venta con ID {ventaId}.");

            if (venta.IsDeleted)
                return ApiResponse<VentaResponseDto>.Fail("La venta ya se encuentra anulada.");

            await _unitOfWork.BeginTransactionAsync(ct);
            try
            {
                foreach (var detalle in venta.DetallesVenta.Where(d => d.TipoItem == TipoItemVentaEnum.Producto))
                {
                    var producto = await _unitOfWork.Productos.GetByIdAsync(detalle.ItemId);
                    if (producto != null)
                    {
                        producto.StockActual += detalle.Cantidad;
                        await _unitOfWork.Productos.RegistrarMovimientoAsync(new MovimientoStock
                        {
                            ProductoId = producto.Id,
                            TipoMovimiento = TipoMovimientoStockEnum.IngresoCompra,
                            Cantidad = detalle.Cantidad,
                            Motivo = $"Restitución por Anulación de Venta #{venta.Id}: {motivo}"
                        });
                    }
                }

                venta.IsDeleted = true;
                venta.DeletedAt = DateTime.UtcNow;
                _unitOfWork.Ventas.Update(venta);

                await _unitOfWork.CommitTransactionAsync(ct);

                var responseDto = await MapToResponseDtoAsync(venta, ct);
                return ApiResponse<VentaResponseDto>.Ok(responseDto, "Venta anulada y stock restituido correctamente.");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                return ApiResponse<VentaResponseDto>.Fail($"Error al anular la venta: {ex.Message}");
            }
        }

        private async Task<VentaResponseDto> MapToResponseDtoAsync(Venta v, CancellationToken ct)
        {
            var detallesDto = new List<VentaDetalleItemResponseDto>();

            foreach (var d in v.DetallesVenta)
            {
                string conceptoNombre;
                if (d.TipoItem == TipoItemVentaEnum.Producto)
                {
                    var prod = await _unitOfWork.Productos.GetByIdAsync(d.ItemId);
                    conceptoNombre = prod?.Nombre ?? $"Producto #{d.ItemId}";
                }
                else
                {
                    var serv = await _unitOfWork.Ventas.GetDatosServicioAsync(d.ItemId, ct);
                    conceptoNombre = serv?.Nombre ?? $"Servicio #{d.ItemId}";
                }

                string? peluqueroNombre = null;
                if (d.PeluqueroId.HasValue)
                {
                    peluqueroNombre = await _unitOfWork.Ventas.GetNombrePeluqueroAsync(d.PeluqueroId.Value, ct);
                }

                detallesDto.Add(new VentaDetalleItemResponseDto
                {
                    Id = d.Id,
                    TipoItem = d.TipoItem.ToString(),
                    ItemId = d.ItemId,
                    ConceptoNombre = conceptoNombre,
                    Cantidad = d.Cantidad,
                    PrecioUnitario = d.PrecioUnitario,
                    Subtotal = d.Subtotal,
                    PeluqueroId = d.PeluqueroId,
                    PeluqueroNombre = peluqueroNombre
                });
            }

            return new VentaResponseDto
            {
                Id = v.Id,
                SesionCajaId = v.SesionCajaId,
                ClienteId = v.ClienteId,
                ClienteNombre = v.Cliente != null ? $"{v.Cliente.Nombre} {v.Cliente.Apellido}".Trim() : "Consumidor Final",
                TurnoId = v.TurnoId,
                UsuarioId = v.UsuarioId,
                Fecha = v.Fecha,
                Total = v.Total,
                Detalles = detallesDto,
                Pagos = v.Pagos.Select(p => new VentaPagoItemResponseDto
                {
                    Id = p.Id,
                    MedioPagoId = p.MedioPagoId,
                    MedioPagoNombre = p.MedioPago?.Nombre ?? string.Empty,
                    Importe = p.Importe
                }).ToList()
            };
        }
    }
}