using FluentValidation;
using MiPeluqueria.Api.DTOs.Ventas;

namespace MiPeluqueria.Api.Validators.Ventas
{
    public class AperturaCajaDtoValidator : AbstractValidator<AperturaCajaDto>
    {
        public AperturaCajaDtoValidator()
        {
            RuleFor(x => x.UsuarioId)
                .GreaterThan(0).WithMessage("El usuario de apertura es obligatorio.");

            RuleFor(x => x.MontoInicial)
                .GreaterThanOrEqualTo(0).WithMessage("El fondo inicial de caja no puede ser negativo.");
        }
    }

    public class CierreCajaDtoValidator : AbstractValidator<CierreCajaDto>
    {
        public CierreCajaDtoValidator()
        {
            RuleFor(x => x.UsuarioId)
                .GreaterThan(0).WithMessage("El usuario de cierre es obligatorio.");

            RuleFor(x => x.MontoFinalReal)
                .GreaterThanOrEqualTo(0).WithMessage("El monto físico declarado no puede ser negativo.");
        }
    }

    public class CrearVentaDtoValidator : AbstractValidator<CrearVentaDto>
    {
        public CrearVentaDtoValidator()
        {
            RuleFor(x => x.SesionCajaId)
                .GreaterThan(0).WithMessage("Debe especificar la sesión de caja activa.");

            RuleFor(x => x.Detalles)
                .NotEmpty().WithMessage("La venta debe incluir al menos un servicio o producto.");

            RuleForEach(x => x.Detalles).ChildRules(detalle =>
            {
                detalle.RuleFor(d => d.TipoItem)
                    .IsInEnum().WithMessage("El tipo de ítem debe ser Servicio (1) o Producto (2).");

                detalle.RuleFor(d => d.ItemId)
                    .GreaterThan(0).WithMessage("El ID del servicio o producto debe ser mayor a cero.");

                detalle.RuleFor(d => d.Cantidad)
                    .GreaterThan(0).WithMessage("La cantidad de cada ítem debe ser al menos 1.");
            });

            RuleFor(x => x.Pagos)
                .NotEmpty().WithMessage("Debe registrar al menos un medio de pago.");

            RuleForEach(x => x.Pagos).ChildRules(pago =>
            {
                pago.RuleFor(p => p.MedioPagoId)
                    .GreaterThan(0).WithMessage("Debe indicar un medio de pago válido.");

                pago.RuleFor(p => p.Importe)
                    .GreaterThan(0).WithMessage("El importe de cada pago debe ser mayor a cero.");
            });
        }
    }
}