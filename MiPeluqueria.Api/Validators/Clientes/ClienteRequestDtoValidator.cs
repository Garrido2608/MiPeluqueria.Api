using FluentValidation;
using MiPeluqueria.Api.DTOs.Clientes;

namespace MiPeluqueria.Api.Validators.Clientes
{
    // Heredamos de AbstractValidator e indicamos a qué DTO va a auditar
    public class ClienteRequestDtoValidator : AbstractValidator<ClienteRequestDto>
    {
        public ClienteRequestDtoValidator()
        {
            RuleFor(x => x.Nombre)
                .NotEmpty().WithMessage("El nombre es obligatorio.")
                .Length(2, 100).WithMessage("El nombre debe tener entre 2 y 100 caracteres.");

            RuleFor(x => x.Apellido)
                .NotEmpty().WithMessage("El apellido es obligatorio.")
                .Length(2, 100).WithMessage("El apellido debe tener entre 2 y 100 caracteres.");

            RuleFor(x => x.Telefono)
                .NotEmpty().WithMessage("El teléfono es obligatorio.")
                .MaximumLength(30).WithMessage("El teléfono no puede superar los 30 caracteres.");

            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("El formato de correo no es válido.")
                .When(x => !string.IsNullOrEmpty(x.Email)); // Se valida solo si el usuario mandó algo

            RuleFor(x => x.FechaNacimiento)
                .NotEmpty().WithMessage("La fecha de nacimiento es obligatoria para el programa de fidelización.");
        }
    }
}