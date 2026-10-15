using FluentValidation;
using MiPeluqueria.Api.DTOs.Clientes;

namespace MiPeluqueria.Api.Validators.Clientes
{
    public class CrearFichaTecnicaDtoValidator : AbstractValidator<CrearFichaTecnicaDto>
    {
        public CrearFichaTecnicaDtoValidator()
        {
            RuleFor(x => x.ClienteId)
                .GreaterThan(0).WithMessage("Debe especificar un cliente válido.");

            RuleFor(x => x.FormulaAplicada)
                .NotEmpty().WithMessage("La fórmula o trabajo técnico aplicado es obligatorio.")
                .MaximumLength(1000).WithMessage("La fórmula aplicada no puede superar los 1000 caracteres.");

            RuleFor(x => x.Observaciones)
                .MaximumLength(1000).WithMessage("Las observaciones no pueden superar los 1000 caracteres.");

            RuleFor(x => x.ProductosUtilizados)
                .MaximumLength(500).WithMessage("El detalle de productos utilizados no puede superar los 500 caracteres.");
        }
    }
}