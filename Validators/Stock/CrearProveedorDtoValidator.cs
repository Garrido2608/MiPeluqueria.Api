using FluentValidation;
using MiPeluqueria.Api.DTOs.Stock;

namespace MiPeluqueria.Api.Validators.Stock
{
    public class CrearProveedorDtoValidator : AbstractValidator<CrearProveedorDto>
    {
        public CrearProveedorDtoValidator()
        {
            RuleFor(x => x.RazonSocial)
                .NotEmpty().WithMessage("La razón social del proveedor es obligatoria.")
                .MaximumLength(150).WithMessage("La razón social no puede superar los 150 caracteres.");

            RuleFor(x => x.Contacto)
                .MaximumLength(100).WithMessage("El nombre de contacto no puede superar los 100 caracteres.");

            RuleFor(x => x.Telefono)
                .MaximumLength(30).WithMessage("El teléfono no puede superar los 30 caracteres.");

            RuleFor(x => x.Email)
                .EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email))
                .WithMessage("El formato del correo electrónico del proveedor no es válido.")
                .MaximumLength(120).WithMessage("El correo electrónico no puede superar los 120 caracteres.");
        }
    }
}