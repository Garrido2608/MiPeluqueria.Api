using System.Linq;
using FluentValidation;
using MiPeluqueria.Api.DTOs.Turnos;

namespace MiPeluqueria.Api.Validators.Turnos
{
    public class HorarioLaboralDtoValidator : AbstractValidator<HorarioLaboralDto>
    {
        public HorarioLaboralDtoValidator()
        {
            RuleFor(x => x.DiaSemana)
                .InclusiveBetween(0, 6)
                .WithMessage("El día de la semana debe estar entre 0 (Domingo) y 6 (Sábado).");

            RuleFor(x => x.HoraInicio)
                .LessThan(x => x.HoraFin)
                .When(x => x.Activo)
                .WithMessage("La hora de inicio debe ser anterior a la hora de finalización de la jornada.");

            RuleFor(x => x.HoraInicioDescanso)
                .LessThan(x => x.HoraFinDescanso)
                .When(x => x.HoraInicioDescanso.HasValue && x.HoraFinDescanso.HasValue)
                .WithMessage("La hora de inicio de descanso debe ser anterior a la hora de fin de descanso.");

            RuleFor(x => x.HoraInicioDescanso)
                .GreaterThanOrEqualTo(x => x.HoraInicio)
                .When(x => x.HoraInicioDescanso.HasValue && x.Activo)
                .WithMessage("El descanso no puede comenzar antes del inicio de la jornada laboral.");

            RuleFor(x => x.HoraFinDescanso)
                .LessThanOrEqualTo(x => x.HoraFin)
                .When(x => x.HoraFinDescanso.HasValue && x.Activo)
                .WithMessage("El descanso no puede finalizar después del cierre de la jornada laboral.");
        }
    }

    public class GuardarHorariosPeluqueroDtoValidator : AbstractValidator<GuardarHorariosPeluqueroDto>
    {
        public GuardarHorariosPeluqueroDtoValidator()
        {
            RuleFor(x => x.Horarios)
                .NotNull().WithMessage("La lista de horarios no puede ser nula.");

            RuleForEach(x => x.Horarios)
                .SetValidator(new HorarioLaboralDtoValidator());

            RuleFor(x => x.Horarios)
                .Must(horarios => horarios == null || horarios.Select(h => h.DiaSemana).Distinct().Count() == horarios.Count)
                .WithMessage("No se pueden enviar días de la semana duplicados para el mismo peluquero.");
        }
    }
}