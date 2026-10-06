using FluentValidation;
using MiPeluqueria.Api.DTOs.Turnos;

namespace MiPeluqueria.Api.Validators.Turnos
{
    public class CrearPeluqueroDtoValidator : AbstractValidator<CrearPeluqueroDto>
    {
        public CrearPeluqueroDtoValidator()
        {
            RuleFor(x => x.Nombre)
                .NotEmpty().WithMessage("El nombre es obligatorio.")
                .MaximumLength(100).WithMessage("El nombre no puede superar los 100 caracteres.");

            RuleFor(x => x.Apellido)
                .NotEmpty().WithMessage("El apellido es obligatorio.")
                .MaximumLength(100).WithMessage("El apellido no puede superar los 100 caracteres.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("El email es obligatorio.")
                .EmailAddress().WithMessage("El formato del email no es válido.");
        }
    }
}