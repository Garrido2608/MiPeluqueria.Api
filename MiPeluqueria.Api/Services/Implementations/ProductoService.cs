using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Stock;
using MiPeluqueria.Api.Models.Stock;
using MiPeluqueria.Api.Repositories.Interfaces;
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Services.Implementations
{
    public class ProductoService : IProductoService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ProductoService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ApiResponse<PagedResult<ProductoDto>>> GetAllAsync(
            PaginationFilterDto filter,
            bool soloBajoStock = false,
            int? categoriaId = null,
            int? proveedorId = null,
            CancellationToken ct = default)
        {
            var effectiveFilter = filter ?? new PaginationFilterDto();
            var (items, totalRecords) = await _unitOfWork.Productos.GetPagedAsync(
                effectiveFilter,
                soloBajoStock,
                categoriaId,
                proveedorId,
                ct);

            var dtos = _mapper.Map<List<ProductoDto>>(items);

            var paged = new PagedResult<ProductoDto>
            {
                Data = dtos,
                TotalRecords = totalRecords,
                PageNumber = effectiveFilter.PageNumber,
                PageSize = effectiveFilter.PageSize
            };

            return ApiResponse<PagedResult<ProductoDto>>.Ok(paged);
        }

        public Task<ApiResponse<PagedResult<ProductoDto>>> GetAllAsync(
            bool soloBajoStock = false,
            CancellationToken ct = default)
        {
            return GetAllAsync(new PaginationFilterDto(), soloBajoStock, null, null, ct);
        }

        public async Task<ApiResponse<IReadOnlyList<ProductoDto>>> GetStockCriticoAsync(CancellationToken ct = default)
        {
            var productos = await _unitOfWork.Productos.GetStockCriticoAsync(ct);
            var dtos = _mapper.Map<List<ProductoDto>>(productos);
            return ApiResponse<IReadOnlyList<ProductoDto>>.Ok(dtos);
        }

        public async Task<ApiResponse<ProductoDto>> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var producto = await _unitOfWork.Productos.GetByIdWithDetailsAsync(id, tracking: false, ct);
            if (producto == null)
                return ApiResponse<ProductoDto>.Fail("El producto solicitado no existe.");

            return ApiResponse<ProductoDto>.Ok(_mapper.Map<ProductoDto>(producto));
        }

        public async Task<ApiResponse<ProductoDto>> CreateAsync(
            CrearProductoDto dto,
            int usuarioId = 1,
            CancellationToken ct = default)
        {
            var categoria = await _unitOfWork.Catalogos.GetCategoriaProductoByIdAsync(dto.CategoriaId, ct);
            if (categoria == null)
                return ApiResponse<ProductoDto>.Fail("La categoría de producto especificada no existe.");

            if (dto.ProveedorId.HasValue)
            {
                var proveedor = await _unitOfWork.Proveedores.GetByIdAsync(dto.ProveedorId.Value, ct);
                if (proveedor == null)
                    return ApiResponse<ProductoDto>.Fail("El proveedor especificado no existe.");
            }

            if (!string.IsNullOrWhiteSpace(dto.CodigoBarras) &&
                await _unitOfWork.Productos.ExistsCodigoBarraAsync(dto.CodigoBarras, null, ct))
            {
                return ApiResponse<ProductoDto>.Fail("Ya existe otro producto registrado con ese código de barras.");
            }

            var producto = new Producto
            {
                CategoriaId = dto.CategoriaId,
                ProveedorId = dto.ProveedorId,
                CodigoBarras = string.IsNullOrWhiteSpace(dto.CodigoBarras) ? null : dto.CodigoBarras.Trim(),
                Nombre = dto.Nombre.Trim(),
                PrecioCosto = dto.PrecioCosto,
                PrecioVenta = dto.PrecioVenta,
                StockActual = dto.StockInicial,
                StockMinimoAlerta = dto.StockMinimoAlerta,
                EsParaVenta = dto.EsParaVenta,
                CreatedByUserId = usuarioId
            };

            await _unitOfWork.ExecuteInTransactionAsync(async token =>
            {
                await _unitOfWork.Productos.AddAsync(producto, token);
                await _unitOfWork.SaveChangesAsync(token);

                if (producto.StockActual > 0)
                {
                    await _unitOfWork.Productos.RegistrarMovimientoAsync(new MovimientoStock
                    {
                        ProductoId = producto.Id,
                        TipoMovimiento = TipoMovimientoStockEnum.IngresoCompra,
                        Cantidad = producto.StockActual,
                        SaldoResultante = producto.StockActual,
                        Fecha = DateTime.UtcNow,
                        UsuarioId = usuarioId,
                        Motivo = "Inventario inicial de alta de producto",
                        CreatedByUserId = usuarioId
                    }, token);

                    await _unitOfWork.SaveChangesAsync(token);
                }
            }, ct: ct);

            var creado = await _unitOfWork.Productos.GetByIdWithDetailsAsync(producto.Id, tracking: false, ct) ?? producto;
            return ApiResponse<ProductoDto>.Ok(_mapper.Map<ProductoDto>(creado), "Producto creado exitosamente.");
        }

        public async Task<ApiResponse<ProductoDto>> UpdateAsync(
            int id,
            ActualizarProductoDto dto,
            int? usuarioModificacionId = null,
            CancellationToken ct = default)
        {
            var producto = await _unitOfWork.Productos.GetByIdWithDetailsAsync(id, tracking: true, ct);
            if (producto == null)
                return ApiResponse<ProductoDto>.Fail("El producto solicitado no existe.");

            var categoria = await _unitOfWork.Catalogos.GetCategoriaProductoByIdAsync(dto.CategoriaId, ct);
            if (categoria == null)
                return ApiResponse<ProductoDto>.Fail("La categoría de producto especificada no existe.");

            if (dto.ProveedorId.HasValue)
            {
                var proveedor = await _unitOfWork.Proveedores.GetByIdAsync(dto.ProveedorId.Value, ct);
                if (proveedor == null)
                    return ApiResponse<ProductoDto>.Fail("El proveedor especificado no existe.");
            }

            if (!string.IsNullOrWhiteSpace(dto.CodigoBarras) &&
                await _unitOfWork.Productos.ExistsCodigoBarraAsync(dto.CodigoBarras, id, ct))
            {
                return ApiResponse<ProductoDto>.Fail("Ya existe otro producto registrado con ese código de barras.");
            }

            decimal costoAnterior = producto.PrecioCosto;
            decimal ventaAnterior = producto.PrecioVenta;
            bool cambioPrecio = costoAnterior != dto.PrecioCosto || ventaAnterior != dto.PrecioVenta;

            await _unitOfWork.ExecuteInTransactionAsync(async token =>
            {
                producto.CategoriaId = dto.CategoriaId;
                producto.ProveedorId = dto.ProveedorId;
                producto.CodigoBarras = string.IsNullOrWhiteSpace(dto.CodigoBarras) ? null : dto.CodigoBarras.Trim();
                producto.Nombre = dto.Nombre.Trim();
                producto.PrecioCosto = dto.PrecioCosto;
                producto.PrecioVenta = dto.PrecioVenta;
                producto.StockMinimoAlerta = dto.StockMinimoAlerta;
                producto.EsParaVenta = dto.EsParaVenta;
                producto.UpdatedByUserId = usuarioModificacionId;

                _unitOfWork.Productos.Update(producto);

                // Regla 3.3: Auditoría automática en HistorialPreciosProducto ante cambios de costo o venta
                if (cambioPrecio)
                {
                    await _unitOfWork.Productos.AddHistorialPrecioAsync(new HistorialPrecioProducto
                    {
                        ProductoId = producto.Id,
                        PrecioCostoAnterior = costoAnterior,
                        PrecioCostoNuevo = dto.PrecioCosto,
                        PrecioVentaAnterior = ventaAnterior,
                        PrecioVentaNuevo = dto.PrecioVenta,
                        FechaCambio = DateTime.UtcNow,
                        UsuarioModificacionId = usuarioModificacionId,
                        Motivo = "Actualización de precios en catálogo de productos",
                        CreatedByUserId = usuarioModificacionId
                    }, token);
                }

                await _unitOfWork.SaveChangesAsync(token);
            }, ct: ct);

            var actualizado = await _unitOfWork.Productos.GetByIdWithDetailsAsync(id, tracking: false, ct) ?? producto;
            return ApiResponse<ProductoDto>.Ok(_mapper.Map<ProductoDto>(actualizado), "Producto actualizado correctamente.");
        }

        public Task<ApiResponse<ProductoDto>> UpdateAsync(int id, CrearProductoDto dto)
        {
            return UpdateAsync(id, new ActualizarProductoDto
            {
                CategoriaId = dto.CategoriaId,
                ProveedorId = dto.ProveedorId,
                CodigoBarras = dto.CodigoBarras,
                Nombre = dto.Nombre,
                PrecioCosto = dto.PrecioCosto,
                PrecioVenta = dto.PrecioVenta,
                StockMinimoAlerta = dto.StockMinimoAlerta,
                EsParaVenta = dto.EsParaVenta
            });
        }

        public async Task<ApiResponse<bool>> DeleteAsync(int id, CancellationToken ct = default)
        {
            var producto = await _unitOfWork.Productos.GetByIdWithDetailsAsync(id, tracking: true, ct);
            if (producto == null)
                return ApiResponse<bool>.Fail("El producto solicitado no existe.");

            _unitOfWork.Productos.Delete(producto);
            await _unitOfWork.SaveChangesAsync(ct);

            return ApiResponse<bool>.Ok(true, "Producto dado de baja correctamente.");
        }

        public async Task<ApiResponse<bool>> RegistrarMovimientoStockAsync(
            MovimientoStockDto dto,
            int usuarioId = 1,
            CancellationToken ct = default)
        {
            if (!Enum.IsDefined(typeof(TipoMovimientoStockEnum), dto.TipoMovimiento))
                return ApiResponse<bool>.Fail("El tipo de movimiento de stock especificado no es válido.");

            var producto = await _unitOfWork.Productos.GetByIdWithDetailsAsync(dto.ProductoId, tracking: true, ct);
            if (producto == null)
                return ApiResponse<bool>.Fail("El producto especificado no existe.");

            int cantidadAbs = Math.Abs(dto.Cantidad);
            if (cantidadAbs <= 0)
                return ApiResponse<bool>.Fail("La cantidad del movimiento de stock debe ser mayor a cero.");

            int delta = dto.TipoMovimiento switch
            {
                TipoMovimientoStockEnum.IngresoCompra => cantidadAbs,
                TipoMovimientoStockEnum.EgresoVentaMostrador => -cantidadAbs,
                TipoMovimientoStockEnum.ConsumoInternoSalon => -cantidadAbs,
                TipoMovimientoStockEnum.AjusteRoturaPerdida => -cantidadAbs,
                _ => -cantidadAbs
            };

            if (producto.StockActual + delta < 0)
            {
                return ApiResponse<bool>.Fail(
                    $"Stock insuficiente para realizar el egreso. Stock actual disponible: {producto.StockActual}.");
            }

            int saldoResultante = 0;

            await _unitOfWork.ExecuteInTransactionAsync(async token =>
            {
                producto.StockActual += delta;
                producto.UpdatedByUserId = usuarioId;
                saldoResultante = producto.StockActual;

                _unitOfWork.Productos.Update(producto);

                await _unitOfWork.Productos.RegistrarMovimientoAsync(new MovimientoStock
                {
                    ProductoId = producto.Id,
                    TipoMovimiento = dto.TipoMovimiento,
                    Cantidad = cantidadAbs,
                    SaldoResultante = saldoResultante,
                    Fecha = DateTime.UtcNow,
                    UsuarioId = usuarioId,
                    Motivo = string.IsNullOrWhiteSpace(dto.Motivo)
                        ? $"Movimiento manual ({dto.TipoMovimiento})"
                        : dto.Motivo.Trim(),
                    CreatedByUserId = usuarioId
                }, token);

                await _unitOfWork.SaveChangesAsync(token);
            }, ct: ct);

            return ApiResponse<bool>.Ok(
                true,
                $"Stock actualizado correctamente. Saldo resultante: {saldoResultante}.");
        }

        public async Task<ApiResponse<PagedResult<KardexItemResponseDto>>> ObtenerKardexProductoAsync(
            int productoId,
            PaginationFilterDto filter,
            CancellationToken ct = default)
        {
            var producto = await _unitOfWork.Productos.GetByIdWithDetailsAsync(productoId, tracking: false, ct);
            if (producto == null)
                return ApiResponse<PagedResult<KardexItemResponseDto>>.Fail("El producto solicitado no existe.");

            var effectiveFilter = filter ?? new PaginationFilterDto();
            var (items, totalRecords) = await _unitOfWork.Productos.GetKardexPagedAsync(productoId, effectiveFilter, ct);

            var dtos = _mapper.Map<List<KardexItemResponseDto>>(items);
            foreach (var item in dtos)
            {
                if (string.IsNullOrWhiteSpace(item.ProductoNombre))
                    item.ProductoNombre = producto.Nombre;
            }

            var paged = new PagedResult<KardexItemResponseDto>
            {
                Data = dtos,
                TotalRecords = totalRecords,
                PageNumber = effectiveFilter.PageNumber,
                PageSize = effectiveFilter.PageSize
            };

            return ApiResponse<PagedResult<KardexItemResponseDto>>.Ok(paged);
        }

        public Task<ApiResponse<PagedResult<KardexItemResponseDto>>> ObtenerKardexProductoAsync(
            int productoId,
            CancellationToken ct = default)
        {
            return ObtenerKardexProductoAsync(productoId, new PaginationFilterDto(), ct);
        }
    }
}
