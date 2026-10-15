using FluentValidation;
using MiPeluqueria.Api.DTOs.Common;

namespace MiPeluqueria.Api.Validators.Common
{
    public class CrearCatalogoItemDtoValidator : AbstractValidator<CrearCatalogoItemDto>
    {
        public CrearCatalogoItemDtoValidator()
        {
            RuleFor(x => x.Nombre)
                .NotEmpty().WithMessage("El nombre del ítem de catálogo es obligatorio.")
                .MaximumLength(100).WithMessage("El nombre no puede superar los 100 caracteres.");

            RuleFor(x => x.Descripcion)
                .MaximumLength(250).WithMessage("La descripción no puede superar los 250 caracteres.");
        }
    }
}