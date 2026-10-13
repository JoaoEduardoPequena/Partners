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

namespace Partners.Application.UseCases.Partners.Commands.CreatePartnerCommand
{
    public class CreatePartnerCommandHandler
        : IRequestHandler<CreatePartnerCommand, PartnerDto>
    {
        private readonly IGenericRepositoryAsync<Partner> _partnerRepository;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<CreatePartnerCommandHandler> _logger;

        public CreatePartnerCommandHandler(
            IGenericRepositoryAsync<Partner> partnerRepository,
            TimeProvider timeProvider,
            ILogger<CreatePartnerCommandHandler> logger)
        {
            _partnerRepository = partnerRepository;
            _timeProvider = timeProvider;
            _logger = logger;
        }

        // O pedido já chega validado (ValidationBehavior).
        public async Task<PartnerDto> Handle(CreatePartnerCommand request, CancellationToken cancellationToken)
        {
            var now = _timeProvider.GetUtcNow().UtcDateTime;

            var partner = new Partner
            {
                name = PartnerNormalizer.NormalizeName(request.Name)!,
                tax_number = PartnerNormalizer.NormalizeTaxNumber(request.TaxNumber)!,
                type = PartnerTypeCodes.Parse(request.Type!),
                email = PartnerNormalizer.NormalizeEmail(request.Email),
                credit_limit = request.CreditLimit,
                is_active = true,
                created_at = now,
                updated_at = now,
            };

            // Sem consulta prévia: o índice único da base de dados é a garantia, e cobre também
            // dois pedidos simultâneos com o mesmo NIF.
            try
            {
                await _partnerRepository.AddAsync(partner, cancellationToken);
            }
            catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
            {
                _logger.LogWarning("Partner creation rejected: tax number already registered.");
                throw new ConflictException("Já existe uma entidade com este NIF.");
            }

            _logger.LogInformation("Partner {PartnerId} created.", partner.id);
            return PartnerMapper.ToDto(partner);
        }
    }
}
