using FluentValidation;
using MiPeluqueria.Api.DTOs.Stock;

namespace MiPeluqueria.Api.Validators.Stock
{
    public class ActualizarProductoDtoValidator : AbstractValidator<ActualizarProductoDto>
    {
        public ActualizarProductoDtoValidator()
        {
            RuleFor(x => x.CategoriaId)
                .GreaterThan(0).WithMessage("Debe seleccionar una categoría válida.");

            RuleFor(x => x.Nombre)
                .NotEmpty().WithMessage("El nombre del producto es obligatorio.")
                .MaximumLength(150);

            RuleFor(x => x.PrecioCosto)
                .GreaterThanOrEqualTo(0).WithMessage("El precio de costo no puede ser negativo.");

            RuleFor(x => x.PrecioVenta)
                .GreaterThan(0).WithMessage("El precio de venta debe ser mayor a cero.")
                .GreaterThanOrEqualTo(x => x.PrecioCosto)
                .WithMessage("El precio de venta no puede ser inferior al precio de costo.");

            RuleFor(x => x.StockMinimoAlerta)
                .GreaterThanOrEqualTo(0).WithMessage("El stock mínimo de alerta no puede ser negativo.");
        }
    }

    public class ActualizarProveedorDtoValidator : AbstractValidator<ActualizarProveedorDto>
    {
        public ActualizarProveedorDtoValidator()
        {
            RuleFor(x => x.RazonSocial)
                .NotEmpty().WithMessage("La razón social del proveedor es obligatoria.")
                .MaximumLength(150);

            RuleFor(x => x.Email)
                .EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email))
                .WithMessage("El formato del correo electrónico no es válido.");
        }
    }
}