using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
    public class LiquidacionService : ILiquidacionService
    {
        private readonly IUnitOfWork _unitOfWork;

        public LiquidacionService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ApiResponse<LiquidacionResponseDto>> LiquidarPeriodoAsync(
            GenerarLiquidacionDto dto,
            int? usuarioLiquidacionId = null,
            CancellationToken ct = default)
        {
            var peluquero = await _unitOfWork.Peluqueros.GetByIdAsync(dto.PeluqueroId, ct);
            if (peluquero == null)
                return ApiResponse<LiquidacionResponseDto>.Falla("El profesional especificado no existe.");

            if (dto.FechaHasta < dto.FechaDesde)
                return ApiResponse<LiquidacionResponseDto>.Falla("La fecha de cierre del período no puede ser anterior a la fecha de inicio.");

            if (dto.Bonificaciones < 0m || dto.Descuentos < 0m)
                return ApiResponse<LiquidacionResponseDto>.Falla("Las bonificaciones y descuentos no pueden ser valores negativos.");

            DateTime fechaHastaInclusiva = dto.FechaHasta.TimeOfDay == TimeSpan.Zero
                ? dto.FechaHasta.Date.AddDays(1).AddTicks(-1)
                : dto.FechaHasta;

            int liquidacionCreadaId = 0;

            try
            {
                await _unitOfWork.ExecuteInTransactionAsync(async token =>
                {
                    var configs = await _unitOfWork.ComisionesConfig.GetByPeluqueroIdWithDetailsAsync(dto.PeluqueroId, token);
                    var configGeneral = configs.FirstOrDefault(c => c.ServicioId == null);

                    // Jerarquía de Comisiones (Sección 4.4):
                    // 1°) Específica (PeluqueroId, ServicioId)
                    // 2°) General (PeluqueroId, ServicioId == null)
                    // 3°) Default 50% servicio / 10% producto
                    decimal pctServicioDefault = configGeneral?.PorcentajeServicio ?? 50.0m;
                    decimal pctProductoDefault = configGeneral?.PorcentajeVentaProducto ?? 10.0m;

                    var itemsPendientes = await _unitOfWork.Liquidaciones.GetDetallesVentaPendientesLiquidacionAsync(
                        dto.PeluqueroId,
                        dto.FechaDesde,
                        fechaHastaInclusiva,
                        token);

                    if (itemsPendientes.Count == 0)
                    {
                        throw new BusinessRuleException(
                            "No existen servicios ni productos pendientes de liquidar para este profesional en el período seleccionado.");
                    }

                    var ahora = DateTime.UtcNow;
                    decimal comisionAcumuladaServicios = 0m;
                    decimal comisionAcumuladaProductos = 0m;

                    var liquidacion = new Liquidacion
                    {
                        PeluqueroId = dto.PeluqueroId,
                        FechaEmision = ahora,
                        FechaDesde = dto.FechaDesde,
                        FechaHasta = fechaHastaInclusiva,
                        FechaVencimiento = ahora.AddDays(7),
                        Bonificaciones = dto.Bonificaciones,
                        Descuentos = dto.Descuentos,
                        Observaciones = string.IsNullOrWhiteSpace(dto.Observaciones)
                            ? null
                            : Truncar(dto.Observaciones.Trim(), 500),
                        Estado = EstadoLiquidacionEnum.Pendiente,
                        UsuarioLiquidacionId = usuarioLiquidacionId,
                        CreatedByUserId = usuarioLiquidacionId,
                        DetallesLiquidacion = new List<DetalleLiquidacion>()
                    };

                    foreach (var item in itemsPendientes)
                    {
                        decimal montoComisionRenglon = 0m;

                        if (item.TipoItem == TipoItemVentaEnum.Servicio)
                        {
                            var configEspecifica = configs.FirstOrDefault(c => c.ServicioId == item.ItemId);
                            decimal pctAplicar = configEspecifica?.PorcentajeServicio ?? pctServicioDefault;
                            montoComisionRenglon = Math.Round(item.Subtotal * (pctAplicar / 100m), 2);
                            comisionAcumuladaServicios += montoComisionRenglon;
                        }
                        else if (item.TipoItem == TipoItemVentaEnum.Producto)
                        {
                            montoComisionRenglon = Math.Round(item.Subtotal * (pctProductoDefault / 100m), 2);
                            comisionAcumuladaProductos += montoComisionRenglon;
                        }

                        liquidacion.DetallesLiquidacion.Add(new DetalleLiquidacion
                        {
                            DetalleVentaId = item.Id,
                            TurnoId = item.Venta?.TurnoId,
                            MontoComisionCalculado = montoComisionRenglon,
                            CreatedByUserId = usuarioLiquidacionId
                        });
                    }

                    decimal totalPagar = comisionAcumuladaServicios + comisionAcumuladaProductos + dto.Bonificaciones - dto.Descuentos;
                    if (totalPagar < 0m)
                        throw new BusinessRuleException("El total neto a pagar de la liquidación no puede ser negativo.");

                    liquidacion.TotalServicios = comisionAcumuladaServicios;
                    liquidacion.TotalProductos = comisionAcumuladaProductos;
                    liquidacion.TotalPagar = totalPagar;

                    await _unitOfWork.Liquidaciones.AddAsync(liquidacion, token);
                    await _unitOfWork.SaveChangesAsync(token);

                    liquidacionCreadaId = liquidacion.Id;
                }, IsolationLevel.ReadCommitted, ct);

                var creadaRes = await ObtenerPorIdAsync(liquidacionCreadaId, ct);
                return ApiResponse<LiquidacionResponseDto>.Exito(
                    creadaRes.Data!,
                    "Liquidación calculada y renglones bloqueados exitosamente.");
            }
            catch (BusinessRuleException ex)
            {
                return ApiResponse<LiquidacionResponseDto>.Falla(ex.Message);
            }
        }

        public async Task<ApiResponse<bool>> RegistrarPagoLiquidacionAsync(
            int liquidacionId,
            int? sesionCajaId = null,
            int? usuarioId = null,
            CancellationToken ct = default)
        {
            try
            {
                await _unitOfWork.ExecuteInTransactionAsync(async token =>
                {
                    var liq = await _unitOfWork.Liquidaciones.GetByIdWithDetallesAsync(liquidacionId, tracking: true, token)
                        ?? throw new BusinessRuleException("La liquidación indicada no existe.");

                    if (liq.Estado == EstadoLiquidacionEnum.Pagado)
                        throw new BusinessRuleException("Esta liquidación ya fue abonada anteriormente.");

                    if (liq.Estado == EstadoLiquidacionEnum.Anulada)
                        throw new BusinessRuleException("No se puede registrar el pago de una liquidación que se encuentra anulada.");

                    var ahora = DateTime.UtcNow;
                    liq.Estado = EstadoLiquidacionEnum.Pagado;
                    liq.FechaPago = ahora;
                    liq.UpdatedAt = ahora;
                    liq.UpdatedByUserId = usuarioId;

                    if (sesionCajaId.HasValue && sesionCajaId.Value > 0)
                    {
                        var cajaAbierta = await _unitOfWork.SesionesCaja.GetCajaAbiertaActualAsync(token)
                            ?? throw new BusinessRuleException("No hay una sesión de caja abierta para imputar el egreso de la liquidación.");

                        if (cajaAbierta.Id != sesionCajaId.Value)
                            throw new BusinessRuleException($"La sesión de caja indicada (#{sesionCajaId.Value}) no coincide con la sesión de caja abierta actual (#{cajaAbierta.Id}).");

                        string nombreProfesional = liq.Peluquero != null
                            ? $"{liq.Peluquero.Nombre} {liq.Peluquero.Apellido}".Trim()
                            : $"ID #{liq.PeluqueroId}";

                        await _unitOfWork.SesionesCaja.RegistrarMovimientoCajaAsync(new MovimientoCaja
                        {
                            SesionCajaId = cajaAbierta.Id,
                            Tipo = "EgresoLiquidacion",
                            Monto = liq.TotalPagar,
                            Concepto = Truncar($"Pago de Liquidación #{liq.Id} - Profesional: {nombreProfesional}", 250),
                            Fecha = ahora,
                            CreatedByUserId = usuarioId
                        }, token);

                        cajaAbierta.TotalEgresosManuales += liq.TotalPagar;
                        cajaAbierta.MontoFinalEsperado = cajaAbierta.MontoInicial
                            + cajaAbierta.TotalVentasEfectivo
                            + cajaAbierta.TotalIngresosManuales
                            - cajaAbierta.TotalEgresosManuales;
                        cajaAbierta.UpdatedByUserId = usuarioId;
                        _unitOfWork.SesionesCaja.ActualizarSesionCaja(cajaAbierta);
                    }

                    _unitOfWork.Liquidaciones.Update(liq);
                    await _unitOfWork.SaveChangesAsync(token);
                }, IsolationLevel.ReadCommitted, ct);

                return ApiResponse<bool>.Exito(true, "Pago de liquidación asentado correctamente.");
            }
            catch (BusinessRuleException ex)
            {
                return ApiResponse<bool>.Falla(ex.Message);
            }
        }

        public async Task<ApiResponse<LiquidacionResponseDto>> AnularLiquidacionAsync(
            int liquidacionId,
            string? motivo = null,
            int? usuarioId = null,
            CancellationToken ct = default)
        {
            var previa = await _unitOfWork.Liquidaciones.GetByIdWithDetallesAsync(liquidacionId, tracking: false, ct);
            if (previa == null)
                return ApiResponse<LiquidacionResponseDto>.Falla("La liquidación solicitada no existe.");

            if (previa.Estado != EstadoLiquidacionEnum.Pendiente)
            {
                return ApiResponse<LiquidacionResponseDto>.Falla(
                    $"Solo se permite anular liquidaciones en estado Pendiente. Estado actual: {previa.Estado}.");
            }

            LiquidacionResponseDto? resultadoDto = null;

            try
            {
                await _unitOfWork.ExecuteInTransactionAsync(async token =>
                {
                    var liq = await _unitOfWork.Liquidaciones.GetByIdWithDetallesAsync(liquidacionId, tracking: true, token)
                        ?? throw new BusinessRuleException("La liquidación solicitada no existe.");

                    if (liq.Estado != EstadoLiquidacionEnum.Pendiente)
                    {
                        throw new BusinessRuleException(
                            $"Solo se permite anular liquidaciones en estado Pendiente. Estado actual: {liq.Estado}.");
                    }

                    var ahora = DateTime.UtcNow;
                    liq.Estado = EstadoLiquidacionEnum.Anulada;
                    liq.UpdatedAt = ahora;
                    liq.UpdatedByUserId = usuarioId;

                    if (!string.IsNullOrWhiteSpace(motivo))
                    {
                        string prefijo = string.IsNullOrWhiteSpace(liq.Observaciones)
                            ? string.Empty
                            : $"{liq.Observaciones} | ";
                        liq.Observaciones = Truncar($"{prefijo}Anulada: {motivo.Trim()}", 500);
                    }

                    foreach (var det in liq.DetallesLiquidacion)
                    {
                        det.DeletedByUserId = usuarioId;
                        det.UpdatedByUserId = usuarioId;
                    }

                    _unitOfWork.Liquidaciones.AnularDetallesLiquidacion(liq.DetallesLiquidacion);
                    _unitOfWork.Liquidaciones.Update(liq);

                    await _unitOfWork.SaveChangesAsync(token);

                    resultadoDto = new LiquidacionResponseDto
                    {
                        Id = liq.Id,
                        PeluqueroId = liq.PeluqueroId,
                        PeluqueroNombre = liq.Peluquero != null
                            ? $"{liq.Peluquero.Nombre} {liq.Peluquero.Apellido}".Trim()
                            : string.Empty,
                        FechaEmision = liq.FechaEmision,
                        FechaDesde = liq.FechaDesde,
                        FechaHasta = liq.FechaHasta,
                        FechaVencimiento = liq.FechaVencimiento,
                        FechaPago = liq.FechaPago,
                        TotalServicios = liq.TotalServicios,
                        TotalProductos = liq.TotalProductos,
                        Bonificaciones = liq.Bonificaciones,
                        Descuentos = liq.Descuentos,
                        TotalPagar = liq.TotalPagar,
                        CantidadItemsLiquidados = 0,
                        Estado = liq.Estado.ToString(),
                        Observaciones = liq.Observaciones,
                        Detalles = new List<DetalleLiquidacionItemDto>()
                    };
                }, IsolationLevel.ReadCommitted, ct);

                return ApiResponse<LiquidacionResponseDto>.Exito(
                    resultadoDto!,
                    "Liquidación anulada y renglones liberados correctamente.");
            }
            catch (BusinessRuleException ex)
            {
                return ApiResponse<LiquidacionResponseDto>.Falla(ex.Message);
            }
        }

        public async Task<ApiResponse<IReadOnlyList<LiquidacionResponseDto>>> ObtenerPorPeluqueroAsync(
            int peluqueroId,
            CancellationToken ct = default)
        {
            var lista = await _unitOfWork.Liquidaciones.GetByPeluqueroIdAsync(peluqueroId, ct);
            var dtos = lista.Select(MapResumenToDto).ToList();
            return ApiResponse<IReadOnlyList<LiquidacionResponseDto>>.Exito(dtos, "Liquidaciones del profesional obtenidas.");
        }

        public async Task<ApiResponse<PagedResult<LiquidacionResponseDto>>> ObtenerTodasAsync(
            int? peluqueroId = null,
            DateTime? desde = null,
            DateTime? hasta = null,
            CancellationToken ct = default)
        {
            var filter = new PaginationFilterDto
            {
                PageNumber = 1,
                PageSize = 15,
                FechaDesde = desde,
                FechaHasta = hasta
            };

            return await ObtenerTodasAsync(filter, peluqueroId, null, ct);
        }

        public async Task<ApiResponse<PagedResult<LiquidacionResponseDto>>> ObtenerTodasAsync(
            PaginationFilterDto filter,
            int? peluqueroId = null,
            EstadoLiquidacionEnum? estado = null,
            CancellationToken ct = default)
        {
            filter ??= new PaginationFilterDto();

            var (items, totalRecords) = await _unitOfWork.Liquidaciones.GetPagedAsync(filter, peluqueroId, estado, ct);
            var data = items.Select(MapResumenToDto).ToList();

            var paged = new PagedResult<LiquidacionResponseDto>
            {
                Data = data,
                TotalRecords = totalRecords,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };

            return ApiResponse<PagedResult<LiquidacionResponseDto>>.Exito(paged, "Historial de liquidaciones obtenido.");
        }

        public async Task<ApiResponse<LiquidacionResponseDto>> ObtenerPorIdAsync(int id, CancellationToken ct = default)
        {
            var l = await _unitOfWork.Liquidaciones.GetByIdWithDetallesAsync(id, tracking: false, ct);
            if (l == null)
                return ApiResponse<LiquidacionResponseDto>.Falla("La liquidación solicitada no existe.");

            var detallesActivos = l.DetallesLiquidacion
                .Where(dl => !dl.IsDeleted)
                .ToList();

            var detallesVentaIds = detallesActivos
                .Where(dl => dl.DetalleVentaId.HasValue)
                .Select(dl => dl.DetalleVentaId!.Value)
                .Distinct()
                .ToList();

            var detallesVentaDict = detallesVentaIds.Count > 0
                ? await _unitOfWork.Liquidaciones.GetDetallesVentaByIdsDictionaryAsync(detallesVentaIds, ct)
                : new Dictionary<int, DetalleVenta>();

            var productoIds = detallesVentaDict.Values
                .Where(dv => dv.TipoItem == TipoItemVentaEnum.Producto)
                .Select(dv => dv.ItemId)
                .Distinct()
                .ToList();

            var servicioIds = detallesVentaDict.Values
                .Where(dv => dv.TipoItem == TipoItemVentaEnum.Servicio)
                .Select(dv => dv.ItemId)
                .Distinct()
                .ToList();

            var productosDict = productoIds.Count > 0
                ? await _unitOfWork.Productos.GetByIdsDictionaryAsync(productoIds, tracking: false, ct)
                : new Dictionary<int, Producto>();

            var serviciosDict = servicioIds.Count > 0
                ? await _unitOfWork.Servicios.GetByIdsDictionaryAsync(servicioIds, tracking: false, ct)
                : new Dictionary<int, Servicio>();

            var dto = MapResumenToDto(l);
            dto.Detalles = detallesActivos.Select(dl =>
            {
                DetalleVenta? dv = null;
                if (dl.DetalleVentaId.HasValue)
                    detallesVentaDict.TryGetValue(dl.DetalleVentaId.Value, out dv);

                string conceptoNombre = string.Empty;
                if (dv != null)
                {
                    if (dv.TipoItem == TipoItemVentaEnum.Producto)
                    {
                        conceptoNombre = productosDict.TryGetValue(dv.ItemId, out var prod)
                            ? prod.Nombre
                            : $"Producto #{dv.ItemId}";
                    }
                    else
                    {
                        conceptoNombre = serviciosDict.TryGetValue(dv.ItemId, out var serv)
                            ? serv.Nombre
                            : $"Servicio #{dv.ItemId}";
                    }
                }

                return new DetalleLiquidacionItemDto
                {
                    Id = dl.Id,
                    DetalleVentaId = dl.DetalleVentaId ?? 0,
                    TurnoId = dl.TurnoId,
                    TipoItem = dv != null ? dv.TipoItem.ToString() : string.Empty,
                    ItemId = dv?.ItemId ?? 0,
                    ConceptoNombre = conceptoNombre,
                    SubtotalVenta = dv?.Subtotal ?? 0m,
                    MontoComisionCalculado = dl.MontoComisionCalculado,
                    FechaVenta = dv?.Venta?.Fecha
                };
            }).ToList();

            return ApiResponse<LiquidacionResponseDto>.Exito(dto);
        }

        private static LiquidacionResponseDto MapResumenToDto(Liquidacion l)
        {
            return new LiquidacionResponseDto
            {
                Id = l.Id,
                PeluqueroId = l.PeluqueroId,
                PeluqueroNombre = l.Peluquero != null
                    ? $"{l.Peluquero.Nombre} {l.Peluquero.Apellido}".Trim()
                    : string.Empty,
                FechaEmision = l.FechaEmision,
                FechaDesde = l.FechaDesde,
                FechaHasta = l.FechaHasta,
                FechaVencimiento = l.FechaVencimiento,
                FechaPago = l.FechaPago,
                TotalServicios = l.TotalServicios,
                TotalProductos = l.TotalProductos,
                Bonificaciones = l.Bonificaciones,
                Descuentos = l.Descuentos,
                TotalPagar = l.TotalPagar,
                CantidadItemsLiquidados = l.DetallesLiquidacion.Count(dl => !dl.IsDeleted),
                Estado = l.Estado.ToString(),
                Observaciones = l.Observaciones
            };
        }

        private static string Truncar(string valor, int maxLength)
            => valor.Length <= maxLength ? valor : valor[..maxLength];
    }
}
