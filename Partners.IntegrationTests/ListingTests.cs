using Partners.Application.DTOs;
using Partners.IntegrationTests.Infrastructure;
using System.Data.SqlTypes;

namespace Partners.IntegrationTests
{
    public class ListingTests(SqlServerFixture sqlServer) : PartnersApiTestBase(sqlServer)
    {
        private async Task<Dictionary<string, PartnerDto>> SeedAsync()
        {
            var partners = new Dictionary<string, PartnerDto>
            {
                ["ana"] = await CreateAsync(NewPartner("Ana Silva", "100000001", "CUSTOMER")),
                ["mariana"] = await CreateAsync(NewPartner("Mariana Costa", "100000002", "SUPPLIER")),
                ["anaPaula"] = await CreateAsync(NewPartner("ANA PAULA", "100000003", "BOTH")),
                ["bruno"] = await CreateAsync(NewPartner("Bruno Lopes", "200000001", "CUSTOMER")),
                ["carla"] = await CreateAsync(NewPartner("Carla Dias", "300000001", "SUPPLIER")),
            };
            partners["carla"] = await UpdateAsync(partners["carla"].Id, UpdateBody(partners["carla"], isActive: false));
            return partners;
        }

        private static string[] Names(IEnumerable<PartnerDto> items) => items.Select(p => p.Name).ToArray();

        // Cenário 2 — filtros por Type
        [Fact]
        public async Task TypeFilter_CustomerAndSupplierIncludeBoth_BothIsExclusive()
        {
            await CreateAsync(NewPartner("Cliente", "111111111", "CUSTOMER"));
            await CreateAsync(NewPartner("Fornecedor", "222222222", "SUPPLIER"));
            await CreateAsync(NewPartner("Ambos", "333333333", "BOTH"));

            Assert.Equal(["Ambos", "Cliente"], Names((await ListAsync("?Type=CUSTOMER")).Items));
            Assert.Equal(["Ambos", "Fornecedor"], Names((await ListAsync("?Type=SUPPLIER")).Items));
            Assert.Equal(["Ambos"], Names((await ListAsync("?Type=BOTH")).Items));
            Assert.Equal(3, (await ListAsync()).TotalCount);
        }

        // Cenário 7 — pesquisa, filtros e paginação
        [Fact]
        public async Task Search_ByName_IsPartialAndCaseInsensitive_AndDoesNotMatchEveryTaxNumber()
        {
            await SeedAsync();

            var expected = new[] { "ANA PAULA", "Ana Silva", "Mariana Costa" };
            Assert.Equal(expected, Names((await ListAsync("?Search=Ana")).Items).Order(StringComparer.OrdinalIgnoreCase));
            Assert.Equal(expected, Names((await ListAsync("?Search=aNA")).Items).Order(StringComparer.OrdinalIgnoreCase));
            Assert.Equal(3, (await ListAsync("?Search=Ana")).TotalCount);
        }

        [Theory]
        [InlineData("100000002")]
        [InlineData("100.000.002")]
        [InlineData("100 000 002")]
        [InlineData("100-000-002")]
        public async Task Search_ByTaxNumber_NormalizesSeparators(string search)
        {
            await SeedAsync();

            var result = await ListAsync($"?Search={Uri.EscapeDataString(search)}");

            Assert.Equal(["Mariana Costa"], Names(result.Items));
        }

        [Fact]
        public async Task Search_ByPartialTaxNumber()
        {
            await SeedAsync();

            var result = await ListAsync("?Search=100.000");

            Assert.Equal(3, result.TotalCount);
        }

        [Theory]
        [InlineData("...")]
        [InlineData("- -")]
        [InlineData(".")]
        public async Task Search_OnlySeparators_DoesNotMatchEverything(string search)
        {
            await SeedAsync();

            var result = await ListAsync($"?Search={Uri.EscapeDataString(search)}");

            Assert.Empty(result.Items);
            Assert.Equal(0, result.TotalCount);
        }

