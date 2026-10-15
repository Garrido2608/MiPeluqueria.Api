using FluentValidation;
using MiPeluqueria.Api.DTOs.Seguridad;

namespace MiPeluqueria.Api.Validators.Seguridad
{
    public class RegistroUsuarioDtoValidator : AbstractValidator<RegistroUsuarioDto>
    {
        public RegistroUsuarioDtoValidator()
        {
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("El nombre de usuario es obligatorio.")
                .MinimumLength(3).WithMessage("El nombre de usuario debe tener al menos 3 caracteres.")
                .MaximumLength(50).WithMessage("El nombre de usuario no puede superar los 50 caracteres.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("El correo electrónico es obligatorio.")
                .EmailAddress().WithMessage("El formato del correo electrónico no es válido.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("La contraseña es obligatoria.")
                .MinimumLength(6).WithMessage("La contraseña debe tener al menos 6 caracteres.");

            RuleFor(x => x.AceptaTerminos)
                .Equal(true).WithMessage("Debe aceptar los términos y condiciones para registrarse.");
        }
    }

    public class VerificarEmailDtoValidator : AbstractValidator<VerificarEmailDto>
    {
        public VerificarEmailDtoValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("El correo electrónico es obligatorio.")
                .EmailAddress().WithMessage("El formato del correo electrónico no es válido.");

            RuleFor(x => x.CodigoVerificacion)
                .NotEmpty().WithMessage("El código de verificación es obligatorio.")
                .Length(6).WithMessage("El código OTP debe tener exactamente 6 dígitos.");
        }
    }

    public class CambiarPasswordDtoValidator : AbstractValidator<CambiarPasswordDto>
    {
        public CambiarPasswordDtoValidator()
        {
            RuleFor(x => x.PasswordActual)
                .NotEmpty().WithMessage("Debe ingresar su contraseña actual.");

            RuleFor(x => x.NuevaPassword)
                .NotEmpty().WithMessage("Debe ingresar la nueva contraseña.")
                .MinimumLength(6).WithMessage("La nueva contraseña debe tener al menos 6 caracteres.");
        }
    }

    public class RestablecerPasswordDtoValidator : AbstractValidator<RestablecerPasswordDto>
    {
        public RestablecerPasswordDtoValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("El correo electrónico es obligatorio.")
                .EmailAddress().WithMessage("El formato del correo electrónico no es válido.");

            RuleFor(x => x.CodigoOtp)
                .NotEmpty().WithMessage("El código OTP es obligatorio.")
                .Length(6).WithMessage("El código OTP debe tener 6 dígitos.");

            RuleFor(x => x.NuevaPassword)
                .NotEmpty().WithMessage("La nueva contraseña es obligatoria.")
                .MinimumLength(6).WithMessage("La nueva contraseña debe tener al menos 6 caracteres.");
        }
    }
}