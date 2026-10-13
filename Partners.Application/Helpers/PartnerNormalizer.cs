using System.Text.RegularExpressions;

namespace Partners.Application.Helpers
{
    // Regras de normalização aplicadas de forma igual na criação, actualização e pesquisa.
    public static partial class PartnerNormalizer
    {
        public static string? NormalizeName(string? name) => name?.Trim();

        // Remove espaços, pontos e hífenes: "001.234.567", "001 234 567" e "001-234-567" → "001234567".
        public static string? NormalizeTaxNumber(string? taxNumber) =>
            taxNumber is null
                ? null
                : new string(taxNumber.Where(c => !char.IsWhiteSpace(c) && c != '.' && c != '-').ToArray());

        // Exactamente nove dígitos ASCII (0-9), depois da normalização.
        public static bool IsValidTaxNumber(string? normalizedTaxNumber) =>
            normalizedTaxNumber is { Length: 9 } && normalizedTaxNumber.All(char.IsAsciiDigit);

        // Strings vazias ou só com espaços passam a null; caso contrário, aplica Trim.
        public static string? NormalizeEmail(string? email) =>
            string.IsNullOrWhiteSpace(email) ? null : email.Trim();

        public static bool IsValidEmail(string email) => EmailRegex().IsMatch(email);

        // Formato prático: parte local e domínio sem espaços nem "@" extra, e domínio com pelo menos um ponto.
        [GeneratedRegex(@"^[^@\s]+@[^@\s.]+(\.[^@\s.]+)+$", RegexOptions.CultureInvariant)]
        private static partial Regex EmailRegex();
    }
}
