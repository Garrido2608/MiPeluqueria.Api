using FluentValidation;
using MiPeluqueria.Api.DTOs.Common;

namespace MiPeluqueria.Api.Validators.Common
{
    public class PaginationFilterDtoValidator : AbstractValidator<PaginationFilterDto>
    {
        public PaginationFilterDtoValidator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThanOrEqualTo(1)
                .WithMessage("El número de página debe ser mayor o igual a 1.");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100)
                .WithMessage("El tamaño de página debe estar entre 1 y 100 registros.");

            RuleFor(x => x.Search)
                .MaximumLength(100)
                .When(x => !string.IsNullOrEmpty(x.Search));
        }
    }
}