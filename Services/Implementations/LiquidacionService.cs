using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MiPeluqueria.Api.Data.Context;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Ventas;
using MiPeluqueria.Api.Models.Ventas;
using MiPeluqueria.Api.Repositories.Interfaces;
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Services.Implementations
{
    public class LiquidacionService : ILiquidacionService
    {
        private readonly ApplicationDbContext _context;
        private readonly IUnitOfWork _unitOfWork;

        public LiquidacionService(ApplicationDbContext context, IUnitOfWork unitOfWork)
        {
            _context = context;
            _unitOfWork = unitOfWork;
        }

        public async Task<ApiResponse<LiquidacionResponseDto>> LiquidarPeriodoAsync(GenerarLiquidacionDto dto)
        {
            var peluquero = await _context.Peluqueros
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == dto.PeluqueroId);

            if (peluquero == null)
                return ApiResponse<LiquidacionResponseDto>.Falla("El profesional especificado no existe.");

            if (dto.FechaHasta < dto.FechaDesde)
                return ApiResponse<LiquidacionResponseDto>.Falla("La fecha de cierre del período no puede ser anterior a la fecha de inicio.");

            var configs = await _context.ComisionesConfigs
                .AsNoTracking()
                .Where(c => c.PeluqueroId == dto.PeluqueroId)
                .ToListAsync();

            var configGeneral = configs.FirstOrDefault(c => c.ServicioId == null);
            decimal pctServicioDefault = configGeneral?.PorcentajeServicio ?? 50.0m;
            decimal pctProductoDefault = configGeneral?.PorcentajeVentaProducto ?? 10.0m;

            var itemsPendientes = await _context.DetallesVenta
                .Include(d => d.Venta)
                .Where(d => d.PeluqueroId == dto.PeluqueroId
                            && d.Venta != null
                            && d.Venta.Fecha >= dto.FechaDesde
                            && d.Venta.Fecha <= dto.FechaHasta
                            && !_context.DetallesLiquidacion.Any(dl => dl.DetalleVentaId == d.Id))
                .ToListAsync();

            if (!itemsPendientes.Any())
            {
                return ApiResponse<LiquidacionResponseDto>.Falla(
                    "No existen servicios ni productos pendientes de liquidar para este profesional en el período seleccionado.");
            }

            decimal comisionAcumuladaServicios = 0;
            decimal comisionAcumuladaProductos = 0;

            var liquidacion = new Liquidacion
            {
                PeluqueroId = dto.PeluqueroId,
                FechaDesde = dto.FechaDesde,
                FechaHasta = dto.FechaHasta,
                FechaVencimiento = DateTime.UtcNow.AddDays(7),
                Estado = EstadoLiquidacionEnum.Pendiente
            };

            foreach (var item in itemsPendientes)
            {
                decimal montoComisionRenglon = 0;

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
                    MontoComisionCalculado = montoComisionRenglon
                });
            }

            liquidacion.TotalServicios = comisionAcumuladaServicios;
            liquidacion.TotalProductos = comisionAcumuladaProductos;
            liquidacion.TotalPagar = comisionAcumuladaServicios + comisionAcumuladaProductos + dto.Bonificaciones - dto.Descuentos;

            await _context.Liquidaciones.AddAsync(liquidacion);
            await _unitOfWork.SaveChangesAsync();

            return ApiResponse<LiquidacionResponseDto>.Exito(new LiquidacionResponseDto
            {
                Id = liquidacion.Id,
                PeluqueroId = liquidacion.PeluqueroId,
                PeluqueroNombre = $"{peluquero.Nombre} {peluquero.Apellido}".Trim(),
                FechaDesde = liquidacion.FechaDesde,
                FechaHasta = liquidacion.FechaHasta,
                FechaVencimiento = liquidacion.FechaVencimiento,
                TotalServicios = liquidacion.TotalServicios,
                TotalProductos = liquidacion.TotalProductos,
                TotalPagar = liquidacion.TotalPagar,
                CantidadItemsLiquidados = itemsPendientes.Count,
                Estado = liquidacion.Estado.ToString()
            }, "Liquidación calculada y renglones bloqueados exitosamente.");
        }

        public async Task<ApiResponse<bool>> RegistrarPagoLiquidacionAsync(int liquidacionId, int? sesionCajaId = null)
        {
            var liq = await _context.Liquidaciones
                .Include(l => l.Peluquero)
                .FirstOrDefaultAsync(l => l.Id == liquidacionId);

            if (liq == null)
                return ApiResponse<bool>.Falla("La liquidación indicada no existe.");

            if (liq.Estado == EstadoLiquidacionEnum.Pagado)
                return ApiResponse<bool>.Falla("Esta liquidación ya fue abonada anteriormente.");

            liq.Estado = EstadoLiquidacionEnum.Pagado;
            liq.FechaPago = DateTime.UtcNow;

            if (sesionCajaId.HasValue && sesionCajaId.Value > 0)
            {
                var sesion = await _context.SesionesCaja.FirstOrDefaultAsync(s => s.Id == sesionCajaId.Value && s.EstadoAbierta);
                if (sesion != null)
                {
                    await _context.MovimientosCaja.AddAsync(new MovimientoCaja
                    {
                        SesionCajaId = sesion.Id,
                        Tipo = "EgresoLiquidacion",
                        Monto = liq.TotalPagar,
                        Concepto = $"Pago de Liquidación #{liq.Id} - Profesional: {liq.Peluquero?.Nombre} {liq.Peluquero?.Apellido}".Trim(),
                        Fecha = DateTime.UtcNow
                    });
                }
            }

            _context.Liquidaciones.Update(liq);
            await _unitOfWork.SaveChangesAsync();

            return ApiResponse<bool>.Exito(true, "Pago de liquidación asentado correctamente.");
        }

        public async Task<ApiResponse<List<LiquidacionResponseDto>>> ObtenerPorPeluqueroAsync(int peluqueroId)
        {
            return await ObtenerTodasAsync(peluqueroId, null, null);
        }

        public async Task<ApiResponse<List<LiquidacionResponseDto>>> ObtenerTodasAsync(int? peluqueroId = null, DateTime? desde = null, DateTime? hasta = null)
        {
            var query = _context.Liquidaciones
                .AsNoTracking()
                .Include(l => l.Peluquero)
                .Include(l => l.DetallesLiquidacion)
                .AsQueryable();

            if (peluqueroId.HasValue)
                query = query.Where(l => l.PeluqueroId == peluqueroId.Value);

            if (desde.HasValue)
                query = query.Where(l => l.FechaDesde >= desde.Value);

            if (hasta.HasValue)
                query = query.Where(l => l.FechaHasta <= hasta.Value);

            var lista = await query
                .OrderByDescending(l => l.FechaHasta)
                .Select(l => new LiquidacionResponseDto
                {
                    Id = l.Id,
                    PeluqueroId = l.PeluqueroId,
                    PeluqueroNombre = l.Peluquero != null ? (l.Peluquero.Nombre + " " + l.Peluquero.Apellido) : string.Empty,
                    FechaDesde = l.FechaDesde,
                    FechaHasta = l.FechaHasta,
                    FechaVencimiento = l.FechaVencimiento,
                    FechaPago = l.FechaPago,
                    TotalServicios = l.TotalServicios,
                    TotalProductos = l.TotalProductos,
                    TotalPagar = l.TotalPagar,
                    CantidadItemsLiquidados = l.DetallesLiquidacion.Count,
                    Estado = l.Estado.ToString()
                })
                .ToListAsync();

            return ApiResponse<List<LiquidacionResponseDto>>.Exito(lista, "Historial de liquidaciones obtenido.");
        }

        public async Task<ApiResponse<LiquidacionResponseDto>> ObtenerPorIdAsync(int id)
        {
            var l = await _context.Liquidaciones
                .AsNoTracking()
                .Include(liq => liq.Peluquero)
                .Include(liq => liq.DetallesLiquidacion)
                .FirstOrDefaultAsync(liq => liq.Id == id);

            if (l == null)
                return ApiResponse<LiquidacionResponseDto>.Falla("La liquidación solicitada no existe.");

            var detallesVentaIds = l.DetallesLiquidacion
                .Where(dl => dl.DetalleVentaId != null)
                .Select(dl => dl.DetalleVentaId!.Value)
                .ToList();

            var detallesVentaDict = await _context.DetallesVenta
                .Include(dv => dv.Venta)
                .AsNoTracking()
                .Where(dv => detallesVentaIds.Contains(dv.Id))
                .ToDictionaryAsync(dv => dv.Id);

            var dto = new LiquidacionResponseDto
            {
                Id = l.Id,
                PeluqueroId = l.PeluqueroId,
                PeluqueroNombre = l.Peluquero != null ? $"{l.Peluquero.Nombre} {l.Peluquero.Apellido}".Trim() : string.Empty,
                FechaDesde = l.FechaDesde,
                FechaHasta = l.FechaHasta,
                FechaVencimiento = l.FechaVencimiento,
                FechaPago = l.FechaPago,
                TotalServicios = l.TotalServicios,
                TotalProductos = l.TotalProductos,
                TotalPagar = l.TotalPagar,
                CantidadItemsLiquidados = l.DetallesLiquidacion.Count,
                Estado = l.Estado.ToString(),
                Detalles = l.DetallesLiquidacion.Select(dl =>
                {
                    DetalleVenta? dv = null;
                    if (dl.DetalleVentaId.HasValue)
                        detallesVentaDict.TryGetValue(dl.DetalleVentaId.Value, out dv);

                    return new DetalleLiquidacionItemDto
                    {
                        Id = dl.Id,
                        DetalleVentaId = dl.DetalleVentaId ?? 0,
                        TurnoId = dl.TurnoId,
                        TipoItem = dv != null ? dv.TipoItem.ToString() : string.Empty,
                        ItemId = dv?.ItemId ?? 0,
                        SubtotalVenta = dv?.Subtotal ?? 0m,
                        MontoComisionCalculado = dl.MontoComisionCalculado,
                        FechaVenta = dv?.Venta?.Fecha
                    };
                }).ToList()
            };

            return ApiResponse<LiquidacionResponseDto>.Exito(dto);
        }
    }
}