        [Fact]
        public async Task Search_WithLikeWildcards_IsTreatedLiterally()
        {
            await SeedAsync();

            Assert.Equal(0, (await ListAsync("?Search=%25")).TotalCount);
            Assert.Equal(0, (await ListAsync("?Search=_")).TotalCount);
        }

        [Fact]
        public async Task Filters_AreCombinedWithAnd()
        {
            await SeedAsync();

            Assert.Equal(["ANA PAULA", "Ana Silva"], Names((await ListAsync("?Search=ana&Type=CUSTOMER")).Items).Order(StringComparer.Ordinal));
            Assert.Equal(["ANA PAULA", "Mariana Costa"], Names((await ListAsync("?Search=ana&Type=SUPPLIER")).Items).Order(StringComparer.Ordinal));
            Assert.Empty((await ListAsync("?Search=Carla&Type=SUPPLIER")).Items);
            Assert.Equal(["Carla Dias"], Names((await ListAsync("?Search=Carla&Type=SUPPLIER&IncludeInactive=true")).Items));
            Assert.Empty((await ListAsync("?Search=Bruno&Type=SUPPLIER&IncludeInactive=true")).Items);
        }

        [Fact]
        public async Task Paging_TotalCountIsBeforePaging_AndPageBeyondEndIsEmpty()
        {
            await SeedAsync();

            var firstPage = await ListAsync("?Page=1&PageSize=2");
            Assert.Equal(2, firstPage.Items.Count);
            Assert.Equal(4, firstPage.TotalCount); // Carla está inactiva
            Assert.Equal(1, firstPage.Page);
            Assert.Equal(2, firstPage.PageSize);
            Assert.Equal(["ANA PAULA", "Ana Silva"], Names(firstPage.Items));

            var secondPage = await ListAsync("?Page=2&PageSize=2");
            Assert.Equal(["Bruno Lopes", "Mariana Costa"], Names(secondPage.Items));

            var beyond = await ListAsync("?Page=3&PageSize=2");
            Assert.Empty(beyond.Items);
            Assert.Equal(4, beyond.TotalCount);
            Assert.Equal(3, beyond.Page);

            var huge = await ListAsync($"?Page={int.MaxValue}&PageSize=100");
            Assert.Empty(huge.Items);
            Assert.Equal(4, huge.TotalCount);
        }

        [Fact]
        public async Task Paging_DefaultsToPage1AndSize20()
        {
            for (var i = 0; i < 25; i++)
                await CreateAsync(NewPartner($"Parceiro {i:D2}", $"5000000{i:D2}"));

            var result = await ListAsync();

            Assert.Equal(1, result.Page);
            Assert.Equal(20, result.PageSize);
            Assert.Equal(20, result.Items.Count);
            Assert.Equal(25, result.TotalCount);
        }

        [Fact]
        public async Task Ordering_SameNames_AreTieBrokenById()
        {
            var ids = new List<Guid>();
            for (var i = 0; i < 4; i++)
                ids.Add((await CreateAsync(NewPartner("Nome Repetido", $"70000000{i}"))).Id);

            // Ordem do SQL Server para uniqueidentifier (SqlGuid), não a de System.Guid.
            var expected = ids.OrderBy(id => new SqlGuid(id)).ToArray();

            var all = await ListAsync("?Search=Repetido");
            Assert.Equal(expected, all.Items.Select(p => p.Id).ToArray());

            var paged = new List<Guid>();
            for (var page = 1; page <= 4; page++)
                paged.AddRange((await ListAsync($"?Search=Repetido&PageSize=1&Page={page}")).Items.Select(p => p.Id));
            Assert.Equal(expected, paged.ToArray());
        }

        [Fact]
        public async Task IncludeInactive_DefaultsToFalse()
        {
            var partners = await SeedAsync();

            Assert.DoesNotContain((await ListAsync()).Items, p => p.Id == partners["carla"].Id);
            Assert.DoesNotContain((await ListAsync("?IncludeInactive=false")).Items, p => p.Id == partners["carla"].Id);
            Assert.Equal(5, (await ListAsync("?IncludeInactive=true")).TotalCount);
        }
    }
}
