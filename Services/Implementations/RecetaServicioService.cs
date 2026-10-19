using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MiPeluqueria.Api.Data.Context;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Turnos;
using MiPeluqueria.Api.Models.Stock;
using MiPeluqueria.Api.Models.Turnos;
using MiPeluqueria.Api.Repositories.Interfaces;
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Services.Implementations
{
    public class RecetaServicioService : IRecetaServicioService
    {
        private readonly ApplicationDbContext _context;
        private readonly IUnitOfWork _uow;

        public RecetaServicioService(ApplicationDbContext context, IUnitOfWork uow)
        {
            _context = context;
            _uow = uow;
        }

        public async Task<ApiResponse<IReadOnlyList<RecetaItemResponseDto>>> ObtenerRecetaPorServicioAsync(int servicioId, CancellationToken ct = default)
        {
            var items = await _context.Set<ServicioInsumoReceta>()
                .Include(r => r.Servicio)
                .Include(r => r.Producto)
                .AsNoTracking()
                .Where(r => r.ServicioId == servicioId)
                .Select(r => new RecetaItemResponseDto
                {
                    Id = r.Id,
                    ServicioId = r.ServicioId,
                    ServicioNombre = r.Servicio != null ? r.Servicio.Nombre : string.Empty,
                    ProductoId = r.ProductoId,
                    ProductoNombre = r.Producto != null ? r.Producto.Nombre : string.Empty,
                    StockActualProducto = r.Producto != null ? r.Producto.StockActual : 0,
                    CantidadUnidadesDescontar = r.CantidadUnidadesDescontar,
                    CantidadMedidaEstandar = r.CantidadMedidaEstandar,
                    UnidadMedida = r.UnidadMedida,
                    CostoEstimadoInsumo = (r.Producto != null ? r.Producto.PrecioCosto : 0m) * r.CantidadUnidadesDescontar
                })
                .ToListAsync(ct);

            return ApiResponse<IReadOnlyList<RecetaItemResponseDto>>.Exito(items);
        }

        public async Task<ApiResponse<bool>> GuardarRecetaServicioAsync(ConfigurarRecetaServicioDto dto, int usuarioId, CancellationToken ct = default)
        {
            var existentes = await _context.Set<ServicioInsumoReceta>()
                .Where(r => r.ServicioId == dto.ServicioId)
                .ToListAsync(ct);

            foreach (var ex in existentes)
            {
                ex.IsDeleted = true;
                ex.DeletedAt = DateTime.UtcNow;
            }

            foreach (var item in dto.Insumos)
            {
                await _context.Set<ServicioInsumoReceta>().AddAsync(new ServicioInsumoReceta
                {
                    ServicioId = dto.ServicioId,
                    ProductoId = item.ProductoId,
                    CantidadUnidadesDescontar = item.CantidadUnidadesDescontar,
                    CantidadMedidaEstandar = item.CantidadMedidaEstandar,
                    UnidadMedida = item.UnidadMedida,
                    DescuentoAutomaticoAlCompletarTurno = item.DescuentoAutomaticoAlCompletarTurno,
                    CreatedByUserId = usuarioId
                }, ct);
            }

            await _uow.SaveChangesAsync(ct);
            return ApiResponse<bool>.Exito(true, "Receta técnica de insumos actualizada correctamente.");
        }

        public async Task<ApiResponse<int>> AplicarConsumoGabinetePorTurnoAsync(int turnoId, int usuarioId, CancellationToken ct = default)
        {
            var turno = await _uow.Turnos.GetByIdConDetallesAsync(turnoId);
            if (turno == null)
                return ApiResponse<int>.Falla("El turno indicado no existe.");

            var serviciosIds = turno.TurnoServicios.Select(ts => ts.ServicioId).Distinct().ToList();
            if (!serviciosIds.Any())
                return ApiResponse<int>.Exito(0, "El turno no posee servicios asociados.");

            var recetas = await _context.Set<ServicioInsumoReceta>()
                .Where(r => serviciosIds.Contains(r.ServicioId) && r.DescuentoAutomaticoAlCompletarTurno)
                .ToListAsync(ct);

            if (!recetas.Any())
                return ApiResponse<int>.Exito(0, "Los servicios del turno no requieren descuento automático de insumos.");

            int movimientosGenerados = 0;
            foreach (var receta in recetas)
            {
                if (receta.CantidadUnidadesDescontar <= 0) continue;

                var producto = await _uow.Productos.GetByIdAsync(receta.ProductoId);
                if (producto != null)
                {
                    producto.StockActual = Math.Max(0, producto.StockActual - receta.CantidadUnidadesDescontar);
                    await _uow.Productos.UpdateAsync(producto);

                    await _uow.Productos.RegistrarMovimientoAsync(new MovimientoStock
                    {
                        ProductoId = producto.Id,
                        TipoMovimiento = TipoMovimientoStockEnum.ConsumoInternoSalon,
                        Cantidad = receta.CantidadUnidadesDescontar,
                        Fecha = DateTime.UtcNow,
                        UsuarioId = usuarioId,
                        Motivo = $"Consumo técnico automático por Turno #{turno.Id} ({receta.CantidadMedidaEstandar} {receta.UnidadMedida})"
                    });
                    movimientosGenerados++;
                }
            }

            await _uow.SaveChangesAsync(ct);
            return ApiResponse<int>.Exito(movimientosGenerados, $"Se descontaron {movimientosGenerados} insumos de gabinete.");
        }
    }
}