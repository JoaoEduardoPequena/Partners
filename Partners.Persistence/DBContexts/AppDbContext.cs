using Partners.Domain.Entities;
using Microsoft.EntityFrameworkCore;
namespace Partners.Persistence.DBContexts
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(
        DbContextOptions<AppDbContext> options)
            : base(options) { }

        // Clientes e fornecedores
        public DbSet<Partner> Partners { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(
                typeof(AppDbContext).Assembly);
            base.OnModelCreating(modelBuilder);
        }
    }
}
