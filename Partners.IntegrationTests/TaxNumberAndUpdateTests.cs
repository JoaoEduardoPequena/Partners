using Partners.Application.DTOs;
using Partners.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;

namespace Partners.IntegrationTests
{
    public class TaxNumberAndUpdateTests(SqlServerFixture sqlServer) : PartnersApiTestBase(sqlServer)
    {
        // Cenário 1 — normalização e unicidade do NIF
        [Fact]
        public async Task Create_NormalizesNameAndTaxNumber_AndKeepsTaxNumberReservedWhenInactive()
        {
            var created = await CreateAsync(NewPartner(name: " Ana Silva ", taxNumber: "001.234.567"));

            Assert.Equal("Ana Silva", created.Name);
            Assert.Equal("001234567", created.TaxNumber);

            // Guardado como texto (varchar), com o zero à esquerda preservado.
            await using (var connection = new SqlConnection(ConnectionString))
            {
                await connection.OpenAsync(Ct);
                await using var typeCommand = new SqlCommand(
                    "SELECT DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'partner' AND COLUMN_NAME = 'tax_number'",
                    connection);
                Assert.Equal("varchar", await typeCommand.ExecuteScalarAsync(Ct));

                await using var valueCommand = new SqlCommand("SELECT tax_number, name FROM partner WHERE id = @id", connection);
                valueCommand.Parameters.AddWithValue("@id", created.Id);
                await using var reader = await valueCommand.ExecuteReaderAsync(Ct);
                Assert.True(await reader.ReadAsync(Ct));
                Assert.Equal("001234567", reader.GetString(0));
                Assert.Equal("Ana Silva", reader.GetString(1));
            }

            var duplicate = await PostAsync(NewPartner(name: "Outra Ana", taxNumber: "001 234 567"));
            Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
            var problem = await duplicate.Content.ReadFromJsonAsync<ProblemDetails>(Json, Ct);
            Assert.Equal(409, problem!.Status);

            await UpdateAsync(created.Id, UpdateBody(created, isActive: false));

            var afterDeactivation = await PostAsync(NewPartner(name: "Mais uma Ana", taxNumber: "001-234-567"));
            Assert.Equal(HttpStatusCode.Conflict, afterDeactivation.StatusCode);
            Assert.Equal(1, await CountPartnersInDatabaseAsync());
        }

        // Cenário 3 — desactivação e reactivação
        [Fact]
        public async Task DeactivateAndReactivate_ControlsDefaultListing()
        {
            var partner = await CreateAsync(NewPartner(name: "Bruno Lopes", taxNumber: "123456789"));
            await CreateAsync(NewPartner(name: "Carla Dias", taxNumber: "223456789"));

            var deactivated = await UpdateAsync(partner.Id, UpdateBody(partner, isActive: false));
            Assert.False(deactivated.IsActive);

            var defaultList = await ListAsync();
            Assert.DoesNotContain(defaultList.Items, p => p.Id == partner.Id);
            Assert.Equal(1, defaultList.TotalCount);

            var withInactive = await ListAsync("?IncludeInactive=true");
            Assert.Contains(withInactive.Items, p => p.Id == partner.Id && !p.IsActive);
            Assert.Equal(2, withInactive.TotalCount);

            Assert.False((await GetAsync(partner.Id)).IsActive);

            var reactivated = await UpdateAsync(partner.Id, UpdateBody(deactivated, isActive: true));
            Assert.True(reactivated.IsActive);
            Assert.Contains((await ListAsync()).Items, p => p.Id == partner.Id);
        }

        // Cenário 4 — alteração do NIF
        [Fact]
        public async Task Update_KeepsOwnTaxNumber_ButRejectsTaxNumberOfAnotherPartner()
        {
            var first = await CreateAsync(NewPartner(name: "Primeira", taxNumber: "111111111"));
            var second = await CreateAsync(NewPartner(name: "Segunda", taxNumber: "222222222"));

            // Mantém o próprio NIF (enviado com separadores) e altera o nome.
            var updated = await UpdateAsync(first.Id, UpdateBody(first, name: "Primeira Lda", taxNumber: "111.111.111"));
            Assert.Equal("Primeira Lda", updated.Name);
            Assert.Equal("111111111", updated.TaxNumber);

            var conflict = await PutAsync(first.Id, UpdateBody(updated, name: "Não deve gravar", taxNumber: "222 222 222", creditLimit: 5m));
            Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);

            var unchanged = await GetAsync(first.Id);
            Assert.Equal("Primeira Lda", unchanged.Name);
            Assert.Equal("111111111", unchanged.TaxNumber);
            Assert.Equal(updated.CreditLimit, unchanged.CreditLimit);
            Assert.Equal(updated.Version, unchanged.Version);
            Assert.Equal("222222222", (await GetAsync(second.Id)).TaxNumber);
        }

