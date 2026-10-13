using Partners.Application.DTOs;
using Partners.Domain.Entities;
using Partners.Domain.Enums;
using Partners.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Partners.Persistence.Extensions;
using System.Net;
using System.Net.Http.Json;

namespace Partners.IntegrationTests
{
    // Cenário 5 — concorrência real contra SQL Server.
    public class ConcurrencyTests(SqlServerFixture sqlServer) : PartnersApiTestBase(sqlServer)
    {
        private const int Rounds = 5;

        // Liberta todos os pedidos ao mesmo tempo (cada um no seu scope/DbContext/ligação da API).
        private static async Task<HttpResponseMessage[]> RunSimultaneouslyAsync(params Func<Task<HttpResponseMessage>>[] requests)
        {
            var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var tasks = requests.Select(async request =>
            {
                await start.Task;
                return await request();
            }).ToArray();

            start.SetResult();
            return await Task.WhenAll(tasks);
        }

        [Fact]
        public async Task ConcurrentCreates_WithSameNormalizedTaxNumber_OnlyOneSucceeds()
        {
            for (var round = 0; round < Rounds; round++)
            {
                var digits = $"55500000{round}";
                var formatted = $"{digits[..3]}.{digits[3..6]}.{digits[6..]}";

                var responses = await RunSimultaneouslyAsync(
                    () => PostAsync(NewPartner($"Pedido A {round}", digits)),
                    () => PostAsync(NewPartner($"Pedido B {round}", formatted)));

                var statuses = responses.Select(r => r.StatusCode).Order().ToArray();
                Assert.Equal([HttpStatusCode.Created, HttpStatusCode.Conflict], statuses);

                await using var context = CreateDbContext();
                Assert.Equal(1, await context.Partners.CountAsync(p => p.tax_number == digits, Ct));
            }
        }

        [Fact]
        public async Task ConcurrentUpdates_WithSameVersion_OnlyOneSucceeds_AndNoUpdateIsLost()
        {
            for (var round = 0; round < Rounds; round++)
            {
                var created = await CreateAsync(NewPartner($"Original {round}", $"66600000{round}"));

                var responses = await RunSimultaneouslyAsync(
                    () => PutAsync(created.Id, UpdateBody(created, name: $"Escrita A {round}", creditLimit: 1m)),
                    () => PutAsync(created.Id, UpdateBody(created, name: $"Escrita B {round}", creditLimit: 2m)));

                Assert.Equal([HttpStatusCode.OK, HttpStatusCode.Conflict], responses.Select(r => r.StatusCode).Order().ToArray());

                var winner = (await responses.Single(r => r.StatusCode == HttpStatusCode.OK)
                    .Content.ReadFromJsonAsync<PartnerDto>(Json, Ct))!;
                var current = await GetAsync(created.Id);

                // O estado final é exactamente o da escrita confirmada — nada misturado nem sobrescrito.
                Assert.Equal(winner.Name, current.Name);
                Assert.Equal(winner.CreditLimit, current.CreditLimit);
                Assert.Equal(winner.Version, current.Version);
                Assert.NotEqual(created.Version, current.Version);
            }
        }

        // Força a sobreposição: a primeira transacção grava e mantém o lock; a segunda fica
        // bloqueada no SQL Server até a primeira confirmar, e só então falha pelo índice único.
        [Fact]
        public async Task IndependentDbContexts_OverlappingInserts_SecondFailsOnUniqueIndex()
        {
            await using var first = CreateDbContext();
            await using var second = CreateDbContext();

            await using var transaction = await first.Database.BeginTransactionAsync(Ct);
            first.Partners.Add(NewEntity("Primeiro", "777777777"));
            await first.SaveChangesAsync(Ct);

            second.Partners.Add(NewEntity("Segundo", "777777777"));
            var blockedInsert = second.SaveChangesAsync(Ct);

            await Task.Delay(500, Ct);
            Assert.False(blockedInsert.IsCompleted, "The second insert should be blocked by the first transaction.");

            await transaction.CommitAsync(Ct);

            var exception = await Assert.ThrowsAsync<DbUpdateException>(() => blockedInsert);
            Assert.True(exception.IsUniqueConstraintViolation());
            Assert.Equal(1, await CountPartnersInDatabaseAsync());
        }

        [Fact]
        public async Task IndependentDbContexts_OverlappingUpdates_SecondFailsOnRowVersion()
        {
            var created = await CreateAsync(NewPartner("Original", "888888888"));

            await using var first = CreateDbContext();
            await using var second = CreateDbContext();
            var firstCopy = await first.Partners.SingleAsync(p => p.id == created.Id, Ct);
            var secondCopy = await second.Partners.SingleAsync(p => p.id == created.Id, Ct);
            Assert.Equal(firstCopy.version, secondCopy.version);

            await using var transaction = await first.Database.BeginTransactionAsync(Ct);
            firstCopy.name = "Primeira escrita";
            await first.SaveChangesAsync(Ct);

            secondCopy.name = "Segunda escrita";
            var blockedUpdate = second.SaveChangesAsync(Ct);

            await Task.Delay(500, Ct);
            Assert.False(blockedUpdate.IsCompleted, "The second update should be blocked by the first transaction.");

            await transaction.CommitAsync(Ct);

            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => blockedUpdate);
            Assert.Equal("Primeira escrita", (await GetAsync(created.Id)).Name);
        }

        private static Partner NewEntity(string name, string taxNumber) => new()
        {
            name = name,
            tax_number = taxNumber,
            type = PartnerType.Customer,
            credit_limit = 0m,
            is_active = true,
            created_at = DateTime.UtcNow,
            updated_at = DateTime.UtcNow,
        };
    }
}
