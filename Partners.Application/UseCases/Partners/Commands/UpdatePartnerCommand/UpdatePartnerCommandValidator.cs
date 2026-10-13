using Partners.Application.Helpers;
using FluentValidation;

namespace Partners.Application.UseCases.Partners.Commands.UpdatePartnerCommand
{
    public class UpdatePartnerCommandValidator : AbstractValidator<UpdatePartnerCommand>
    {
        public UpdatePartnerCommandValidator()
        {
            RuleFor(x => PartnerNormalizer.NormalizeName(x.Name)).PartnerName().OverridePropertyName(nameof(UpdatePartnerCommand.Name));
            RuleFor(x => PartnerNormalizer.NormalizeTaxNumber(x.TaxNumber)).PartnerTaxNumber().OverridePropertyName(nameof(UpdatePartnerCommand.TaxNumber));
            RuleFor(x => x.Type).PartnerType();
            RuleFor(x => PartnerNormalizer.NormalizeEmail(x.Email)).PartnerEmail().OverridePropertyName(nameof(UpdatePartnerCommand.Email));
            RuleFor(x => x.CreditLimit).PartnerCreditLimit();

            RuleFor(x => x.Version)
                .NotEmpty().WithMessage("A versão (Version) é obrigatória.")
                .Must(v => VersionToken.TryDecode(v, out _))
                    .WithMessage("A versão (Version) é inválida: deve ser o token Base64 devolvido pela API.");
        }
    }
}
