using FluentValidation;
using MiPeluqueria.Api.DTOs.Turnos;
using System;

namespace MiPeluqueria.Api.Validators.Turnos
{
    public class CrearTurnoDtoValidator : AbstractValidator<CrearTurnoDto>
    {
        public CrearTurnoDtoValidator()
        {
            RuleFor(x => x.ClienteId)
                .GreaterThan(0).WithMessage("Debe especificar un cliente válido.");

            RuleFor(x => x.PeluqueroId)
                .GreaterThan(0).WithMessage("Debe especificar un profesional válido.");

            RuleFor(x => x.FechaHoraInicio)
                .GreaterThan(DateTime.UtcNow.AddMinutes(-5))
                .WithMessage("La fecha del turno no puede ser en el pasado.");

            RuleFor(x => x.ServiciosIds)
                .NotEmpty().WithMessage("Debe seleccionar al menos un servicio.")
                .Must(x => x != null && x.Count > 0).WithMessage("La lista de servicios no puede estar vacía.");
        }
    }
}