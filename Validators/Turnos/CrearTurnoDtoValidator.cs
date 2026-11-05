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

    public class ReprogramarTurnoDtoValidator : AbstractValidator<ReprogramarTurnoDto>
    {
        public ReprogramarTurnoDtoValidator()
        {
            RuleFor(x => x.PeluqueroId)
                .GreaterThan(0).WithMessage("Debe seleccionar un profesional válido.");

            RuleFor(x => x.NuevaFechaHoraInicio)
                .GreaterThan(DateTime.UtcNow.AddMinutes(-10))
                .WithMessage("La nueva fecha y hora del turno no puede estar en el pasado.");

            RuleFor(x => x.Observaciones)
                .MaximumLength(300).WithMessage("Las observaciones no pueden exceder los 300 caracteres.");
        }
    }

    public class ReservaPublicaRequestDtoValidator : AbstractValidator<ReservaPublicaRequestDto>
    {
        public ReservaPublicaRequestDtoValidator()
        {
            RuleFor(x => x.NombreCliente)
                .NotEmpty().WithMessage("El nombre del cliente es obligatorio.")
                .MaximumLength(100);

            RuleFor(x => x.ApellidoCliente)
                .NotEmpty().WithMessage("El apellido del cliente es obligatorio.")
                .MaximumLength(100);

            RuleFor(x => x.TelefonoCliente)
                .NotEmpty().WithMessage("El teléfono del cliente es obligatorio.")
                .MaximumLength(30);

            RuleFor(x => x.EmailCliente)
                .EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.EmailCliente))
                .MaximumLength(150);

            RuleFor(x => x.PeluqueroId)
                .GreaterThan(0).WithMessage("Debe seleccionar un profesional válido.");

            RuleFor(x => x.FechaHoraInicio)
                .GreaterThan(DateTime.UtcNow.AddMinutes(-10))
                .WithMessage("La fecha y hora de la reserva no puede estar en el pasado.");

            RuleFor(x => x.ServiciosIds)
                .NotEmpty().WithMessage("Debe seleccionar al menos un servicio.");
        }
    }

    public class ConfigurarRecetaServicioDtoValidator : AbstractValidator<ConfigurarRecetaServicioDto>
    {
        public ConfigurarRecetaServicioDtoValidator()
        {
            RuleFor(x => x.ServicioId)
                .GreaterThan(0).WithMessage("Debe indicar un servicio válido.");

            RuleFor(x => x.Insumos)
                .NotNull().WithMessage("La lista de insumos no puede ser nula.");

            RuleForEach(x => x.Insumos).ChildRules(insumo =>
            {
                insumo.RuleFor(i => i.ProductoId)
                    .GreaterThan(0).WithMessage("Debe indicar un producto válido.");

                insumo.RuleFor(i => i.CantidadUnidadesDescontar)
                    .GreaterThan(0).WithMessage("La cantidad de unidades a descontar debe ser mayor a cero.");

                insumo.RuleFor(i => i.CantidadMedidaEstandar)
                    .GreaterThan(0).WithMessage("La cantidad de medida estándar debe ser mayor a cero.");
            });
        }
    }
}