namespace Partners.Domain.Enums
{
    public enum PartnerType
    {
        Customer = 1,
        Supplier = 2,
        Both = 3
    }

    // Códigos usados no contrato HTTP e na coluna "type" da base de dados.
    // A conversão é exacta (ordinal, sensível a maiúsculas) — não aceita números nem variações.
    public static class PartnerTypeCodes
    {
        public const string Customer = "CUSTOMER";
        public const string Supplier = "SUPPLIER";
        public const string Both = "BOTH";

        public static readonly IReadOnlyList<string> All = [Customer, Supplier, Both];

        public static string ToCode(PartnerType type) => type switch
        {
            PartnerType.Customer => Customer,
            PartnerType.Supplier => Supplier,
            PartnerType.Both => Both,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown partner type."),
        };

        public static bool TryParse(string? code, out PartnerType type)
        {
            switch (code)
            {
                case Customer: type = PartnerType.Customer; return true;
                case Supplier: type = PartnerType.Supplier; return true;
                case Both: type = PartnerType.Both; return true;
                default: type = default; return false;
            }
        }

        public static PartnerType Parse(string code) =>
            TryParse(code, out var type)
                ? type
                : throw new ArgumentException($"Unknown partner type code '{code}'.", nameof(code));
    }
}
