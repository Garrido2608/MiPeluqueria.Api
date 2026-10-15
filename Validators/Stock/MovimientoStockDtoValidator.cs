using FluentValidation;
using MiPeluqueria.Api.DTOs.Stock;

namespace MiPeluqueria.Api.Validators.Stock
{
    public class MovimientoStockDtoValidator : AbstractValidator<MovimientoStockDto>
    {
        public MovimientoStockDtoValidator()
        {
            RuleFor(x => x.ProductoId)
                .GreaterThan(0).WithMessage("Debe indicar un producto válido.");

            RuleFor(x => x.Cantidad)
                .GreaterThan(0).WithMessage("La cantidad del movimiento debe ser mayor a cero.");

            RuleFor(x => x.TipoMovimiento)
                .IsInEnum()
                .WithMessage("El tipo de movimiento de stock especificado no es válido.");

            RuleFor(x => x.Motivo)
                .NotEmpty().WithMessage("Debe especificar el motivo del movimiento de stock.")
                .MaximumLength(250).WithMessage("El motivo no puede exceder los 250 caracteres.");
        }
    }
}