using AutoMapper;
using Microsoft.EntityFrameworkCore;
using MiPeluqueria.Api.Data.Context;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Stock;
using MiPeluqueria.Api.Models.Stock;
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Services.Implementations
{
    public class ProductoService : IProductoService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public ProductoService(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<ApiResponse<IEnumerable<ProductoDto>>> GetAllAsync(bool soloBajoStock = false)
        {
            if (soloBajoStock)
                return await GetStockCriticoAsync();

            var productos = await _context.Productos
                .AsNoTracking()
                .ToListAsync();

            var dtos = _mapper.Map<IEnumerable<ProductoDto>>(productos);
            return ApiResponse<IEnumerable<ProductoDto>>.Ok(dtos);
        }

        public async Task<ApiResponse<IEnumerable<ProductoDto>>> GetStockCriticoAsync()
        {
            var productos = await _context.Productos
                .Where(p => p.StockActual <= p.StockMinimoAlerta)
                .AsNoTracking()
                .ToListAsync();

            var dtos = _mapper.Map<IEnumerable<ProductoDto>>(productos);
            return ApiResponse<IEnumerable<ProductoDto>>.Ok(dtos);
        }

        public async Task<ApiResponse<ProductoDto>> GetByIdAsync(int id)
        {
            var producto = await _context.Productos
                .FirstOrDefaultAsync(p => p.Id == id);

            if (producto == null)
                return ApiResponse<ProductoDto>.Fail("El producto solicitado no existe.");

            return ApiResponse<ProductoDto>.Ok(_mapper.Map<ProductoDto>(producto));
        }

        public async Task<ApiResponse<ProductoDto>> CreateAsync(CrearProductoDto dto, int usuarioId = 1)
        {
            var producto = _mapper.Map<Producto>(dto);
            await _context.Productos.AddAsync(producto);
            await _context.SaveChangesAsync();

            if (producto.StockActual > 0)
            {
                var movInicial = new MovimientoStock
                {
                    ProductoId = producto.Id,
                    TipoMovimiento = (TipoMovimientoStockEnum)1,
                    Cantidad = producto.StockActual,
                    Motivo = "Inventario inicial de alta de producto"
                };
                SetOptionalProp(movInicial, "UsuarioId", usuarioId);
                SetOptionalProp(movInicial, "StockResultante", producto.StockActual);

                await _context.MovimientosStock.AddAsync(movInicial);
                await _context.SaveChangesAsync();
            }

            return ApiResponse<ProductoDto>.Ok(_mapper.Map<ProductoDto>(producto), "Producto creado exitosamente.");
        }

        public async Task<ApiResponse<ProductoDto>> UpdateAsync(int id, CrearProductoDto dto)
        {
            var producto = await _context.Productos.FirstOrDefaultAsync(p => p.Id == id);
            if (producto == null)
                return ApiResponse<ProductoDto>.Fail("El producto solicitado no existe.");

            var stockActualPrevio = producto.StockActual;
            _mapper.Map(dto, producto);
            producto.StockActual = stockActualPrevio;

            _context.Productos.Update(producto);
            await _context.SaveChangesAsync();

            return ApiResponse<ProductoDto>.Ok(_mapper.Map<ProductoDto>(producto), "Producto actualizado correctamente.");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(int id)
        {
            var producto = await _context.Productos.FirstOrDefaultAsync(p => p.Id == id);
            if (producto == null)
                return ApiResponse<bool>.Fail("El producto solicitado no existe.");

            producto.IsDeleted = true;
            await _context.SaveChangesAsync();

            return ApiResponse<bool>.Ok(true, "Producto dado de baja correctamente.");
        }

        public async Task<ApiResponse<bool>> RegistrarMovimientoStockAsync(MovimientoStockDto dto, int usuarioId = 1)
        {
            var producto = await _context.Productos.FirstOrDefaultAsync(p => p.Id == dto.ProductoId);
            if (producto == null)
                return ApiResponse<bool>.Fail("El producto especificado no existe.");

            var nombreTipo = dto.TipoMovimiento.ToString().ToLower();
            bool esEgreso = nombreTipo.Contains("egreso") ||
                            nombreTipo.Contains("salida") ||
                            nombreTipo.Contains("venta") ||
                            nombreTipo.Contains("consumo") ||
                            nombreTipo.Contains("negativo") ||
                            Convert.ToInt32(dto.TipoMovimiento) == 2;

            int delta = esEgreso ? -Math.Abs(dto.Cantidad) : Math.Abs(dto.Cantidad);

            if (producto.StockActual + delta < 0)
                return ApiResponse<bool>.Fail($"Stock insuficiente. Stock actual: {producto.StockActual}.");

            producto.StockActual += delta;
            _context.Productos.Update(producto);

            var movimiento = new MovimientoStock
            {
                ProductoId = producto.Id,
                TipoMovimiento = dto.TipoMovimiento,
                Cantidad = Math.Abs(dto.Cantidad),
                Motivo = dto.Motivo
            };
            SetOptionalProp(movimiento, "UsuarioId", usuarioId);
            SetOptionalProp(movimiento, "StockResultante", producto.StockActual);

            await _context.MovimientosStock.AddAsync(movimiento);
            await _context.SaveChangesAsync();

            return ApiResponse<bool>.Ok(true, $"Stock actualizado correctamente. Stock resultante: {producto.StockActual}.");
        }

        public async Task<ApiResponse<IEnumerable<object>>> ObtenerKardexProductoAsync(int productoId)
        {
            var existe = await _context.Productos.AnyAsync(p => p.Id == productoId);
            if (!existe)
                return ApiResponse<IEnumerable<object>>.Fail("El producto solicitado no existe.");

            var movimientos = await _context.MovimientosStock
                .Where(m => m.ProductoId == productoId)
                .OrderByDescending(m => m.Id)
                .AsNoTracking()
                .ToListAsync();

            var resultado = movimientos.Select(m => (object)new
            {
                m.Id,
                m.ProductoId,
                TipoMovimiento = m.TipoMovimiento.ToString(),
                m.Cantidad,
                m.Motivo
            }).ToList();

            return ApiResponse<IEnumerable<object>>.Ok(resultado);
        }

        private static void SetOptionalProp(object target, string propName, object value)
        {
            var prop = target.GetType().GetProperty(propName);
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(target, value);
            }
        }
    }
}