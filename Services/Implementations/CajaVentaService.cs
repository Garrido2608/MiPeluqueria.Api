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
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Services.Implementations
{
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

        public async Task<ApiResponse<bool>> RegistrarEgresoCajaAsync(int sesionCajaId, decimal monto, string concepto, string tipo = "EgresoMenor")
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

        public async Task<ApiResponse<bool>> RegistrarIngresoCajaAsync(int sesionCajaId, decimal monto, string concepto, string tipo = "IngresoManual")
        {
            var caja = await _cajaRepo.GetCajaAbiertaActualAsync();
            if (caja == null || caja.Id != sesionCajaId)
                return ApiResponse<bool>.Falla("No existe una sesión de caja activa coincidente.");

            if (monto <= 0)
                return ApiResponse<bool>.Falla("El monto del ingreso de caja debe ser mayor a cero.");

            await _cajaRepo.RegistrarMovimientoCajaAsync(new MovimientoCaja
            {
                SesionCajaId = sesionCajaId,
                Tipo = string.IsNullOrWhiteSpace(tipo) ? "IngresoManual" : tipo,
                Monto = monto,
                Concepto = concepto,
                Fecha = DateTime.UtcNow
            });

            await _unitOfWork.SaveChangesAsync();
            return ApiResponse<bool>.Exito(true, "Movimiento de ingreso registrado en la caja actual.");
        }

        public async Task<ApiResponse<decimal>> CerrarCajaAsync(CierreCajaDto dto)
        {
            var caja = await _cajaRepo.GetCajaAbiertaActualAsync();
            if (caja == null)
                return ApiResponse<decimal>.Falla("No hay ninguna sesión de caja abierta para cerrar.");

            decimal totalVentasEfectivo = caja.Ventas
                .SelectMany(v => v.Pagos)
                .Where(p => p.MedioPagoId == 1)
                .Sum(p => p.Importe);

            decimal totalIngresosManuales = caja.MovimientosCaja
                .Where(m => m.Tipo != null && m.Tipo.StartsWith("Ingreso", StringComparison.OrdinalIgnoreCase))
                .Sum(m => m.Monto);

            decimal totalEgresos = caja.MovimientosCaja
                .Where(m => m.Tipo == null || !m.Tipo.StartsWith("Ingreso", StringComparison.OrdinalIgnoreCase))
                .Sum(m => m.Monto);

            decimal esperado = caja.MontoInicial + totalVentasEfectivo + totalIngresosManuales - totalEgresos;

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

        public async Task<ApiResponse<object>> ObtenerEstadoCajaActualAsync()
        {
            var caja = await _cajaRepo.GetCajaAbiertaActualAsync();
            if (caja == null)
            {
                return ApiResponse<object>.Exito(new
                {
                    CajaAbierta = false,
                    SesionCajaId = (int?)null
                }, "No hay ninguna sesión de caja abierta en este momento.");
            }

            decimal ventasEfectivo = caja.Ventas?
                .SelectMany(v => v.Pagos)
                .Where(p => p.MedioPagoId == 1)
                .Sum(p => p.Importe) ?? 0m;

            decimal ventasOtrosMedios = caja.Ventas?
                .SelectMany(v => v.Pagos)
                .Where(p => p.MedioPagoId != 1)
                .Sum(p => p.Importe) ?? 0m;

            decimal ingresosManuales = caja.MovimientosCaja?
                .Where(m => m.Tipo != null && m.Tipo.StartsWith("Ingreso", StringComparison.OrdinalIgnoreCase))
                .Sum(m => m.Monto) ?? 0m;

            decimal egresos = caja.MovimientosCaja?
                .Where(m => m.Tipo == null || !m.Tipo.StartsWith("Ingreso", StringComparison.OrdinalIgnoreCase))
                .Sum(m => m.Monto) ?? 0m;

            decimal saldoEfectivoEsperado = caja.MontoInicial + ventasEfectivo + ingresosManuales - egresos;

            return ApiResponse<object>.Exito(new
            {
                CajaAbierta = true,
                SesionCajaId = caja.Id,
                caja.FechaApertura,
                caja.UsuarioAperturaId,
                caja.MontoInicial,
                TotalVentasEfectivo = ventasEfectivo,
                TotalVentasOtrosMedios = ventasOtrosMedios,
                TotalIngresosManuales = ingresosManuales,
                TotalEgresos = egresos,
                SaldoEfectivoEsperado = saldoEfectivoEsperado,
                CantidadVentas = caja.Ventas?.Count ?? 0
            }, "Estado de caja activa obtenido correctamente.");
        }

        public async Task<ApiResponse<IEnumerable<object>>> ObtenerHistorialCajasAsync(DateTime? fechaDesde = null, DateTime? fechaHasta = null)
        {
            var query = _context.SesionesCaja
                .Include(s => s.Ventas)
                .Include(s => s.MovimientosCaja)
                .AsNoTracking()
                .AsQueryable();

            if (fechaDesde.HasValue)
                query = query.Where(s => s.FechaApertura >= fechaDesde.Value);

            if (fechaHasta.HasValue)
                query = query.Where(s => s.FechaApertura <= fechaHasta.Value);

            var sesiones = await query
                .OrderByDescending(s => s.FechaApertura)
                .Select(s => (object)new
                {
                    s.Id,
                    s.FechaApertura,
                    s.FechaCierre,
                    s.UsuarioAperturaId,
                    s.UsuarioCierreId,
                    s.EstadoAbierta,
                    s.MontoInicial,
                    s.MontoFinalEsperado,
                    s.MontoFinalReal,
                    s.Diferencia,
                    CantidadVentas = s.Ventas.Count,
                    TotalFacturado = s.Ventas.Sum(v => v.Total),
                    CantidadMovimientosManuales = s.MovimientosCaja.Count
                })
                .ToListAsync();

            return ApiResponse<IEnumerable<object>>.Exito(sesiones, "Historial de sesiones de caja obtenido.");
        }

        public async Task<ApiResponse<VentaResponseDto>> ObtenerVentaPorIdAsync(int id)
        {
            var venta = await _context.Ventas
                .Include(v => v.Cliente)
                .Include(v => v.DetallesVenta)
                .Include(v => v.Pagos)
                .ThenInclude(p => p.MedioPago)
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == id);

            if (venta == null)
                return ApiResponse<VentaResponseDto>.Falla("La venta solicitada no existe.");

            var dto = await MapVentaToDtoAsync(venta);
            return ApiResponse<VentaResponseDto>.Exito(dto);
        }

        public async Task<ApiResponse<IEnumerable<VentaResponseDto>>> ObtenerVentasPorSesionAsync(int sesionCajaId)
        {
            var ventas = await _context.Ventas
                .Include(v => v.Cliente)
                .Include(v => v.DetallesVenta)
                .Include(v => v.Pagos)
                .ThenInclude(p => p.MedioPago)
                .Where(v => v.SesionCajaId == sesionCajaId)
                .OrderByDescending(v => v.Fecha)
                .AsNoTracking()
                .ToListAsync();

            var lista = new List<VentaResponseDto>();
            foreach (var v in ventas)
            {
                lista.Add(await MapVentaToDtoAsync(v));
            }

            return ApiResponse<IEnumerable<VentaResponseDto>>.Exito(lista);
        }

        public async Task<ApiResponse<bool>> AnularVentaAsync(int ventaId, string motivo, int usuarioId)
        {
            var venta = await _context.Ventas
                .Include(v => v.DetallesVenta)
                .Include(v => v.Pagos)
                .FirstOrDefaultAsync(v => v.Id == ventaId);

            if (venta == null)
                return ApiResponse<bool>.Falla("La venta que intenta anular no existe.");

            var detallesIds = venta.DetallesVenta.Select(d => d.Id).ToList();

            bool tieneItemsLiquidados = await _context.DetallesLiquidacion
                .AnyAsync(dl => dl.DetalleVentaId != null && detallesIds.Contains(dl.DetalleVentaId.Value));

            if (tieneItemsLiquidados)
                return ApiResponse<bool>.Falla("No se puede anular una venta cuyos servicios/productos ya fueron incluidos en una liquidación de comisiones.");

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                foreach (var det in venta.DetallesVenta.Where(d => d.TipoItem == TipoItemVentaEnum.Producto))
                {
                    var prod = await _context.Productos.FindAsync(det.ItemId);
                    if (prod != null)
                    {
                        prod.StockActual += det.Cantidad;
                        _context.Productos.Update(prod);

                        await _context.MovimientosStock.AddAsync(new MovimientoStock
                        {
                            ProductoId = prod.Id,
                            TipoMovimiento = (TipoMovimientoStockEnum)1,
                            Cantidad = det.Cantidad,
                            Fecha = DateTime.UtcNow,
                            UsuarioId = usuarioId,
                            Motivo = $"Reingreso por anulación de Venta #{venta.Id}: {motivo}"
                        });
                    }
                }

                _context.Pagos.RemoveRange(venta.Pagos);
                _context.DetallesVenta.RemoveRange(venta.DetallesVenta);
                _context.Ventas.Remove(venta);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                return ApiResponse<bool>.Exito(true, $"Venta #{ventaId} anulada correctamente y stock restituido.");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return ApiResponse<bool>.Falla($"Error al anular la venta: {ex.Message}");
            }
        }

        private async Task<VentaResponseDto> MapVentaToDtoAsync(Venta v)
        {
            var serviciosDict = await _context.Servicios
                .AsNoTracking()
                .ToDictionaryAsync(s => s.Id, s => s.Nombre);

            var productosDict = await _context.Productos
                .AsNoTracking()
                .ToDictionaryAsync(p => p.Id, p => p.Nombre);

            var peluquerosDict = await _context.Peluqueros
                .AsNoTracking()
                .ToDictionaryAsync(p => p.Id, p => $"{p.Nombre} {p.Apellido}".Trim());

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
                Detalles = v.DetallesVenta.Select(d => new VentaDetalleItemResponseDto
                {
                    Id = d.Id,
                    TipoItem = d.TipoItem.ToString(),
                    ItemId = d.ItemId,
                    ConceptoNombre = d.TipoItem == TipoItemVentaEnum.Servicio
                        ? (serviciosDict.TryGetValue(d.ItemId, out var sNom) ? sNom : $"Servicio #{d.ItemId}")
                        : (productosDict.TryGetValue(d.ItemId, out var pNom) ? pNom : $"Producto #{d.ItemId}"),
                    Cantidad = d.Cantidad,
                    PrecioUnitario = d.PrecioUnitario,
                    Subtotal = d.Subtotal,
                    PeluqueroId = d.PeluqueroId,
                    PeluqueroNombre = d.PeluqueroId.HasValue && peluquerosDict.TryGetValue(d.PeluqueroId.Value, out var pelNom)
                        ? pelNom
                        : null
                }).ToList(),
                Pagos = v.Pagos.Select(p => new VentaPagoItemResponseDto
                {
                    Id = p.Id,
                    MedioPagoId = p.MedioPagoId,
                    MedioPagoNombre = p.MedioPago != null ? p.MedioPago.Nombre : $"Medio #{p.MedioPagoId}",
                    Importe = p.Importe
                }).ToList()
            };
        }
    }
}