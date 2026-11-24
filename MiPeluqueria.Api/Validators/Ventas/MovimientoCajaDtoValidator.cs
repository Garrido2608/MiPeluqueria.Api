using FluentValidation;
using MiPeluqueria.Api.DTOs.Ventas;

namespace MiPeluqueria.Api.Validators.Ventas
{
    public class RegistrarMovimientoCajaDtoValidator : AbstractValidator<RegistrarMovimientoCajaDto>
    {
        public RegistrarMovimientoCajaDtoValidator()
        {
            RuleFor(x => x.SesionCajaId)
                .GreaterThan(0).WithMessage("Debe especificar una sesión de caja válida.");

            RuleFor(x => x.Monto)
                .GreaterThan(0).WithMessage("El monto del movimiento de caja debe ser mayor a cero.");

            RuleFor(x => x.Concepto)
                .NotEmpty().WithMessage("Debe especificar el concepto del movimiento de caja.")
                .MaximumLength(250).WithMessage("El concepto no puede exceder los 250 caracteres.");
        }
    }
}