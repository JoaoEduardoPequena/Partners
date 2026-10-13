namespace Partners.Application.DTOs
{
    public class PartnerDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string TaxNumber { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? Email { get; set; }
        public decimal CreditLimit { get; set; }
        public bool IsActive { get; set; }

        // Token opaco (rowversion em Base64) a enviar no PUT seguinte.
        public string Version { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
