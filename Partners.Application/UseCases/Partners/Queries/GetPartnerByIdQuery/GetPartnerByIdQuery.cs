using Partners.Application.DTOs;
using MediatR;

namespace Partners.Application.UseCases.Partners.Queries.GetPartnerByIdQuery
{
    public record GetPartnerByIdQuery(Guid Id) : IRequest<PartnerDto>;
}
