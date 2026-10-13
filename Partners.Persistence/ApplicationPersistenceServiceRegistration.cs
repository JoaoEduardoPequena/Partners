using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Partners.Persistence.DBContexts;
using Microsoft.EntityFrameworkCore;

namespace Partners.Persistence
{
    public static class ApplicationPersistenceServiceRegistration
    {
        public static void AddPersistenceServices(this IServiceCollection services, IConfiguration config)
        {
            // Lida de appsettings ou da variável de ambiente ConnectionStrings__Connection.
            var connectionString = config.GetConnectionString("Connection");
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException(
                    "Connection string 'ConnectionStrings:Connection' is not configured. " +
                    "Set it in appsettings, user-secrets or the ConnectionStrings__Connection environment variable.");

            // Sem EnableRetryOnFailure: o TransactionBehavior abre transações explícitas, que não são
            // compatíveis com a estratégia de retry do EF Core.
            services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
        }
    }
}
