using Partners.Application.DTOs;
using Partners.Application.Wrapper;
using MediatR;

namespace Partners.Application.UseCases.Partners.Queries.GetPartnersQuery
{
    // Parâmetros de query do GET /api/partners.
    public record GetPartnersQuery : IRequest<PagedResponse<PartnerDto>>
    {
        public const int DefaultPageSize = 20;
        public const int MaxPageSize = 100;
        public const int SearchMaxLength = 100;

        // Parte do nome (sem distinguir maiúsculas/minúsculas) ou do NIF (com ou sem separadores).
        public string? Search { get; init; }

        // CUSTOMER (inclui BOTH), SUPPLIER (inclui BOTH) ou BOTH (só BOTH).
        public string? Type { get; init; }
        public bool IncludeInactive { get; init; }
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = DefaultPageSize;
    }
}
