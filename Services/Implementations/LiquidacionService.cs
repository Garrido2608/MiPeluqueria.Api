using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MiPeluqueria.Api.Data.Context;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.Models.Ventas;
using MiPeluqueria.Api.Repositories.Interfaces;

namespace MiPeluqueria.Api.Services.Implementations
{
    public class GenerarLiquidacionDto
    {
        public int PeluqueroId { get; set; }
        public DateTime FechaDesde { get; set; }
        public DateTime FechaHasta { get; set; }
    }

    public class LiquidacionResponseDto
    {
        public int Id { get; set; }
        public int PeluqueroId { get; set; }
        public string PeluqueroNombre { get; set; } = string.Empty;
        public DateTime FechaDesde { get; set; }
        public DateTime FechaHasta { get; set; }
        public DateTime? FechaVencimiento { get; set; }
        public decimal TotalServicios { get; set; }
        public decimal TotalProductos { get; set; }
        public decimal TotalPagar { get; set; }
        public int CantidadItemsLiquidados { get; set; }
        public string Estado { get; set; } = string.Empty;
    }

    public interface ILiquidacionService
    {
        Task<ApiResponse<LiquidacionResponseDto>> LiquidarPeriodoAsync(GenerarLiquidacionDto dto);
        Task<ApiResponse<bool>> RegistrarPagoLiquidacionAsync(int liquidacionId);
        Task<ApiResponse<List<LiquidacionResponseDto>>> ObtenerPorPeluqueroAsync(int peluqueroId);
    }

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

            // 1. Obtener configuraciones de comisión del profesional (general y específicas por servicio)
            var configs = await _context.ComisionesConfigs
                .AsNoTracking()
                .Where(c => c.PeluqueroId == dto.PeluqueroId)
                .ToListAsync();

            var configGeneral = configs.FirstOrDefault(c => c.ServicioId == null);
            decimal pctServicioDefault = configGeneral?.PorcentajeServicio ?? 50.0m;
            decimal pctProductoDefault = configGeneral?.PorcentajeVentaProducto ?? 10.0m;

            // 2. Buscar todos los renglones de venta ejecutados por este profesional en el período
            // que NO hayan sido liquidados previamente en DetallesLiquidacion
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
            liquidacion.TotalPagar = comisionAcumuladaServicios + comisionAcumuladaProductos;

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

        public async Task<ApiResponse<bool>> RegistrarPagoLiquidacionAsync(int liquidacionId)
        {
            var liq = await _context.Liquidaciones.FindAsync(liquidacionId);
            if (liq == null)
                return ApiResponse<bool>.Falla("La liquidación indicada no existe.");

            if (liq.Estado == EstadoLiquidacionEnum.Pagado)
                return ApiResponse<bool>.Falla("Esta liquidación ya fue abonada anteriormente.");

            liq.Estado = EstadoLiquidacionEnum.Pagado;
            liq.FechaPago = DateTime.UtcNow;

            _context.Liquidaciones.Update(liq);
            await _unitOfWork.SaveChangesAsync();

            return ApiResponse<bool>.Exito(true, "Pago de liquidación asentado correctamente.");
        }

        public async Task<ApiResponse<List<LiquidacionResponseDto>>> ObtenerPorPeluqueroAsync(int peluqueroId)
        {
            var lista = await _context.Liquidaciones
                .AsNoTracking()
                .Include(l => l.Peluquero)
                .Include(l => l.DetallesLiquidacion)
                .Where(l => l.PeluqueroId == peluqueroId)
                .OrderByDescending(l => l.FechaHasta)
                .Select(l => new LiquidacionResponseDto
                {
                    Id = l.Id,
                    PeluqueroId = l.PeluqueroId,
                    PeluqueroNombre = l.Peluquero != null ? (l.Peluquero.Nombre + " " + l.Peluquero.Apellido) : string.Empty,
                    FechaDesde = l.FechaDesde,
                    FechaHasta = l.FechaHasta,
                    FechaVencimiento = l.FechaVencimiento,
                    TotalServicios = l.TotalServicios,
                    TotalProductos = l.TotalProductos,
                    TotalPagar = l.TotalPagar,
                    CantidadItemsLiquidados = l.DetallesLiquidacion.Count,
                    Estado = l.Estado.ToString()
                })
                .ToListAsync();

            return ApiResponse<List<LiquidacionResponseDto>>.Exito(lista, "Historial de liquidaciones obtenido.");
        }
    }
}