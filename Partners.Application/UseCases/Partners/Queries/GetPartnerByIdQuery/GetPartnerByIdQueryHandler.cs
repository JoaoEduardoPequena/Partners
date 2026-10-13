using Partners.Application.DTOs;
using Partners.Application.Exceptions;
using Partners.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Partners.Persistence.Interfaces;

namespace Partners.Application.UseCases.Partners.Queries.GetPartnerByIdQuery
{
    public class GetPartnerByIdQueryHandler
        : IRequestHandler<GetPartnerByIdQuery, PartnerDto>
    {
        private readonly IGenericRepositoryAsync<Partner> _partnerRepository;

        public GetPartnerByIdQueryHandler(IGenericRepositoryAsync<Partner> partnerRepository)
        {
            _partnerRepository = partnerRepository;
        }

        // Devolve também entidades inactivas.
        public async Task<PartnerDto> Handle(GetPartnerByIdQuery request, CancellationToken cancellationToken)
        {
            var partner = await _partnerRepository
                .Where(x => x.id == request.Id)
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundException($"A entidade '{request.Id}' não foi encontrada.");

            return PartnerMapper.ToDto(partner);
        }
    }
}
