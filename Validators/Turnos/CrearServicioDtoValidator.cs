using FluentValidation;
using MiPeluqueria.Api.DTOs.Turnos;

namespace MiPeluqueria.Api.Validators.Turnos
{
    public class CrearServicioDtoValidator : AbstractValidator<CrearServicioDto>
    {
        public CrearServicioDtoValidator()
        {
            RuleFor(x => x.CategoriaId)
                .GreaterThan(0).WithMessage("Debe seleccionar una categoría de servicio válida.");

            RuleFor(x => x.Nombre)
                .NotEmpty().WithMessage("El nombre del servicio es obligatorio.")
                .Length(3, 150).WithMessage("El nombre debe tener entre 3 y 150 caracteres.");

            RuleFor(x => x.Precio)
                .GreaterThan(0).WithMessage("El precio del servicio debe ser mayor a cero.");

            RuleFor(x => x.DuracionMinutos)
                .InclusiveBetween(5, 480).WithMessage("La duración debe oscilar entre 5 y 480 minutos.");
        }
    }
}