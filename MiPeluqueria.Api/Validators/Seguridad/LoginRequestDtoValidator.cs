using FluentValidation;
using MiPeluqueria.Api.DTOs.Seguridad;

namespace MiPeluqueria.Api.Validators.Seguridad
{
    public class LoginRequestDtoValidator : AbstractValidator<LoginRequestDto>
    {
        public LoginRequestDtoValidator()
        {
            RuleFor(x => x.UsernameOrEmail)
                .NotEmpty().WithMessage("El usuario o correo electrónico es obligatorio.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("La contraseña es obligatoria.");
        }
    }
}