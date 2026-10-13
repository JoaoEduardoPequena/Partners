using Partners.Application.DTOs;
using Partners.Application.Wrapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Partners.Persistence.DBContexts;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Partners.IntegrationTests.Infrastructure
{
    // Cada teste: base de dados nova, criada pela migração real (MigrateAsync), e uma instância da
    // API em memória (WebApplicationFactory) ligada a ela. No fim, a base de dados é apagada.
    public abstract class PartnersApiTestBase(SqlServerFixture sqlServer) : IAsyncLifetime
    {
        protected static readonly JsonSerializerOptions Json = JsonSerializerOptions.Web;

        private WebApplicationFactory<Program> _factory = null!;

        protected string ConnectionString { get; private set; } = string.Empty;
        protected HttpClient Client { get; private set; } = null!;
        protected static CancellationToken Ct => TestContext.Current.CancellationToken;

        public async ValueTask InitializeAsync()
        {
            ConnectionString = sqlServer.CreateDatabaseConnectionString($"partners_test_{Guid.NewGuid():N}");

            await using (var context = CreateDbContext())
                await context.Database.MigrateAsync();

            _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.UseSetting("ConnectionStrings:Connection", ConnectionString);
            });
            Client = _factory.CreateClient();
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await _factory.DisposeAsync();

            SqlConnection.ClearAllPools();
            await using var context = CreateDbContext();
            await context.Database.EnsureDeletedAsync();
        }

        // DbContext independente da API (ligação própria), para verificar o estado real da BD.
        protected AppDbContext CreateDbContext() =>
            new(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(ConnectionString).Options);

        protected static object NewPartner(
            string name = "Ana Silva",
            string taxNumber = "001234567",
            string type = "CUSTOMER",
            string? email = null,
            decimal creditLimit = 1000m) =>
            new { name, taxNumber, type, email, creditLimit };

        protected static object UpdateBody(PartnerDto current, string? name = null, string? taxNumber = null,
            string? type = null, decimal? creditLimit = null, bool? isActive = null, string? version = null) =>
            new
            {
                name = name ?? current.Name,
                taxNumber = taxNumber ?? current.TaxNumber,
                type = type ?? current.Type,
                email = current.Email,
                creditLimit = creditLimit ?? current.CreditLimit,
                isActive = isActive ?? current.IsActive,
                version = version ?? current.Version,
            };

        protected Task<HttpResponseMessage> PostAsync(object body) =>
            Client.PostAsJsonAsync("/api/partners", body, Json, Ct);

        protected Task<HttpResponseMessage> PostRawAsync(string json) =>
            Client.PostAsync("/api/partners", new StringContent(json, Encoding.UTF8, "application/json"), Ct);

        protected Task<HttpResponseMessage> PutAsync(Guid id, object body) =>
            Client.PutAsJsonAsync($"/api/partners/{id}", body, Json, Ct);

        protected Task<HttpResponseMessage> PutRawAsync(Guid id, string json) =>
            Client.PutAsync($"/api/partners/{id}", new StringContent(json, Encoding.UTF8, "application/json"), Ct);

        protected async Task<PartnerDto> CreateAsync(object body)
        {
            var response = await PostAsync(body);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            return (await response.Content.ReadFromJsonAsync<PartnerDto>(Json, Ct))!;
        }

        protected async Task<PartnerDto> GetAsync(Guid id)
        {
            var response = await Client.GetAsync($"/api/partners/{id}", Ct);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return (await response.Content.ReadFromJsonAsync<PartnerDto>(Json, Ct))!;
        }

        protected async Task<PartnerDto> UpdateAsync(Guid id, object body)
        {
            var response = await PutAsync(id, body);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return (await response.Content.ReadFromJsonAsync<PartnerDto>(Json, Ct))!;
        }

        protected async Task<PagedResponse<PartnerDto>> ListAsync(string queryString = "")
        {
            var response = await Client.GetAsync($"/api/partners{queryString}", Ct);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return (await response.Content.ReadFromJsonAsync<PagedResponse<PartnerDto>>(Json, Ct))!;
        }

        protected async Task<int> CountPartnersInDatabaseAsync()
        {
            await using var context = CreateDbContext();
            return await context.Partners.CountAsync(Ct);
        }
    }
}
