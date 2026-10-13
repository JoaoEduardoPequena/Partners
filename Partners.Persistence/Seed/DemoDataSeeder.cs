using Partners.Domain.Entities;
using Partners.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Partners.Persistence.DBContexts;

namespace Partners.Persistence.Seed
{
    // Dados fictícios para desenvolvimento. Só corre quando chamado explicitamente (ver Program.cs:
    // ambiente Development + DemoData:SeedOnStartup=true) e só se a tabela estiver vazia.
    public static class DemoDataSeeder
    {
        private static readonly (string Name, string TaxNumber, PartnerType Type, string? Email, decimal CreditLimit, bool IsActive)[] Partners =
        [
            ("Ana Silva", "001234567", PartnerType.Customer, "ana.silva@example.com", 5000m, true),
            ("Ana Silva", "001234568", PartnerType.Supplier, "ana.silva.lda@example.com", 12000m, true),
            ("Mariana Costa", "100000001", PartnerType.Customer, "mariana.costa@example.com", 2500.50m, true),
            ("Luana Ferreira", "100000002", PartnerType.Both, null, 0m, true),
            ("Distribuidora Atlântico", "200000001", PartnerType.Supplier, "geral@atlantico.example.com", 250000m, true),
            ("Distribuidora Atlântico", "200000002", PartnerType.Customer, "compras@atlantico.example.com", 50000m, false),
            ("Construções Kwanza", "300000001", PartnerType.Both, "obras@kwanza.example.com", 750000m, true),
            ("Mercearia Central", "300000002", PartnerType.Customer, "central@example.com", 1500m, true),
            ("Transportes Benguela", "400000001", PartnerType.Supplier, "frota@benguela.example.com", 80000m, true),
            ("Farmácia Saúde+", "400000002", PartnerType.Customer, null, 3000m, false),
            ("Tecnologia Huíla", "500000001", PartnerType.Both, "info@huila.example.com", 120000m, true),
            ("Gráfica Moderna", "500000002", PartnerType.Supplier, "orcamentos@grafica.example.com", 20000m, true),
            ("Restaurante Mar Azul", "600000001", PartnerType.Customer, "reservas@marazul.example.com", 8000m, true),
            ("Papelaria Escolar", "600000002", PartnerType.Supplier, null, 4000m, false),
            ("Clínica Bem-Estar", "700000001", PartnerType.Customer, "admin@bemestar.example.com", 15000m, true),
            ("Seguros Horizonte", "700000002", PartnerType.Both, "parcerias@horizonte.example.com", 1000000m, true),
            ("João Pedro", "800000001", PartnerType.Customer, "joao.pedro@example.com", 999.99m, true),
            ("Joana Pedro", "800000002", PartnerType.Customer, "joana.pedro@example.com", 1200m, true),
            ("Agro Cuanza", "900000001", PartnerType.Supplier, "vendas@agrocuanza.example.com", 60000m, true),
            ("Energia Solar Sul", "900000002", PartnerType.Supplier, "suporte@solarsul.example.com", 90000m, true),
            ("Hotel Miramar", "012345678", PartnerType.Customer, "rececao@miramar.example.com", 30000m, true),
            ("Oficina Auto Rápido", "023456789", PartnerType.Both, null, 7000m, false),
            ("Livraria Saber", "034567890", PartnerType.Customer, "loja@saber.example.com", 2000m, true),
            ("Importadora Lobito", "045678901", PartnerType.Supplier, "import@lobito.example.com", 400000m, true),
            ("Padaria Pão Quente", "056789012", PartnerType.Customer, "encomendas@paoquente.example.com", 500m, true),
        ];

        public static async Task SeedAsync(AppDbContext context, ILogger logger, CancellationToken cancellationToken = default)
        {
            if (await context.Partners.AnyAsync(cancellationToken))
            {
                logger.LogInformation("Demo data skipped: table partner already has data.");
                return;
            }

            var now = DateTime.UtcNow;
            context.Partners.AddRange(Partners.Select(p => new Partner
            {
                name = p.Name,
                tax_number = p.TaxNumber,
                type = p.Type,
                email = p.Email,
                credit_limit = p.CreditLimit,
                is_active = p.IsActive,
                created_at = now,
                updated_at = now,
            }));

            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Demo data seeded: {Count} partners.", Partners.Length);
        }
    }
}
