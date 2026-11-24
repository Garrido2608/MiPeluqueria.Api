using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        private readonly IUnitOfWork _uow;

        public RecetaServicioService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<ApiResponse<IReadOnlyList<RecetaItemResponseDto>>> ObtenerRecetaPorServicioAsync(
            int servicioId,
            CancellationToken ct = default)
        {
            var items = await _uow.Recetas.GetByServicioIdWithDetailsAsync(servicioId, false, ct);
            var dtos = items.Select(r => new RecetaItemResponseDto
            {
                Id = r.Id,
                ServicioId = r.ServicioId,
                ServicioNombre = r.Servicio?.Nombre ?? string.Empty,
                ProductoId = r.ProductoId,
                ProductoNombre = r.Producto?.Nombre ?? string.Empty,
                StockActualProducto = r.Producto?.StockActual ?? 0,
                CantidadUnidadesDescontar = r.CantidadUnidadesDescontar,
                CantidadMedidaEstandar = r.CantidadMedidaEstandar,
                UnidadMedida = r.UnidadMedida,
                CostoEstimadoInsumo = (r.Producto?.PrecioCosto ?? 0m) * r.CantidadUnidadesDescontar
            }).ToList();

            return ApiResponse<IReadOnlyList<RecetaItemResponseDto>>.Exito(dtos);
        }

        public async Task<ApiResponse<bool>> GuardarRecetaServicioAsync(
            ConfigurarRecetaServicioDto dto,
            int usuarioId,
            CancellationToken ct = default)
        {
            var servicio = await _uow.Servicios.GetByIdAsync(dto.ServicioId, ct);
            if (servicio == null)
                return ApiResponse<bool>.Falla("El servicio especificado no existe.");

            var productosIds = dto.Insumos.Select(i => i.ProductoId).Distinct().ToList();
            if (productosIds.Count > 0)
            {
                var productosDict = await _uow.Productos.GetByIdsDictionaryAsync(productosIds, false, ct);
                foreach (var prodId in productosIds)
                {
                    if (!productosDict.ContainsKey(prodId))
                        return ApiResponse<bool>.Falla($"El producto insumo con ID {prodId} no existe.");
                }
            }

            await _uow.ExecuteInTransactionAsync(async token =>
            {
                var existentes = await _uow.Recetas.GetByServicioIdWithDetailsAsync(dto.ServicioId, true, token);
                foreach (var ex in existentes)
                {
                    _uow.Recetas.Delete(ex);
                }

                if (dto.Insumos.Count > 0)
                {
                    var nuevos = dto.Insumos.Select(item => new ServicioInsumoReceta
                    {
                        ServicioId = dto.ServicioId,
                        ProductoId = item.ProductoId,
                        CantidadUnidadesDescontar = item.CantidadUnidadesDescontar,
                        CantidadMedidaEstandar = item.CantidadMedidaEstandar,
                        UnidadMedida = string.IsNullOrWhiteSpace(item.UnidadMedida) ? "Unid" : item.UnidadMedida.Trim(),
                        DescuentoAutomaticoAlCompletarTurno = item.DescuentoAutomaticoAlCompletarTurno,
                        CreatedByUserId = usuarioId
                    }).ToList();

                    await _uow.Recetas.AddRangeAsync(nuevos, token);
                }

                await _uow.SaveChangesAsync(token);
            }, ct: ct);

            return ApiResponse<bool>.Exito(true, "Receta técnica de insumos actualizada correctamente.");
        }

        public async Task<ApiResponse<int>> AplicarConsumoGabinetePorTurnoAsync(
            int turnoId,
            int usuarioId,
            CancellationToken ct = default)
        {
            // Regla de Idempotencia Estricta (05_BUSINESS_RULES.md Sección 3.2):
            bool yaConsumido = await _uow.Recetas.ExisteConsumoGabinetePorTurnoAsync(turnoId, ct);
            if (yaConsumido)
            {
                return ApiResponse<int>.Ok(
                    0,
                    $"El consumo de gabinete para el Turno #{turnoId} ya fue registrado previamente (operación idempotente).");
            }

            var turno = await _uow.Turnos.GetByIdConDetallesAsync(turnoId, false, ct);
            if (turno == null)
                return ApiResponse<int>.Falla("El turno indicado no existe.");

            var serviciosIds = turno.TurnoServicios.Select(ts => ts.ServicioId).Distinct().ToList();
            if (serviciosIds.Count == 0)
                return ApiResponse<int>.Exito(0, "El turno no posee servicios asociados.");

            var recetas = await _uow.Recetas.GetByServiciosIdsAsync(serviciosIds, soloDescuentoAutomatico: true, ct);
            if (recetas.Count == 0)
                return ApiResponse<int>.Exito(0, "Los servicios del turno no requieren descuento automático de insumos.");

            var productosIds = recetas.Select(r => r.ProductoId).Distinct().ToList();
            var productosDict = await _uow.Productos.GetByIdsDictionaryAsync(productosIds, tracking: true, ct);

            var warnings = new List<string>();
            int movimientosGenerados = 0;

            await _uow.ExecuteInTransactionAsync(async token =>
            {
                foreach (var receta in recetas)
                {
                    if (receta.CantidadUnidadesDescontar <= 0)
                        continue;

                    if (!productosDict.TryGetValue(receta.ProductoId, out var producto))
                        continue;

                    if (producto.StockActual < receta.CantidadUnidadesDescontar)
                    {
                        warnings.Add(
                            $"Stock insuficiente en gabinete para '{producto.Nombre}' (Turno #{turno.Id}): se requerían {receta.CantidadUnidadesDescontar} unid. y había {producto.StockActual} unid. Stock ajustado a 0.");
                    }

                    producto.StockActual = Math.Max(0, producto.StockActual - receta.CantidadUnidadesDescontar);
                    _uow.Productos.Update(producto);

                    await _uow.Productos.RegistrarMovimientoAsync(new MovimientoStock
                    {
                        ProductoId = producto.Id,
                        TipoMovimiento = TipoMovimientoStockEnum.ConsumoInternoSalon,
                        Cantidad = receta.CantidadUnidadesDescontar,
                        SaldoResultante = producto.StockActual,
                        Fecha = DateTime.UtcNow,
                        UsuarioId = usuarioId,
                        Motivo = $"Consumo técnico automático por Turno #{turno.Id} ({receta.CantidadMedidaEstandar} {receta.UnidadMedida})"
                    }, token);

                    movimientosGenerados++;
                }

                await _uow.SaveChangesAsync(token);
            }, ct: ct);

            return ApiResponse<int>.Ok(
                movimientosGenerados,
                $"Se descontaron {movimientosGenerados} insumos de gabinete.",
                warnings.Count > 0 ? warnings : null);
        }
    }
}
