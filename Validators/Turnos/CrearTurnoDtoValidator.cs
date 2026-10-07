using System;
using FluentValidation;
using MiPeluqueria.Api.DTOs.Turnos;

namespace MiPeluqueria.Api.Validators.Turnos
{
    public class CrearTurnoDtoValidator : AbstractValidator<CrearTurnoDto>
    {
        public CrearTurnoDtoValidator()
        {
            RuleFor(x => x.ClienteId)
                .GreaterThan(0).WithMessage("Debe seleccionar un cliente válido.");

            RuleFor(x => x.PeluqueroId)
                .GreaterThan(0).WithMessage("Debe seleccionar un profesional válido.");

            RuleFor(x => x.FechaHoraInicio)
                .GreaterThan(DateTime.UtcNow.AddMinutes(-10))
                .WithMessage("La fecha y hora del turno no puede estar en el pasado.");

            RuleFor(x => x.ServiciosIds)
                .NotEmpty().WithMessage("Debe seleccionar al menos un servicio para el turno.")
                .Must(list => list != null && list.Count > 0)
                .WithMessage("La lista de servicios del turno no puede estar vacía.");

            RuleFor(x => x.Observaciones)
                .MaximumLength(500).WithMessage("Las observaciones no pueden exceder los 500 caracteres.");
        }
    }
}