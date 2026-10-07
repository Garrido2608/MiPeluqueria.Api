using FluentValidation;
using MiPeluqueria.Api.DTOs.Turnos;

namespace MiPeluqueria.Api.Validators.Turnos
{
    public class CrearPeluqueroDtoValidator : AbstractValidator<CrearPeluqueroDto>
    {
        public CrearPeluqueroDtoValidator()
        {
            RuleFor(x => x.Nombre)
                .NotEmpty().WithMessage("El nombre del profesional es obligatorio.")
                .MaximumLength(100).WithMessage("El nombre no puede superar los 100 caracteres.");

            RuleFor(x => x.Apellido)
                .NotEmpty().WithMessage("El apellido del profesional es obligatorio.")
                .MaximumLength(100).WithMessage("El apellido no puede superar los 100 caracteres.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("El email es obligatorio.")
                .EmailAddress().WithMessage("El formato del email no es válido.")
                .MaximumLength(150);

            RuleFor(x => x.ColorAgendaHex)
                .NotEmpty().WithMessage("El color de agenda es obligatorio.")
                .Matches("^#[0-9A-Fa-f]{6}$").WithMessage("El color debe estar en formato hexadecimal válido (ej. #3B82F6).");
        }
    }
}