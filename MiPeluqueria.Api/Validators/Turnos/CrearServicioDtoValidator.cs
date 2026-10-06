using FluentValidation;
using MiPeluqueria.Api.DTOs.Turnos;

namespace MiPeluqueria.Api.Validators.Turnos
{
    public class CrearServicioDtoValidator : AbstractValidator<CrearServicioDto>
    {
        public CrearServicioDtoValidator()
        {
            RuleFor(x => x.CategoriaId)
                .GreaterThan(0).WithMessage("Debe seleccionar una categoría válida.");

            RuleFor(x => x.Nombre)
                .NotEmpty().WithMessage("El nombre del servicio es obligatorio.")
                .MaximumLength(150).WithMessage("El nombre no puede superar los 150 caracteres.");

            RuleFor(x => x.Precio)
                .GreaterThan(0).WithMessage("El precio debe ser mayor a 0.");

            RuleFor(x => x.DuracionMinutos)
                .InclusiveBetween(5, 480).WithMessage("La duración debe oscilar entre 5 y 480 minutos.");
        }
    }
}