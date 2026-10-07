using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly IProductoRepository _productoRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ProductoService(
            IProductoRepository productoRepo,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _productoRepo = productoRepo;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ApiResponse<List<ProductoDto>>> GetAllAsync()
        {
            var productos = await _productoRepo.GetAllAsync();
            var dtos = _mapper.Map<List<ProductoDto>>(productos);
            return ApiResponse<List<ProductoDto>>.Exito(dtos, "Inventario de productos obtenido.");
        }

        public async Task<ApiResponse<List<ProductoDto>>> GetStockCriticoAsync()
        {
            var productos = await _productoRepo.GetAllAsync();
            var criticos = productos.Where(p => p.StockActual <= p.StockMinimoAlerta).ToList();
            var dtos = _mapper.Map<List<ProductoDto>>(criticos);
            return ApiResponse<List<ProductoDto>>.Exito(dtos, $"Se detectaron {dtos.Count} productos en nivel de alerta de reposición.");
        }

        public async Task<ApiResponse<ProductoDto>> GetByIdAsync(int id)
        {
            var p = await _productoRepo.GetByIdAsync(id);
            if (p == null)
                return ApiResponse<ProductoDto>.Falla($"No se encontró el producto con ID {id}.");

            return ApiResponse<ProductoDto>.Exito(_mapper.Map<ProductoDto>(p));
        }

        public async Task<ApiResponse<ProductoDto>> CreateAsync(CrearProductoDto dto, int usuarioId)
        {
            var producto = _mapper.Map<Producto>(dto);
            producto.StockActual = dto.StockInicial;

            if (dto.StockInicial > 0)
            {
                producto.MovimientosStock.Add(new MovimientoStock
                {
                    TipoMovimiento = TipoMovimientoStockEnum.IngresoCompra,
                    Cantidad = dto.StockInicial,
                    Fecha = DateTime.UtcNow,
                    UsuarioId = usuarioId,
                    Motivo = "Carga de stock inicial en el alta del producto."
                });
            }

            await _productoRepo.AddAsync(producto);
            await _unitOfWork.SaveChangesAsync();

            var creado = await _productoRepo.GetByIdAsync(producto.Id);
            return ApiResponse<ProductoDto>.Exito(_mapper.Map<ProductoDto>(creado ?? producto), "Producto registrado exitosamente.");
        }

        public async Task<ApiResponse<bool>> RegistrarMovimientoAsync(MovimientoStockDto dto, int usuarioId)
        {
            var p = await _productoRepo.GetByIdAsync(dto.ProductoId);
            if (p == null)
                return ApiResponse<bool>.Falla("El producto especificado no existe.");

            if (dto.TipoMovimiento == TipoMovimientoStockEnum.EgresoVentaMostrador ||
                dto.TipoMovimiento == TipoMovimientoStockEnum.ConsumoInternoSalon ||
                dto.TipoMovimiento == TipoMovimientoStockEnum.AjusteRoturaPerdida)
            {
                if (p.StockActual < dto.Cantidad)
                    return ApiResponse<bool>.Falla($"Stock insuficiente ({p.StockActual} unidades disponibles).");

                p.StockActual -= dto.Cantidad;
            }
            else if (dto.TipoMovimiento == TipoMovimientoStockEnum.IngresoCompra)
            {
                p.StockActual += dto.Cantidad;
            }

            await _productoRepo.UpdateAsync(p);
            await _productoRepo.RegistrarMovimientoAsync(new MovimientoStock
            {
                ProductoId = p.Id,
                TipoMovimiento = dto.TipoMovimiento,
                Cantidad = dto.Cantidad,
                Fecha = DateTime.UtcNow,
                UsuarioId = usuarioId,
                Motivo = dto.Motivo ?? "Ajuste manual de inventario."
            });

            await _unitOfWork.SaveChangesAsync();
            return ApiResponse<bool>.Exito(true, "Movimiento de stock asentado y existencias actualizadas.");
        }
    }
}