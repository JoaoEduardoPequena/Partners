using Partners.Application.DTOs;
using MediatR;

namespace Partners.Application.UseCases.Partners.Commands.UpdatePartnerCommand
{
    public record UpdatePartnerCommand(
        Guid Id,
        string? Name,
        string? TaxNumber,
        string? Type,
        string? Email,
        decimal CreditLimit,
        bool IsActive,
        string? Version
    ) : IRequest<PartnerDto>
    {
        public static UpdatePartnerCommand From(Guid id, UpdatePartnerRequest request) => new(
            id,
            request.Name,
            request.TaxNumber,
            request.Type,
            request.Email,
            request.CreditLimit,
            request.IsActive,
            request.Version);
    }
}
