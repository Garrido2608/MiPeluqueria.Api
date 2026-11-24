using FluentValidation;
using MiPeluqueria.Api.DTOs.Turnos;
using MiPeluqueria.Api.Models.Turnos;

namespace MiPeluqueria.Api.Validators.Turnos
{
    public class CambiarEstadoTurnoDtoValidator : AbstractValidator<CambiarEstadoTurnoDto>
    {
        public CambiarEstadoTurnoDtoValidator()
        {
            RuleFor(x => x.NuevoEstadoId)
                .InclusiveBetween(1, 6)
                .WithMessage("El estado seleccionado no pertenece al catálogo válido de estados de turno (1 a 6).");

            RuleFor(x => x.MotivoCancelacionId)
                .NotNull()
                .GreaterThan(0)
                .When(x => x.NuevoEstadoId == (int)EstadoTurnoEnum.Cancelado)
                .WithMessage("Es obligatorio indicar el motivo de cancelación cuando se cancela un turno.");

            RuleFor(x => x.Observaciones)
                .MaximumLength(300).WithMessage("Las observaciones no pueden superar los 300 caracteres.");
        }
    }
}