using Partners.Domain.Entities;
using Partners.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Partners.Persistence.Configuration
{
    public class PartnerConfiguration : IEntityTypeConfiguration<Partner>
    {
        // Collation explícita: a pesquisa por nome não diferencia maiúsculas de minúsculas,
        // independentemente da collation por defeito da base de dados.
        public const string NameCollation = "Latin1_General_100_CI_AS";

        public void Configure(EntityTypeBuilder<Partner> builder)
        {
            builder.ToTable("partner", t =>
            {
                // Defesa em profundidade: a API já valida, mas a base de dados também rejeita
                // valores fora das regras, mesmo que escritos por outra via.
                t.HasCheckConstraint("ck_partner_type", "[type] IN ('CUSTOMER', 'SUPPLIER', 'BOTH')");
                t.HasCheckConstraint("ck_partner_tax_number", "LEN([tax_number]) = 9 AND [tax_number] NOT LIKE '%[^0-9]%'");
                t.HasCheckConstraint("ck_partner_credit_limit", "[credit_limit] >= 0 AND [credit_limit] <= 1000000");
            });

            builder.HasKey(x => x.id);

            // Guid gerado pelo servidor da API no Add (SequentialGuidValueGenerator do EF Core,
            // amigável para o índice clustered do SQL Server).
            builder.Property(x => x.id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd()
                .IsRequired();

            builder.Property(x => x.name)
                .HasColumnName("name")
                .HasMaxLength(Partner.NameMaxLength)
                .UseCollation(NameCollation)
                .IsRequired();

            builder.Property(x => x.tax_number)
                .HasColumnName("tax_number")
                .HasMaxLength(Partner.TaxNumberLength)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(x => x.type)
                .HasColumnName("type")
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasConversion(v => PartnerTypeCodes.ToCode(v), v => PartnerTypeCodes.Parse(v))
                .IsRequired();

            builder.Property(x => x.email)
                .HasColumnName("email")
                .HasMaxLength(Partner.EmailMaxLength);

            builder.Property(x => x.credit_limit)
                .HasColumnName("credit_limit")
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(x => x.is_active)
                .HasColumnName("is_active")
                .IsRequired();

            builder.Property(x => x.version)
                .HasColumnName("version")
                .IsRowVersion();

            // datetime2 não guarda o Kind — ao ler, marca-se como UTC (é sempre gravado em UTC).
            builder.Property(x => x.created_at)
                .HasColumnName("created_at")
                .HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
                .IsRequired();

            builder.Property(x => x.updated_at)
                .HasColumnName("updated_at")
                .HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
                .IsRequired();

            // A unicidade do NIF é garantida aqui, pela base de dados — inclui registos inactivos.
            builder.HasIndex(x => x.tax_number)
                .HasDatabaseName("idx_partner_tax_number")
                .IsUnique();

            // Suporta a ordenação determinística da listagem (name, id).
            builder.HasIndex(x => new { x.name, x.id })
                .HasDatabaseName("idx_partner_name_id");
        }
    }
}
