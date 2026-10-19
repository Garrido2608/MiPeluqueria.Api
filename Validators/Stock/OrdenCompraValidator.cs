using FluentValidation;
using MiPeluqueria.Api.DTOs.Stock;

namespace MiPeluqueria.Api.Validators.Stock
{
    public class CrearOrdenCompraDtoValidator : AbstractValidator<CrearOrdenCompraDto>
    {
        public CrearOrdenCompraDtoValidator()
        {
            RuleFor(x => x.ProveedorId)
                .GreaterThan(0).WithMessage("El proveedor es obligatorio.");

            RuleFor(x => x.Detalles)
                .NotEmpty().WithMessage("La orden de compra debe incluir al menos un producto.");

            RuleForEach(x => x.Detalles).ChildRules(detalle =>
            {
                detalle.RuleFor(d => d.ProductoId)
                    .GreaterThan(0).WithMessage("ID de producto inválido.");

                detalle.RuleFor(d => d.CantidadPedida)
                    .GreaterThan(0).WithMessage("La cantidad pedida debe ser mayor a 0.");

                detalle.RuleFor(d => d.PrecioUnitarioCosto)
                    .GreaterThan(0).WithMessage("El costo unitario debe ser mayor a 0.");
            });
        }
    }
}