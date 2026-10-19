using FluentValidation;
using MiPeluqueria.Api.DTOs.Common;

namespace MiPeluqueria.Api.Validators.Common
{
    public class ActualizarConfiguracionSalonDtoValidator : AbstractValidator<ActualizarConfiguracionSalonDto>
    {
        public ActualizarConfiguracionSalonDtoValidator()
        {
            RuleFor(x => x.NombreSalon)
                .NotEmpty().WithMessage("El nombre del salón es obligatorio.")
                .MaximumLength(150);

            RuleFor(x => x.RazonSocial)
                .MaximumLength(150);

            RuleFor(x => x.Cuit)
                .MaximumLength(20);

            RuleFor(x => x.EmailContacto)
                .EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.EmailContacto))
                .WithMessage("El email de contacto no tiene un formato válido.")
                .MaximumLength(150);

            RuleFor(x => x.MonedaIso)
                .NotEmpty()
                .Length(3).WithMessage("El código de moneda ISO debe tener exactamente 3 letras (ej: ARS, USD, EUR).");

            RuleFor(x => x.TimeZoneId)
                .NotEmpty().WithMessage("El huso horario (TimeZoneId) es obligatorio.")
                .MaximumLength(100);

            RuleFor(x => x.IntervaloTurnosMinutos)
                .InclusiveBetween(5, 120)
                .WithMessage("El intervalo de turnos debe estar entre 5 y 120 minutos.");

            RuleFor(x => x.MinutosToleranciaEspera)
                .InclusiveBetween(0, 60)
                .WithMessage("La tolerancia de espera debe estar entre 0 y 60 minutos.");

            RuleFor(x => x.HorasAnticipacionCancelacion)
                .InclusiveBetween(0, 168)
                .WithMessage("Las horas de anticipación para cancelación deben estar entre 0 y 168 horas.");

            RuleFor(x => x.PorcentajeSenaReserva)
                .InclusiveBetween(0, 100)
                .WithMessage("El porcentaje de seña debe estar entre 0% y 100%.");
        }
    }
}