namespace Partners.Application.DTOs
{
    // Corpo do PUT /api/partners/{id}. Todas as propriedades têm de estar presentes no JSON
    // ("required"); Email pode ter o valor null. Id, datas e outras propriedades não fazem parte
    // do contrato e são ignoradas.
    public record UpdatePartnerRequest
    {
        public required string? Name { get; init; }
        public required string? TaxNumber { get; init; }
        public required string? Type { get; init; }
        public required string? Email { get; init; }
        public required decimal CreditLimit { get; init; }
        public required bool IsActive { get; init; }
        public required string? Version { get; init; }
    }
}
