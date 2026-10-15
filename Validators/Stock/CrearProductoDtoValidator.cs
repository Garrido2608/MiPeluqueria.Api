using FluentValidation;
using MiPeluqueria.Api.DTOs.Stock;
using MiPeluqueria.Api.Models.Stock;

namespace MiPeluqueria.Api.Validators.Stock
{
    public class AjusteMovimientoStockDtoValidator : AbstractValidator<MovimientoStockDto>
    {
        public AjusteMovimientoStockDtoValidator()
        {
            RuleFor(x => x.ProductoId)
                .GreaterThan(0).WithMessage("Debe indicar un producto válido.");

            RuleFor(x => x.Cantidad)
                .GreaterThan(0).WithMessage("La cantidad del movimiento debe ser mayor a cero.");

            RuleFor(x => x.TipoMovimiento)
                .Must(t => Enum.IsDefined(typeof(TipoMovimientoStockEnum), t))
                .WithMessage("El tipo de movimiento de stock especificado no es válido.");

            RuleFor(x => x.Motivo)
                .NotEmpty().WithMessage("Debe especificar el motivo del movimiento de stock.")
                .MaximumLength(250).WithMessage("El motivo no puede exceder los 250 caracteres.");
        }
    }
}