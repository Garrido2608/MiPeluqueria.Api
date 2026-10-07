using FluentValidation;
using MiPeluqueria.Api.DTOs.Stock;

namespace MiPeluqueria.Api.Validators.Stock
{
    public class CrearProductoDtoValidator : AbstractValidator<CrearProductoDto>
    {
        public CrearProductoDtoValidator()
        {
            RuleFor(x => x.CategoriaId)
                .GreaterThan(0).WithMessage("Debe seleccionar una categoría de producto válida.");

            RuleFor(x => x.Nombre)
                .NotEmpty().WithMessage("El nombre del producto es obligatorio.")
                .MaximumLength(150).WithMessage("El nombre no puede superar los 150 caracteres.");

            RuleFor(x => x.PrecioCosto)
                .GreaterThanOrEqualTo(0).WithMessage("El precio de costo no puede ser negativo.");

            RuleFor(x => x.PrecioVenta)
                .GreaterThan(0).WithMessage("El precio de venta debe ser mayor a cero.")
                .GreaterThanOrEqualTo(x => x.PrecioCosto)
                .WithMessage("El precio de venta no puede ser inferior al precio de costo.");

            RuleFor(x => x.StockInicial)
                .GreaterThanOrEqualTo(0).WithMessage("El stock inicial no puede ser negativo.");

            RuleFor(x => x.StockMinimoAlerta)
                .GreaterThanOrEqualTo(0).WithMessage("El stock mínimo de alerta no puede ser negativo.");
        }
    }

    public class MovimientoStockDtoValidator : AbstractValidator<MovimientoStockDto>
    {
        public MovimientoStockDtoValidator()
        {
            RuleFor(x => x.ProductoId)
                .GreaterThan(0).WithMessage("Debe indicar un producto válido.");

            RuleFor(x => x.TipoMovimiento)
                .IsInEnum().WithMessage("El tipo de movimiento de stock no es válido.");

            RuleFor(x => x.Cantidad)
                .GreaterThan(0).WithMessage("La cantidad del movimiento debe ser mayor a cero.");

            RuleFor(x => x.Motivo)
                .MaximumLength(300).WithMessage("El motivo no puede exceder los 300 caracteres.");
        }
    }
}