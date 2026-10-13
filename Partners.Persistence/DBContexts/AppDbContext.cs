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
        /*protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var con = "Data Source=IP;Initial Catalog=Partners;Integrated Security=False;user id=user;password=senha;MultipleActiveResultSets=true;TrustServerCertificate=True";
            optionsBuilder.UseSqlServer(con);
        }*/
    }
}
