using Partners.Domain.Enums;
using FluentValidation;

namespace Partners.Application.UseCases.Partners.Queries.GetPartnersQuery
{
    public class GetPartnersQueryValidator : AbstractValidator<GetPartnersQuery>
    {
        public GetPartnersQueryValidator()
        {
            RuleFor(x => x.Page)
                .GreaterThanOrEqualTo(1).WithMessage("A página (Page) deve ser maior ou igual a 1.");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, GetPartnersQuery.MaxPageSize)
                    .WithMessage($"O tamanho da página (PageSize) deve estar entre 1 e {GetPartnersQuery.MaxPageSize}.");

            RuleFor(x => x.Type)
                .Must(v => PartnerTypeCodes.TryParse(v, out _))
                    .WithMessage($"O tipo deve ser um dos seguintes valores: {string.Join(", ", PartnerTypeCodes.All)}.")
                .When(x => x.Type is not null);

            RuleFor(x => x.Search)
                .MaximumLength(GetPartnersQuery.SearchMaxLength)
                    .WithMessage($"A pesquisa (Search) deve ter no máximo {GetPartnersQuery.SearchMaxLength} caracteres.");
        }
    }
}
