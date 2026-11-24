using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Ventas;
using MiPeluqueria.Api.Exceptions;
using MiPeluqueria.Api.Models.Stock;
using MiPeluqueria.Api.Models.Turnos;
using MiPeluqueria.Api.Models.Ventas;
using MiPeluqueria.Api.Repositories.Interfaces;
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Services.Implementations
{
    public class VentaService : IVentaService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRecetaServicioService _recetaServicioService;

        public VentaService(
            IUnitOfWork unitOfWork,
            IRecetaServicioService recetaServicioService)
        {
            _unitOfWork = unitOfWork;
            _recetaServicioService = recetaServicioService;
        }

        public async Task<ApiResponse<VentaResponseDto>> ObtenerVentaPorIdAsync(int id, CancellationToken ct = default)
        {
            var venta = await _unitOfWork.Ventas.GetByIdCompletaAsync(id, ct);
            if (venta == null)
                return ApiResponse<VentaResponseDto>.Fail($"No se encontró la venta con ID {id}.");

            var dto = await MapSingleToResponseDtoAsync(venta, ct);
            return ApiResponse<VentaResponseDto>.Ok(dto);
        }

        public async Task<ApiResponse<PagedResult<VentaResponseDto>>> ObtenerVentasPorSesionAsync(
            int sesionCajaId,
            CancellationToken ct = default)
        {
            return await ObtenerVentasPorSesionAsync(sesionCajaId, new PaginationFilterDto(), ct);
        }

        public async Task<ApiResponse<PagedResult<VentaResponseDto>>> ObtenerVentasPorSesionAsync(
            int sesionCajaId,
            PaginationFilterDto filter,
            CancellationToken ct = default)
        {
            filter ??= new PaginationFilterDto();

            var (items, totalRecords) = await _unitOfWork.Ventas.GetBySesionCajaIdPagedAsync(sesionCajaId, filter, ct);
            var mappedItems = await MapBatchToResponseDtosAsync(items, ct);

            var pagedResult = new PagedResult<VentaResponseDto>
            {
                Data = mappedItems,
                TotalRecords = totalRecords,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };

            return ApiResponse<PagedResult<VentaResponseDto>>.Ok(pagedResult, "Listado de ventas de la sesión obtenido.");
        }

        public async Task<ApiResponse<VentaResponseDto>> RegistrarVentaAsync(
            CrearVentaDto dto,
            int usuarioId,
            CancellationToken ct = default)
        {
            string? normalizedKey = string.IsNullOrWhiteSpace(dto.IdempotencyKey)
                ? null
                : Truncar(dto.IdempotencyKey.Trim(), 64);

            // 1. Control de Idempotencia POS al inicio (X-Idempotency-Key / dto.IdempotencyKey)
            if (normalizedKey != null)
            {
                var ventaExistente = await _unitOfWork.Ventas.GetByIdempotencyKeyAsync(normalizedKey, ct);
                if (ventaExistente != null)
                {
                    var dtoExistente = await MapSingleToResponseDtoAsync(ventaExistente, ct);
                    return ApiResponse<VentaResponseDto>.Ok(
                        dtoExistente,
                        "Venta recuperada de forma idempotente (operación ya registrada previamente).");
                }
            }

            if (dto.Detalles == null || dto.Detalles.Count == 0)
                return ApiResponse<VentaResponseDto>.Fail("La venta debe contener al menos un ítem en el detalle.");

            if (dto.Pagos == null || dto.Pagos.Count == 0)
                return ApiResponse<VentaResponseDto>.Fail("Debe registrar al menos un medio de pago.");

            var mediosPago = await _unitOfWork.Catalogos.GetMediosPagoAsync(ct);
            var mediosPagoDict = mediosPago.ToDictionary(m => m.Id);

            foreach (var pagoDto in dto.Pagos)
            {
                if (pagoDto.Importe < 0m)
                    return ApiResponse<VentaResponseDto>.Fail("El importe de los pagos no puede ser negativo.");

                if (!mediosPagoDict.ContainsKey(pagoDto.MedioPagoId))
                    return ApiResponse<VentaResponseDto>.Fail($"El medio de pago con ID {pagoDto.MedioPagoId} no existe o se encuentra inactivo.");
            }

            int ventaCreadaId = 0;
            var warnings = new List<string>();

            try
            {
                await _unitOfWork.ExecuteInTransactionAsync(async token =>
                {
                    warnings.Clear();

                    if (normalizedKey != null)
                    {
                        var duplicadaEnTx = await _unitOfWork.Ventas.GetByIdempotencyKeyAsync(normalizedKey, token);
                        if (duplicadaEnTx != null)
                        {
                            ventaCreadaId = duplicadaEnTx.Id;
                            return;
                        }
                    }

                    var cajaAbierta = await _unitOfWork.SesionesCaja.GetCajaAbiertaActualAsync(token)
                        ?? throw new BusinessRuleException("Debe abrir una sesión de caja antes de registrar ventas.");

                    if (dto.SesionCajaId > 0 && dto.SesionCajaId != cajaAbierta.Id)
                    {
                        throw new BusinessRuleException(
                            $"La sesión de caja indicada (#{dto.SesionCajaId}) no coincide con la sesión de caja abierta actual (#{cajaAbierta.Id}).");
                    }

                    Turno? turno = null;
                    if (dto.TurnoId.HasValue && dto.TurnoId.Value > 0)
                    {
                        turno = await _unitOfWork.Turnos.GetByIdConDetallesAsync(dto.TurnoId.Value, tracking: true, token)
                            ?? throw new BusinessRuleException($"El turno con ID {dto.TurnoId.Value} no existe.");

                        if (turno.EstadoId == (int)EstadoTurnoEnum.Cancelado || turno.EstadoId == (int)EstadoTurnoEnum.NoAsistio)
                            throw new BusinessRuleException("No se puede facturar un turno cancelado o registrado como inasistencia.");
                    }

                    int? clienteIdFinal = (dto.ClienteId.HasValue && dto.ClienteId.Value > 0)
                        ? dto.ClienteId.Value
                        : turno?.ClienteId;

                    if (clienteIdFinal.HasValue)
                    {
                        bool existeCliente = await _unitOfWork.Clientes.ExistsAsync(c => c.Id == clienteIdFinal.Value, token);
                        if (!existeCliente)
                            throw new BusinessRuleException($"El cliente con ID {clienteIdFinal.Value} no existe.");
                    }

                    // 2. Precarga en lote O(1) de Productos, Servicios y Peluqueros explícitos
                    var productoIds = dto.Detalles
                        .Where(d => d.TipoItem == TipoItemVentaEnum.Producto)
                        .Select(d => d.ItemId)
                        .Distinct()
                        .ToList();

                    var servicioIds = dto.Detalles
                        .Where(d => d.TipoItem == TipoItemVentaEnum.Servicio)
                        .Select(d => d.ItemId)
                        .Distinct()
                        .ToList();

                    var peluqueroIdsExplicitos = dto.Detalles
                        .Where(d => d.PeluqueroId.HasValue && d.PeluqueroId.Value > 0)
                        .Select(d => d.PeluqueroId!.Value)
                        .Distinct()
                        .ToList();

                    var productosDict = productoIds.Count > 0
                        ? await _unitOfWork.Productos.GetByIdsDictionaryAsync(productoIds, tracking: true, token)
                        : new Dictionary<int, Producto>();

                    var serviciosDict = servicioIds.Count > 0
                        ? await _unitOfWork.Servicios.GetByIdsDictionaryAsync(servicioIds, tracking: false, token)
                        : new Dictionary<int, Servicio>();

                    if (peluqueroIdsExplicitos.Count > 0)
                    {
                        var peluquerosValidos = await _unitOfWork.Peluqueros.GetByIdsDictionaryAsync(peluqueroIdsExplicitos, tracking: false, token);
                        foreach (var pid in peluqueroIdsExplicitos)
                        {
                            if (!peluquerosValidos.ContainsKey(pid))
                                throw new BusinessRuleException($"El profesional con ID {pid} indicado en el detalle no existe.");
                        }
                    }

                    var venta = new Venta
                    {
                        SesionCajaId = cajaAbierta.Id,
                        ClienteId = clienteIdFinal,
                        TurnoId = turno?.Id,
                        UsuarioId = usuarioId,
                        Fecha = DateTime.UtcNow,
                        IdempotencyKey = normalizedKey,
                        CreatedByUserId = usuarioId,
                        DetallesVenta = new List<DetalleVenta>(),
                        Pagos = new List<Pago>()
                    };

                    decimal subtotalVenta = 0m;

                    foreach (var item in dto.Detalles)
                    {
                        if (item.Cantidad <= 0)
                            throw new BusinessRuleException("La cantidad de cada ítem de la venta debe ser al menos 1.");

                        decimal precioUnitario;
                        int? peluqueroAsignadoId = (item.PeluqueroId.HasValue && item.PeluqueroId.Value > 0)
                            ? item.PeluqueroId.Value
                            : turno?.PeluqueroId;

                        if (item.TipoItem == TipoItemVentaEnum.Producto)
                        {
                            if (!productosDict.TryGetValue(item.ItemId, out var producto) || producto.IsDeleted)
                                throw new BusinessRuleException($"El producto con ID {item.ItemId} no existe o se encuentra dado de baja.");

                            if (producto.StockActual < item.Cantidad)
                                throw new BusinessRuleException($"Stock insuficiente para '{producto.Nombre}'. Disponible: {producto.StockActual}.");

                            precioUnitario = producto.PrecioVenta;
                            producto.StockActual -= item.Cantidad;
                            producto.UpdatedByUserId = usuarioId;
                            _unitOfWork.Productos.Update(producto);

                            await _unitOfWork.Productos.RegistrarMovimientoAsync(new MovimientoStock
                            {
                                ProductoId = producto.Id,
                                TipoMovimiento = TipoMovimientoStockEnum.EgresoVentaMostrador,
                                Cantidad = item.Cantidad,
                                SaldoResultante = producto.StockActual,
                                Fecha = DateTime.UtcNow,
                                UsuarioId = usuarioId,
                                Motivo = $"Venta en caja #{cajaAbierta.Id}",
                                CreatedByUserId = usuarioId
                            }, token);
                        }
                        else if (item.TipoItem == TipoItemVentaEnum.Servicio)
                        {
                            if (!serviciosDict.TryGetValue(item.ItemId, out var servicio))
                                throw new BusinessRuleException($"El servicio con ID {item.ItemId} no existe.");

                            var tsHistorico = turno?.TurnoServicios
                                .FirstOrDefault(ts => !ts.IsDeleted && ts.ServicioId == servicio.Id);

                            precioUnitario = (tsHistorico != null && tsHistorico.PrecioHistorico > 0m)
                                ? tsHistorico.PrecioHistorico
                                : servicio.Precio;
                        }
                        else
                        {
                            throw new BusinessRuleException($"Tipo de ítem de venta inválido ({item.TipoItem}).");
                        }

                        decimal subtotalItem = Math.Round(precioUnitario * item.Cantidad, 2);
                        subtotalVenta += subtotalItem;

                        venta.DetallesVenta.Add(new DetalleVenta
                        {
                            TipoItem = item.TipoItem,
                            ItemId = item.ItemId,
                            Cantidad = item.Cantidad,
                            PrecioUnitario = precioUnitario,
                            Subtotal = subtotalItem,
                            PeluqueroId = peluqueroAsignadoId,
                            CreatedByUserId = usuarioId
                        });
                    }

                    // 3. Deducción automática de Seña abonada en el Turno
                    decimal descuentoSena = 0m;
                    if (turno != null && turno.SenaPagada && turno.MontoSena > 0m)
                    {
                        bool senaYaDescontada = await _unitOfWork.Ventas.ExisteVentaActivaConSenaPorTurnoAsync(turno.Id, null, token);
                        if (!senaYaDescontada)
                        {
                            descuentoSena = Math.Min(subtotalVenta, turno.MontoSena);
                        }
                    }

                    decimal totalVenta = Math.Max(0m, subtotalVenta - descuentoSena);
                    venta.Subtotal = subtotalVenta;
                    venta.DescuentoSenaAplicado = descuentoSena;
                    venta.Total = totalVenta;

                    decimal sumaPagos = dto.Pagos.Sum(p => p.Importe);
                    if (Math.Abs(sumaPagos - venta.Total) > 0.01m)
                    {
                        throw new BusinessRuleException(
                            $"La suma de los pagos ({sumaPagos:N2}) no coincide con el total neto de la venta ({venta.Total:N2}).");
                    }

                    decimal incrementoEfectivo = 0m;
                    decimal incrementoDigital = 0m;

                    foreach (var pagoDto in dto.Pagos)
                    {
                        var medio = mediosPagoDict[pagoDto.MedioPagoId];
                        if (medio.EsEfectivoFisico)
                            incrementoEfectivo += pagoDto.Importe;
                        else
                            incrementoDigital += pagoDto.Importe;

                        venta.Pagos.Add(new Pago
                        {
                            MedioPagoId = pagoDto.MedioPagoId,
                            Importe = pagoDto.Importe,
                            ReferenciaOperacion = string.IsNullOrWhiteSpace(pagoDto.ReferenciaOperacion)
                                ? null
                                : Truncar(pagoDto.ReferenciaOperacion.Trim(), 100),
                            CreatedByUserId = usuarioId
                        });
                    }

                    await _unitOfWork.Ventas.AddAsync(venta, token);

                    // 4. Actualización de acumuladores físicos vs digitales y MontoFinalEsperado en SesionCaja
                    var pagosPrevios = cajaAbierta.Ventas
                        .Where(v => !v.IsDeleted)
                        .SelectMany(v => v.Pagos)
                        .Where(p => !p.IsDeleted)
                        .ToList();

                    decimal efectivoPrevio = pagosPrevios
                        .Where(p => p.MedioPago != null && p.MedioPago.EsEfectivoFisico)
                        .Sum(p => p.Importe);

                    decimal digitalPrevio = pagosPrevios
                        .Where(p => p.MedioPago == null || !p.MedioPago.EsEfectivoFisico)
                        .Sum(p => p.Importe);

                    decimal ingresosManuales = cajaAbierta.MovimientosCaja
                        .Where(m => !m.IsDeleted && m.Tipo.StartsWith("Ingreso", StringComparison.OrdinalIgnoreCase))
                        .Sum(m => m.Monto);

                    decimal egresosManuales = cajaAbierta.MovimientosCaja
                        .Where(m => !m.IsDeleted && !m.Tipo.StartsWith("Ingreso", StringComparison.OrdinalIgnoreCase))
                        .Sum(m => m.Monto);

                    cajaAbierta.TotalVentasEfectivo = efectivoPrevio + incrementoEfectivo;
                    cajaAbierta.TotalVentasDigital = digitalPrevio + incrementoDigital;
                    cajaAbierta.TotalIngresosManuales = ingresosManuales;
                    cajaAbierta.TotalEgresosManuales = egresosManuales;
                    cajaAbierta.MontoFinalEsperado = cajaAbierta.MontoInicial
                        + cajaAbierta.TotalVentasEfectivo
                        + cajaAbierta.TotalIngresosManuales
                        - cajaAbierta.TotalEgresosManuales;
                    cajaAbierta.UpdatedByUserId = usuarioId;
                    _unitOfWork.SesionesCaja.ActualizarSesionCaja(cajaAbierta);

                    // 5. Autocompletado del Turno a Completado (4) dentro de la misma transacción
                    if (turno != null && turno.EstadoId != (int)EstadoTurnoEnum.Completado)
                    {
                        int estadoAnterior = turno.EstadoId;
                        turno.EstadoId = (int)EstadoTurnoEnum.Completado;
                        turno.UpdatedAt = DateTime.UtcNow;
                        turno.UpdatedByUserId = usuarioId;
                        _unitOfWork.Turnos.Update(turno);

                        await _unitOfWork.Turnos.AddHistorialEstadoAsync(new HistorialEstadoTurno
                        {
                            TurnoId = turno.Id,
                            EstadoAnteriorId = estadoAnterior,
                            EstadoNuevoId = (int)EstadoTurnoEnum.Completado,
                            FechaCambio = DateTime.UtcNow,
                            UsuarioId = usuarioId,
                            Observaciones = "Turno completado automáticamente al registrar cobro en POS.",
                            CreatedByUserId = usuarioId
                        }, token);
                    }

                    await _unitOfWork.SaveChangesAsync(token);
                    ventaCreadaId = venta.Id;

                    if (turno != null)
                    {
                        var recetaRes = await _recetaServicioService.AplicarConsumoGabinetePorTurnoAsync(turno.Id, usuarioId, token);
                        if (recetaRes.Warnings != null && recetaRes.Warnings.Count > 0)
                        {
                            warnings.AddRange(recetaRes.Warnings);
                        }
                    }
                }, IsolationLevel.ReadCommitted, ct);

                var ventaCompleta = await _unitOfWork.Ventas.GetByIdCompletaAsync(ventaCreadaId, ct);
                var responseDto = await MapSingleToResponseDtoAsync(ventaCompleta!, ct);

                return ApiResponse<VentaResponseDto>.Ok(
                    responseDto,
                    "Venta registrada exitosamente.",
                    warnings.Count > 0 ? warnings : null);
            }
            catch (BusinessRuleException ex)
            {
                return ApiResponse<VentaResponseDto>.Fail(ex.Message);
            }
            catch (DbUpdateException) when (normalizedKey != null)
            {
                var ventaConcurrente = await _unitOfWork.Ventas.GetByIdempotencyKeyAsync(normalizedKey, ct);
                if (ventaConcurrente != null)
                {
                    var dtoConcurrente = await MapSingleToResponseDtoAsync(ventaConcurrente, ct);
                    return ApiResponse<VentaResponseDto>.Ok(
                        dtoConcurrente,
                        "Venta recuperada de forma idempotente (operación ya registrada previamente).");
                }

                throw;
            }
        }

        public async Task<ApiResponse<VentaResponseDto>> AnularVentaAsync(
            int ventaId,
            string motivo,
            int usuarioId,
            CancellationToken ct = default)
        {
            var ventaPrevia = await _unitOfWork.Ventas.GetByIdCompletaAsync(ventaId, ct);
            if (ventaPrevia == null)
                return ApiResponse<VentaResponseDto>.Fail($"No se encontró la venta con ID {ventaId}.");

            if (ventaPrevia.IsDeleted)
                return ApiResponse<VentaResponseDto>.Fail("La venta ya se encuentra anulada.");

            if (ventaPrevia.SesionCaja == null || !ventaPrevia.SesionCaja.EstadoAbierta)
                return ApiResponse<VentaResponseDto>.Fail("No se permite anular una venta perteneciente a una sesión de caja ya cerrada.");

            if (await _unitOfWork.Ventas.TieneRenglonesEnLiquidacionActivaAsync(ventaId, ct))
                return ApiResponse<VentaResponseDto>.Fail("No se permite anular una venta cuyos renglones ya se encuentran incluidos en una liquidación de comisiones activa.");

            string motivoNormalizado = string.IsNullOrWhiteSpace(motivo)
                ? "Anulación de comprobante en mostrador."
                : motivo.Trim();

            VentaResponseDto? responseDto = null;

            try
            {
                await _unitOfWork.ExecuteInTransactionAsync(async token =>
                {
                    var venta = await _unitOfWork.Ventas.GetByIdCompletaAsync(ventaId, token)
                        ?? throw new BusinessRuleException($"No se encontró la venta con ID {ventaId}.");

                    if (venta.IsDeleted)
                        throw new BusinessRuleException("La venta ya se encuentra anulada.");

                    if (venta.SesionCaja == null || !venta.SesionCaja.EstadoAbierta)
                        throw new BusinessRuleException("No se permite anular una venta perteneciente a una sesión de caja ya cerrada.");

                    if (await _unitOfWork.Ventas.TieneRenglonesEnLiquidacionActivaAsync(ventaId, token))
                        throw new BusinessRuleException("No se permite anular una venta cuyos renglones ya se encuentran incluidos en una liquidación de comisiones activa.");

                    responseDto = await MapSingleToResponseDtoAsync(venta, token);

                    var detallesProducto = venta.DetallesVenta
                        .Where(d => !d.IsDeleted && d.TipoItem == TipoItemVentaEnum.Producto)
                        .ToList();

                    if (detallesProducto.Count > 0)
                    {
                        var productoIds = detallesProducto.Select(d => d.ItemId).Distinct().ToList();
                        var productosDict = await _unitOfWork.Productos.GetByIdsDictionaryAsync(productoIds, tracking: true, token);

                        foreach (var detalle in detallesProducto)
                        {
                            if (productosDict.TryGetValue(detalle.ItemId, out var producto))
                            {
                                producto.StockActual += detalle.Cantidad;
                                producto.UpdatedByUserId = usuarioId;
                                _unitOfWork.Productos.Update(producto);

                                await _unitOfWork.Productos.RegistrarMovimientoAsync(new MovimientoStock
                                {
                                    ProductoId = producto.Id,
                                    TipoMovimiento = TipoMovimientoStockEnum.IngresoCompra,
                                    Cantidad = detalle.Cantidad,
                                    SaldoResultante = producto.StockActual,
                                    Fecha = DateTime.UtcNow,
                                    UsuarioId = usuarioId,
                                    Motivo = Truncar($"Restitución por Anulación de Venta #{venta.Id}: {motivoNormalizado}", 300),
                                    CreatedByUserId = usuarioId
                                }, token);
                            }
                        }
                    }

                    decimal reversoEfectivo = venta.Pagos
                        .Where(p => !p.IsDeleted && p.MedioPago != null && p.MedioPago.EsEfectivoFisico)
                        .Sum(p => p.Importe);

                    decimal reversoDigital = venta.Pagos
                        .Where(p => !p.IsDeleted && (p.MedioPago == null || !p.MedioPago.EsEfectivoFisico))
                        .Sum(p => p.Importe);

                    var caja = venta.SesionCaja;
                    caja.TotalVentasEfectivo = Math.Max(0m, caja.TotalVentasEfectivo - reversoEfectivo);
                    caja.TotalVentasDigital = Math.Max(0m, caja.TotalVentasDigital - reversoDigital);
                    caja.MontoFinalEsperado = caja.MontoInicial
                        + caja.TotalVentasEfectivo
                        + caja.TotalIngresosManuales
                        - caja.TotalEgresosManuales;
                    caja.UpdatedByUserId = usuarioId;
                    _unitOfWork.SesionesCaja.ActualizarSesionCaja(caja);

                    var ahora = DateTime.UtcNow;
                    foreach (var det in venta.DetallesVenta)
                    {
                        det.IsDeleted = true;
                        det.DeletedAt = ahora;
                        det.DeletedByUserId = usuarioId;
                    }

                    foreach (var pago in venta.Pagos)
                    {
                        pago.IsDeleted = true;
                        pago.DeletedAt = ahora;
                        pago.DeletedByUserId = usuarioId;
                    }

                    venta.IsDeleted = true;
                    venta.DeletedAt = ahora;
                    venta.DeletedByUserId = usuarioId;
                    venta.UpdatedAt = ahora;
                    venta.UpdatedByUserId = usuarioId;
                    _unitOfWork.Ventas.Update(venta);

                    await _unitOfWork.SaveChangesAsync(token);
                }, IsolationLevel.ReadCommitted, ct);

                return ApiResponse<VentaResponseDto>.Ok(responseDto!, "Venta anulada y stock restituido correctamente.");
            }
            catch (BusinessRuleException ex)
            {
                return ApiResponse<VentaResponseDto>.Fail(ex.Message);
            }
        }

        private async Task<VentaResponseDto> MapSingleToResponseDtoAsync(Venta venta, CancellationToken ct)
        {
            var list = await MapBatchToResponseDtosAsync(new[] { venta }, ct);
            return list[0];
        }

        private async Task<List<VentaResponseDto>> MapBatchToResponseDtosAsync(
            IReadOnlyList<Venta> ventas,
            CancellationToken ct)
        {
            if (ventas.Count == 0)
                return new List<VentaResponseDto>();

            var todosDetalles = ventas.SelectMany(v => v.DetallesVenta).ToList();

            var productoIds = todosDetalles
                .Where(d => d.TipoItem == TipoItemVentaEnum.Producto)
                .Select(d => d.ItemId)
                .Distinct()
                .ToList();

            var servicioIds = todosDetalles
                .Where(d => d.TipoItem == TipoItemVentaEnum.Servicio)
                .Select(d => d.ItemId)
                .Distinct()
                .ToList();

            var peluqueroIds = todosDetalles
                .Where(d => d.PeluqueroId.HasValue && d.PeluqueroId.Value > 0)
                .Select(d => d.PeluqueroId!.Value)
                .Distinct()
                .ToList();

            var productosDict = productoIds.Count > 0
                ? await _unitOfWork.Productos.GetByIdsDictionaryAsync(productoIds, tracking: false, ct)
                : new Dictionary<int, Producto>();

            var serviciosDict = servicioIds.Count > 0
                ? await _unitOfWork.Servicios.GetByIdsDictionaryAsync(servicioIds, tracking: false, ct)
                : new Dictionary<int, Servicio>();

            var peluquerosDict = peluqueroIds.Count > 0
                ? await _unitOfWork.Peluqueros.GetByIdsDictionaryAsync(peluqueroIds, tracking: false, ct)
                : new Dictionary<int, Peluquero>();

            var resultado = new List<VentaResponseDto>(ventas.Count);

            foreach (var v in ventas)
            {
                var detallesDto = v.DetallesVenta.Select(d =>
                {
                    string conceptoNombre;
                    if (d.TipoItem == TipoItemVentaEnum.Producto)
                    {
                        conceptoNombre = productosDict.TryGetValue(d.ItemId, out var prod)
                            ? prod.Nombre
                            : $"Producto #{d.ItemId}";
                    }
                    else
                    {
                        conceptoNombre = serviciosDict.TryGetValue(d.ItemId, out var serv)
                            ? serv.Nombre
                            : $"Servicio #{d.ItemId}";
                    }

                    string? peluqueroNombre = null;
                    if (d.PeluqueroId.HasValue && peluquerosDict.TryGetValue(d.PeluqueroId.Value, out var pel))
                    {
                        peluqueroNombre = $"{pel.Nombre} {pel.Apellido}".Trim();
                    }

                    return new VentaDetalleItemResponseDto
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
                    };
                }).ToList();

                decimal subtotalCalculado = v.Subtotal > 0m
                    ? v.Subtotal
                    : v.Total + v.DescuentoSenaAplicado;

                resultado.Add(new VentaResponseDto
                {
                    Id = v.Id,
                    SesionCajaId = v.SesionCajaId,
                    ClienteId = v.ClienteId,
                    ClienteNombre = v.Cliente != null
                        ? $"{v.Cliente.Nombre} {v.Cliente.Apellido}".Trim()
                        : "Consumidor Final",
                    TurnoId = v.TurnoId,
                    UsuarioId = v.UsuarioId,
                    Fecha = v.Fecha,
                    Subtotal = subtotalCalculado,
                    DescuentoSenaAplicado = v.DescuentoSenaAplicado,
                    Total = v.Total,
                    IdempotencyKey = v.IdempotencyKey,
                    Detalles = detallesDto,
                    Pagos = v.Pagos.Select(p => new VentaPagoItemResponseDto
                    {
                        Id = p.Id,
                        MedioPagoId = p.MedioPagoId,
                        MedioPagoNombre = p.MedioPago?.Nombre ?? string.Empty,
                        Importe = p.Importe,
                        ReferenciaOperacion = p.ReferenciaOperacion
                    }).ToList()
                });
            }

            return resultado;
        }

        private static string Truncar(string valor, int maxLength)
            => valor.Length <= maxLength ? valor : valor[..maxLength];
    }
}
