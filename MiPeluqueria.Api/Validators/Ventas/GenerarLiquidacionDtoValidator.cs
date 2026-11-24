using FluentValidation;
using MiPeluqueria.Api.DTOs.Ventas;

namespace MiPeluqueria.Api.Validators.Ventas
{
    public class GenerarLiquidacionDtoValidator : AbstractValidator<GenerarLiquidacionDto>
    {
        public GenerarLiquidacionDtoValidator()
        {
            RuleFor(x => x.PeluqueroId)
                .GreaterThan(0).WithMessage("Debe seleccionar un peluquero válido.");

            RuleFor(x => x.FechaDesde)
                .NotEmpty().WithMessage("La fecha de inicio del período es obligatoria.");

            RuleFor(x => x.FechaHasta)
                .NotEmpty().WithMessage("La fecha de fin del período es obligatoria.")
                .GreaterThanOrEqualTo(x => x.FechaDesde)
                .WithMessage("La fecha hasta debe ser mayor o igual a la fecha desde.");

            RuleFor(x => x.Bonificaciones)
                .GreaterThanOrEqualTo(0).WithMessage("Las bonificaciones no pueden ser negativas.");

            RuleFor(x => x.Descuentos)
                .GreaterThanOrEqualTo(0).WithMessage("Los descuentos no pueden ser negativos.");
        }
    }
}