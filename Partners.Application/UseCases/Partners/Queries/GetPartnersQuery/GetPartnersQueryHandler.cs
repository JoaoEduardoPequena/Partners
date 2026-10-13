using Partners.Application.DTOs;
using Partners.Application.Helpers;
using Partners.Application.Wrapper;
using Partners.Domain.Entities;
using Partners.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Partners.Persistence.Interfaces;

namespace Partners.Application.UseCases.Partners.Queries.GetPartnersQuery
{
    public class GetPartnersQueryHandler
        : IRequestHandler<GetPartnersQuery, PagedResponse<PartnerDto>>
    {
        private readonly IGenericRepositoryAsync<Partner> _partnerRepository;

        public GetPartnersQueryHandler(IGenericRepositoryAsync<Partner> partnerRepository)
        {
            _partnerRepository = partnerRepository;
        }

        // Filtros (AND), contagem, ordenação e paginação são todos traduzidos para SQL Server.
        public async Task<PagedResponse<PartnerDto>> Handle(GetPartnersQuery request, CancellationToken cancellationToken)
        {
            var query = (request.IncludeInactive
                    ? _partnerRepository.Where(x => true)
                    : _partnerRepository.Where(x => x.is_active))
                .AsNoTracking();

            if (request.Type is not null)
            {
                var type = PartnerTypeCodes.Parse(request.Type);
                query = type switch
                {
                    PartnerType.Customer => query.Where(x => x.type == PartnerType.Customer || x.type == PartnerType.Both),
                    PartnerType.Supplier => query.Where(x => x.type == PartnerType.Supplier || x.type == PartnerType.Both),
                    _ => query.Where(x => x.type == PartnerType.Both),
                };
            }

            var search = request.Search?.Trim();
            if (!string.IsNullOrEmpty(search))
            {
                // O ramo do NIF só entra quando o termo, depois de normalizado, é uma sequência
                // não vazia de dígitos — "Ana" ou "..." nunca fazem todos os NIF corresponder.
                var taxNumberSearch = PartnerNormalizer.NormalizeTaxNumber(search)!;
                var searchesTaxNumber = taxNumberSearch.Length > 0 && taxNumberSearch.All(char.IsAsciiDigit);

                // Contains é traduzido para LIKE parametrizado (com escape dos wildcards); a coluna
                // name usa uma collation case-insensitive (ver PartnerConfiguration).
                query = searchesTaxNumber
                    ? query.Where(x => x.name.Contains(search) || x.tax_number.Contains(taxNumberSearch))
                    : query.Where(x => x.name.Contains(search));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var skip = ((long)request.Page - 1) * request.PageSize;
            List<Partner> partners = skip >= totalCount
                ? []
                : await query
                    .OrderBy(x => x.name)
                    .ThenBy(x => x.id)
                    .Skip((int)skip)
                    .Take(request.PageSize)
                    .ToListAsync(cancellationToken);

            return new PagedResponse<PartnerDto>
            {
                Items = partners.Select(PartnerMapper.ToDto).ToList(),
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize,
            };
        }
    }
}