        [Fact]
        public async Task Update_PreservesCreatedAt_SetsUpdatedAtAndNewVersion()
        {
            var created = await CreateAsync(NewPartner());
            Assert.Equal(created.CreatedAt, created.UpdatedAt);
            Assert.Equal(DateTimeKind.Utc, created.CreatedAt.Kind);
            Assert.InRange(created.CreatedAt, DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow.AddMinutes(1));

            await Task.Delay(20, Ct);

            // Datas e Id enviados pelo cliente são ignorados.
            var body = new
            {
                name = "Ana Silva Actualizada",
                taxNumber = created.TaxNumber,
                type = "BOTH",
                email = "  ana@example.com  ",
                creditLimit = 2500.75m,
                isActive = true,
                version = created.Version,
                id = Guid.NewGuid(),
                createdAt = "2000-01-01T00:00:00Z",
                updatedAt = "2000-01-01T00:00:00Z",
            };
            var updated = await UpdateAsync(created.Id, body);

            Assert.Equal(created.Id, updated.Id);
            Assert.Equal("ana@example.com", updated.Email);
            Assert.Equal("BOTH", updated.Type);
            Assert.Equal(2500.75m, updated.CreditLimit);
            Assert.Equal(created.CreatedAt, updated.CreatedAt);
            Assert.True(updated.UpdatedAt > created.UpdatedAt);
            Assert.NotEqual(created.Version, updated.Version);
            Assert.Equal(8, Convert.FromBase64String(updated.Version).Length);

            var reloaded = await GetAsync(created.Id);
            Assert.Equal(created.CreatedAt, reloaded.CreatedAt);
            Assert.Equal(updated.UpdatedAt, reloaded.UpdatedAt);
            Assert.Equal(updated.Version, reloaded.Version);
        }

        [Fact]
        public async Task Update_WithStaleVersion_Returns409AndKeepsLatestData()
        {
            var created = await CreateAsync(NewPartner());
            var latest = await UpdateAsync(created.Id, UpdateBody(created, name: "Versão 2"));

            var stale = await PutAsync(created.Id, UpdateBody(created, name: "Versão antiga"));

            Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
            var current = await GetAsync(created.Id);
            Assert.Equal("Versão 2", current.Name);
            Assert.Equal(latest.Version, current.Version);
        }

        [Fact]
        public async Task Post_IgnoresServerControlledFields_AndReturnsLocation()
        {
            var clientId = Guid.NewGuid();
            var response = await PostRawAsync($$"""
                {"id":"{{clientId}}","name":"Ana","taxNumber":"987654321","type":"SUPPLIER","creditLimit":0,
                 "isActive":false,"createdAt":"2000-01-01T00:00:00Z","updatedAt":"2000-01-01T00:00:00Z","version":"AAAAAAAAAAE="}
                """);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var created = (await response.Content.ReadFromJsonAsync<PartnerDto>(Json, Ct))!;
            Assert.NotEqual(clientId, created.Id);
            Assert.True(created.IsActive);
            Assert.Null(created.Email);
            Assert.True(created.CreatedAt > new DateTime(2020, 1, 1));
            Assert.NotEqual("AAAAAAAAAAE=", created.Version);
            Assert.Equal(8, Convert.FromBase64String(created.Version).Length);

            Assert.NotNull(response.Headers.Location);
            Assert.EndsWith($"/api/partners/{created.Id}", response.Headers.Location!.ToString());
            var fromLocation = await Client.GetFromJsonAsync<PartnerDto>(response.Headers.Location, Json, Ct);
            Assert.Equivalent(created, fromLocation);
        }

        [Fact]
        public async Task GetById_UnknownId_Returns404()
        {
            var response = await Client.GetAsync($"/api/partners/{Guid.NewGuid()}", Ct);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        }

        [Fact]
        public async Task Put_UnknownId_Returns404()
        {
            var response = await PutAsync(Guid.NewGuid(), new
            {
                name = "Ana", taxNumber = "001234567", type = "CUSTOMER", email = (string?)null,
                creditLimit = 0m, isActive = true, version = "AAAAAAAAB9E=",
            });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal(0, await CountPartnersInDatabaseAsync());
        }

        [Fact]
        public async Task Put_EmailMustBePresentButMayBeNull()
        {
            var created = await CreateAsync(NewPartner(email: "ana@example.com"));

            var missingEmail = await PutRawAsync(created.Id, $$"""
                {"name":"Ana","taxNumber":"{{created.TaxNumber}}","type":"CUSTOMER","creditLimit":1,"isActive":true,"version":"{{created.Version}}"}
                """);
            Assert.Equal(HttpStatusCode.BadRequest, missingEmail.StatusCode);
            Assert.Equal("ana@example.com", (await GetAsync(created.Id)).Email);

            var nullEmail = await PutRawAsync(created.Id, $$"""
                {"name":"Ana","taxNumber":"{{created.TaxNumber}}","type":"CUSTOMER","email":null,"creditLimit":1,"isActive":true,"version":"{{created.Version}}"}
                """);
            Assert.Equal(HttpStatusCode.OK, nullEmail.StatusCode);
            Assert.Null((await GetAsync(created.Id)).Email);
        }

        [Fact]
        public async Task Put_MissingVersion_Returns400()
        {
            var created = await CreateAsync(NewPartner());

            var response = await PutRawAsync(created.Id, $$"""
                {"name":"Ana","taxNumber":"{{created.TaxNumber}}","type":"CUSTOMER","email":null,"creditLimit":1,"isActive":true}
                """);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            await using var context = CreateDbContext();
            Assert.Equal(1000m, (await context.Partners.SingleAsync(Ct)).credit_limit);
        }
    }
}
