using Partners.Application.Helpers;
using FluentValidation;

namespace Partners.Application.UseCases.Partners.Commands.CreatePartnerCommand
{
    public class CreatePartnerCommandValidator : AbstractValidator<CreatePartnerCommand>
    {
        public CreatePartnerCommandValidator()
        {
            RuleFor(x => PartnerNormalizer.NormalizeName(x.Name)).PartnerName().OverridePropertyName(nameof(CreatePartnerCommand.Name));
            RuleFor(x => PartnerNormalizer.NormalizeTaxNumber(x.TaxNumber)).PartnerTaxNumber().OverridePropertyName(nameof(CreatePartnerCommand.TaxNumber));
            RuleFor(x => x.Type).PartnerType();
            RuleFor(x => PartnerNormalizer.NormalizeEmail(x.Email)).PartnerEmail().OverridePropertyName(nameof(CreatePartnerCommand.Email));
            RuleFor(x => x.CreditLimit).PartnerCreditLimit();
        }
    }
}
