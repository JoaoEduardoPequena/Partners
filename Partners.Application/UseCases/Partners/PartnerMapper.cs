using Partners.Application.DTOs;
using Partners.Application.Helpers;
using Partners.Domain.Entities;
using Partners.Domain.Enums;

namespace Partners.Application.UseCases.Partners
{
    public static class PartnerMapper
    {
        public static PartnerDto ToDto(Partner entity) => new()
        {
            Id = entity.id,
            Name = entity.name,
            TaxNumber = entity.tax_number,
            Type = PartnerTypeCodes.ToCode(entity.type),
            Email = entity.email,
            CreditLimit = entity.credit_limit,
            IsActive = entity.is_active,
            Version = VersionToken.Encode(entity.version),
            CreatedAt = entity.created_at,
            UpdatedAt = entity.updated_at,
        };
    }
}
