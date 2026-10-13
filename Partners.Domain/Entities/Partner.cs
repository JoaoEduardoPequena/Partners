using Partners.Domain.Enums;

namespace Partners.Domain.Entities
{
    // Cliente, fornecedor ou ambos. A empresa tem um único registo por NIF, mesmo quando a
    // entidade desempenha os dois papéis.
    public class Partner
    {
        public const int NameMinLength = 2;
        public const int NameMaxLength = 100;
        public const int TaxNumberLength = 9;
        public const int EmailMaxLength = 254;
        public const decimal CreditLimitMin = 0m;
        public const decimal CreditLimitMax = 1_000_000m;
        public const int CreditLimitDecimalPlaces = 2;

        public Guid id { get; set; }
        public string name { get; set; } = string.Empty;

        // NIF normalizado: exactamente 9 dígitos, guardado como texto para preservar zeros à esquerda.
        public string tax_number { get; set; } = string.Empty;
        public PartnerType type { get; set; }
        public string? email { get; set; }
        public decimal credit_limit { get; set; }
        public bool is_active { get; set; }

        // rowversion do SQL Server — token de concorrência optimista, gerado pela base de dados.
        public byte[] version { get; set; } = [];

        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
    }
}
