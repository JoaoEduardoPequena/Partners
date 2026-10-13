using Partners.Application.TransactionBehavior;
using Partners.Application.ValidationBehavior;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Partners.Persistence.Interfaces;
using Partners.Persistence.Repositories;
using System.Globalization;
using System.Reflection;

namespace Partners.Application
{
    public static class ApplicationServiceRegistration
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Mensagens em português também para regras sem WithMessage (tradução incluída no FluentValidation).
            ValidatorOptions.Global.LanguageManager.Culture = new CultureInfo("pt");
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
            services.AddMediatR(config =>
            {
                config.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
                // Ordem importa: valida primeiro (falha rápido, sem tocar na BD), só depois abre a transação.
                config.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
                config.AddBehavior(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
            });

            services.AddScoped(typeof(IGenericRepositoryAsync<>), typeof(GenericRepositoryAsync<>));

            // Relógio do servidor (UTC) para CreatedAt/UpdatedAt — substituível nos testes.
            services.TryAddSingleton(TimeProvider.System);

            return services;
        }
    }
}
