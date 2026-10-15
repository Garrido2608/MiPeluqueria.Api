using FluentValidation;
using MiPeluqueria.Api.DTOs.Ventas;

namespace MiPeluqueria.Api.Validators.Ventas
{
    public class GuardarComisionConfigDtoValidator : AbstractValidator<GuardarComisionConfigDto>
    {
        public GuardarComisionConfigDtoValidator()
        {
            RuleFor(x => x.PeluqueroId)
                .GreaterThan(0).WithMessage("Debe especificar un peluquero válido.");

            RuleFor(x => x.ServicioId)
                .GreaterThan(0).When(x => x.ServicioId.HasValue)
                .WithMessage("El identificador de servicio debe ser mayor a cero.");

            RuleFor(x => x.PorcentajeServicio)
                .InclusiveBetween(0, 100)
                .WithMessage("El porcentaje de comisión por servicio debe estar entre 0% y 100%.");

            RuleFor(x => x.PorcentajeVentaProducto)
                .InclusiveBetween(0, 100)
                .WithMessage("El porcentaje de comisión por venta de producto debe estar entre 0% y 100%.");
        }
    }
}