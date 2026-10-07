using FluentValidation;
using MiPeluqueria.Api.DTOs.Turnos;

namespace MiPeluqueria.Api.Validators.Turnos
{
    public class BloqueoHorarioDtoValidator : AbstractValidator<BloqueoHorarioDto>
    {
        public BloqueoHorarioDtoValidator()
        {
            RuleFor(x => x.PeluqueroId)
                .GreaterThan(0).WithMessage("Debe indicar el profesional al que se aplica el bloqueo.");

            RuleFor(x => x.FechaInicio)
                .NotEmpty().WithMessage("La fecha y hora de inicio del bloqueo es obligatoria.");

            RuleFor(x => x.FechaFin)
                .NotEmpty().WithMessage("La fecha y hora de fin del bloqueo es obligatoria.")
                .GreaterThan(x => x.FechaInicio)
                .WithMessage("La fecha de fin del bloqueo debe ser posterior a la fecha de inicio.");

            RuleFor(x => x.Motivo)
                .NotEmpty().WithMessage("El motivo del bloqueo horario es obligatorio.")
                .MaximumLength(200).WithMessage("El motivo no puede superar los 200 caracteres.");
        }
    }
}