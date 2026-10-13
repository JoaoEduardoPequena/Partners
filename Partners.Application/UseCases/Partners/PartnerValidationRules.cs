using Partners.Application.Helpers;
using Partners.Domain.Entities;
using Partners.Domain.Enums;
using FluentValidation;

namespace Partners.Application.UseCases.Partners
{
    // Regras partilhadas pelos validadores de criação e actualização. Recebem os valores já
    // normalizados (via PartnerNormalizer), para validar exactamente o que vai ser gravado.
    public static class PartnerValidationRules
    {
        public static IRuleBuilderOptions<T, string?> PartnerName<T>(this IRuleBuilder<T, string?> rule) =>
            rule
                .NotEmpty().WithMessage("O nome é obrigatório.")
                .Length(Partner.NameMinLength, Partner.NameMaxLength)
                    .WithMessage($"O nome deve ter entre {Partner.NameMinLength} e {Partner.NameMaxLength} caracteres.")
                .Must(v => v is null || !v.Any(char.IsControl)).WithMessage("O nome contém caracteres inválidos.");

        public static IRuleBuilderOptions<T, string?> PartnerTaxNumber<T>(this IRuleBuilder<T, string?> rule) =>
            rule
                .NotEmpty().WithMessage("O NIF é obrigatório.")
                .Must(PartnerNormalizer.IsValidTaxNumber)
                    .WithMessage("O NIF deve ter exactamente 9 dígitos (espaços, pontos e hífenes são ignorados).");

        public static IRuleBuilderOptions<T, string?> PartnerType<T>(this IRuleBuilder<T, string?> rule) =>
            rule
                .NotEmpty().WithMessage("O tipo é obrigatório.")
                .Must(v => PartnerTypeCodes.TryParse(v, out _))
                    .WithMessage($"O tipo deve ser um dos seguintes valores: {string.Join(", ", PartnerTypeCodes.All)}.");

        public static IRuleBuilderOptions<T, string?> PartnerEmail<T>(this IRuleBuilder<T, string?> rule) =>
            rule
                .MaximumLength(Partner.EmailMaxLength).WithMessage($"O email deve ter no máximo {Partner.EmailMaxLength} caracteres.")
                .Must(v => v is null || PartnerNormalizer.IsValidEmail(v)).WithMessage("O email não tem um formato válido.");

        public static IRuleBuilderOptions<T, decimal> PartnerCreditLimit<T>(this IRuleBuilder<T, decimal> rule) =>
            rule
                .InclusiveBetween(Partner.CreditLimitMin, Partner.CreditLimitMax)
                    .WithMessage($"O limite de crédito deve estar entre {Partner.CreditLimitMin} e {Partner.CreditLimitMax:0}.")
                // Sem arredondamento silencioso: 10.999 é rejeitado em vez de gravado como 11.00.
                .Must(v => decimal.Round(v, Partner.CreditLimitDecimalPlaces) == v)
                    .WithMessage($"O limite de crédito deve ter no máximo {Partner.CreditLimitDecimalPlaces} casas decimais.");
    }
}
