using Partners.Application.DTOs;
using Partners.Application.Exceptions;
using Partners.Application.Helpers;
using Partners.Domain.Entities;
using Partners.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Partners.Persistence.Extensions;
using Partners.Persistence.Interfaces;

namespace Partners.Application.UseCases.Partners.Commands.UpdatePartnerCommand
{
    public class UpdatePartnerCommandHandler
        : IRequestHandler<UpdatePartnerCommand, PartnerDto>
    {
        private readonly IGenericRepositoryAsync<Partner> _partnerRepository;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<UpdatePartnerCommandHandler> _logger;

        public UpdatePartnerCommandHandler(
            IGenericRepositoryAsync<Partner> partnerRepository,
            TimeProvider timeProvider,
            ILogger<UpdatePartnerCommandHandler> logger)
        {
            _partnerRepository = partnerRepository;
            _timeProvider = timeProvider;
            _logger = logger;
        }

        // O pedido já chega validado (ValidationBehavior), incluindo o formato da Version.
        public async Task<PartnerDto> Handle(UpdatePartnerCommand request, CancellationToken cancellationToken)
        {
            // Lido sem tracking: a entidade é depois anexada pelo UpdateAsync com a Version enviada
            // pelo cliente como valor original. O EF Core gera então
            // "UPDATE ... WHERE id = @id AND version = @versaoDoCliente" — a verificação de
            // concorrência acontece no próprio UPDATE, no SQL Server, e não em C#.
            var partner = await _partnerRepository
                .Where(x => x.id == request.Id)
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundException($"A entidade '{request.Id}' não foi encontrada.");

            VersionToken.TryDecode(request.Version, out var expectedVersion);

            partner.name = PartnerNormalizer.NormalizeName(request.Name)!;
            partner.tax_number = PartnerNormalizer.NormalizeTaxNumber(request.TaxNumber)!;
            partner.type = PartnerTypeCodes.Parse(request.Type!);
            partner.email = PartnerNormalizer.NormalizeEmail(request.Email);
            partner.credit_limit = request.CreditLimit;
            partner.is_active = request.IsActive;
            partner.updated_at = _timeProvider.GetUtcNow().UtcDateTime;
            partner.version = expectedVersion;
            // created_at mantém o valor lido da base de dados.

            try
            {
                await _partnerRepository.UpdateAsync(partner, cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                _logger.LogWarning("Partner {PartnerId} update rejected: stale version.", request.Id);
                throw new ConflictException(
                    "A entidade foi alterada por outro pedido. Obtenha-a novamente e repita o pedido com a versão (Version) actual.");
            }
            catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
            {
                _logger.LogWarning("Partner {PartnerId} update rejected: tax number belongs to another partner.", request.Id);
                throw new ConflictException("Já existe uma entidade com este NIF.");
            }

            _logger.LogInformation("Partner {PartnerId} updated.", partner.id);
            return PartnerMapper.ToDto(partner);
        }
    }
}
