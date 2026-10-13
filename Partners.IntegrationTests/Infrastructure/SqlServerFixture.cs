using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

[assembly: AssemblyFixture(typeof(Partners.IntegrationTests.Infrastructure.SqlServerFixture))]

namespace Partners.IntegrationTests.Infrastructure
{
    // Um SQL Server real por execução de testes:
    //  - por defeito, um container Docker (Testcontainers);
    //  - se INTEGRATION_TESTS_SQLSERVER estiver definida, usa esse servidor (ex.: LocalDB ou
    //    uma instância local) — útil em ambientes sem Docker.
    // Cada teste cria a sua própria base de dados (ver PartnersApiTestBase).
    public sealed class SqlServerFixture : IAsyncLifetime
    {
        public const string ConnectionStringVariable = "INTEGRATION_TESTS_SQLSERVER";
        public const string Image = "mcr.microsoft.com/mssql/server:2022-latest";

        private MsSqlContainer? _container;
        private string _serverConnectionString = string.Empty;

        public async ValueTask InitializeAsync()
        {
            var external = Environment.GetEnvironmentVariable(ConnectionStringVariable);
            if (!string.IsNullOrWhiteSpace(external))
            {
                _serverConnectionString = external;
                return;
            }

            _container = new MsSqlBuilder(Image).Build();
            await _container.StartAsync();
            _serverConnectionString = _container.GetConnectionString();
        }

        public string CreateDatabaseConnectionString(string databaseName) =>
            new SqlConnectionStringBuilder(_serverConnectionString)
            {
                InitialCatalog = databaseName,
                TrustServerCertificate = true,
            }.ConnectionString;

        public async ValueTask DisposeAsync()
        {
            if (_container is not null)
                await _container.DisposeAsync();
        }
    }
}
