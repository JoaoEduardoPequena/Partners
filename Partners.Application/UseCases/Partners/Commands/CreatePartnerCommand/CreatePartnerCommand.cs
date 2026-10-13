using Partners.Application.DTOs;
using MediatR;

namespace Partners.Application.UseCases.Partners.Commands.CreatePartnerCommand
{
    // Corpo do POST /api/partners. Id, IsActive, Version e datas são definidos pelo servidor;
    // se o cliente os enviar, são ignorados.
    public record CreatePartnerCommand : IRequest<PartnerDto>
    {
        public required string? Name { get; init; }
        public required string? TaxNumber { get; init; }
        public required string? Type { get; init; }
        public string? Email { get; init; }
        public required decimal CreditLimit { get; init; }
    }
}
