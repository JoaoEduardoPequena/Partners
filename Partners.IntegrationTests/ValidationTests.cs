using Partners.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http.Json;

namespace Partners.IntegrationTests
{
    // Cenário 6 — validações e ausência de alterações
    public class ValidationTests(SqlServerFixture sqlServer) : PartnersApiTestBase(sqlServer)
    {
        public static TheoryData<string, string> InvalidCreateBodies => new()
        {
            { """{"name":"Ana","taxNumber":"001234567","type":"CUSTOMER","creditLimit":10.999}""", "CreditLimit" },
            { """{"name":"Ana","taxNumber":"001234567","type":"CUSTOMER","creditLimit":-1}""", "CreditLimit" },
            { """{"name":"Ana","taxNumber":"001234567","type":"CUSTOMER","creditLimit":1000000.01}""", "CreditLimit" },
            { """{"name":"Ana","taxNumber":"00123456A","type":"CUSTOMER","creditLimit":0}""", "TaxNumber" },
            { """{"name":"Ana","taxNumber":"0012345678","type":"CUSTOMER","creditLimit":0}""", "TaxNumber" },
            { """{"name":"Ana","taxNumber":"001234567","type":"PARTNER","creditLimit":0}""", "Type" },
            { """{"name":"Ana","taxNumber":"001234567","type":"customer","creditLimit":0}""", "Type" },
            { """{"name":"Ana","taxNumber":"001234567","type":1,"creditLimit":0}""", "$.type" },
            { """{"name":"   ","taxNumber":"001234567","type":"CUSTOMER","creditLimit":0}""", "Name" },
            { """{"name":" A ","taxNumber":"001234567","type":"CUSTOMER","creditLimit":0}""", "Name" },
            { """{"name":null,"taxNumber":"001234567","type":"CUSTOMER","creditLimit":0}""", "Name" },
            { """{"name":"Ana","taxNumber":"001234567","type":"CUSTOMER","email":"ana@","creditLimit":0}""", "Email" },
            { """{"name":"Ana","taxNumber":"001234567","type":"CUSTOMER","creditLimit":"abc"}""", "$.creditLimit" },
        };

        [Theory]
        [MemberData(nameof(InvalidCreateBodies))]
        public async Task Post_InvalidData_Returns400_AndWritesNothing(string body, string errorKey)
        {
            var response = await PostRawAsync(body);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(Json, Ct);
            Assert.Equal(400, problem!.Status);
            Assert.Contains(problem.Errors.Keys, key => key.Equals(errorKey, StringComparison.OrdinalIgnoreCase));
            Assert.Equal(0, await CountPartnersInDatabaseAsync());
        }

        [Fact]
        public async Task Put_InvalidData_Returns400_AndLeavesRowUnchanged()
        {
            var created = await CreateAsync(NewPartner());

            var invalidBodies = new object[]
            {
                UpdateBody(created, creditLimit: 10.999m),
                UpdateBody(created, creditLimit: -5m),
                UpdateBody(created, creditLimit: 1_000_001m),
                UpdateBody(created, taxNumber: "ABC123456"),
                UpdateBody(created, type: "EVERYONE"),
                UpdateBody(created, name: "x"),
                UpdateBody(created, version: "not base64!"),
                UpdateBody(created, version: "AAAA"),
                new { name = "Ana", taxNumber = created.TaxNumber, type = "CUSTOMER", email = "invalid", creditLimit = 1m, isActive = false, version = created.Version },
            };

            foreach (var body in invalidBodies)
            {
                var response = await PutAsync(created.Id, body);
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            }

            var current = await GetAsync(created.Id);
            Assert.Equivalent(created, current);
        }

        [Theory]
        [InlineData("?Page=0")]
        [InlineData("?Page=-1")]
        [InlineData("?PageSize=0")]
        [InlineData("?PageSize=101")]
        [InlineData("?Type=OTHER")]
        [InlineData("?Type=customer")]
        [InlineData("?Type=1")]
        [InlineData("?Page=abc")]
        [InlineData("?IncludeInactive=maybe")]
        public async Task List_InvalidParameters_Returns400(string queryString)
        {
            var response = await Client.GetAsync($"/api/partners{queryString}", Ct);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        }

        [Theory]
        [InlineData("?PageSize=1")]
        [InlineData("?PageSize=100")]
        [InlineData("?Type=BOTH&IncludeInactive=true&Search=x&Page=2")]
        public async Task List_BoundaryParameters_AreAccepted(string queryString)
        {
            var response = await Client.GetAsync($"/api/partners{queryString}", Ct);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        public static TheoryData<string, string, string, string> PortugueseMessages => new()
        {
            // método, URL, corpo JSON, mensagem esperada
            { "POST", "/api/partners", """{"name":"Ana","taxNumber":"001234567","type":"CUSTOMER","creditLimit":10.999}""", "O limite de crédito deve ter no máximo 2 casas decimais." },
            { "POST", "/api/partners", """{"name":"Ana","taxNumber":"ABC","type":"CUSTOMER","creditLimit":0}""", "O NIF deve ter exactamente 9 dígitos (espaços, pontos e hífenes são ignorados)." },
            { "POST", "/api/partners", """{"name":"Ana","taxNumber":"001234567","type":"X","creditLimit":0}""", "O tipo deve ser um dos seguintes valores: CUSTOMER, SUPPLIER, BOTH." },
            { "POST", "/api/partners", """{"name":"Ana","taxNumber":"001234567","type":"CUSTOMER","email":"x@","creditLimit":0}""", "O email não tem um formato válido." },
            { "POST", "/api/partners", """{"name":" ","taxNumber":"001234567","type":"CUSTOMER","creditLimit":0}""", "O nome é obrigatório." },
            { "POST", "/api/partners", """{"name":"Ana","taxNumber":"001234567","type":"CUSTOMER"}""", "Propriedades obrigatórias em falta: creditLimit." },
            { "POST", "/api/partners", """{"name":"Ana","taxNumber":"001234567","type":"CUSTOMER","creditLimit":"abc"}""", "O valor indicado para 'creditLimit' não é válido." },
            { "POST", "/api/partners", """{"name":"Ana" """, "O corpo do pedido não é um JSON válido." },
            { "POST", "/api/partners", "", "O corpo do pedido é obrigatório." },
            { "PUT", $"/api/partners/{Guid.Empty}", """{"name":"Ana","taxNumber":"001234567","type":"CUSTOMER","creditLimit":0,"isActive":true,"version":"AAAAAAAAB9E="}""", "Propriedades obrigatórias em falta: email." },
            { "PUT", $"/api/partners/{Guid.Empty}", """{"name":"Ana","taxNumber":"001234567","type":"CUSTOMER","email":null,"creditLimit":0,"isActive":true,"version":"???"}""", "A versão (Version) é inválida: deve ser o token Base64 devolvido pela API." },
            { "GET", "/api/partners?Page=0", "", "A página (Page) deve ser maior ou igual a 1." },
            { "GET", "/api/partners?PageSize=500", "", "O tamanho da página (PageSize) deve estar entre 1 e 100." },
            { "GET", "/api/partners?Page=abc", "", "O valor 'abc' não é válido para 'Page'." },
        };

        [Theory]
        [MemberData(nameof(PortugueseMessages))]
        public async Task ValidationMessages_AreInPortuguese(string method, string url, string body, string expectedMessage)
        {
            using var request = new HttpRequestMessage(new HttpMethod(method), url);
            if (method != "GET")
                request.Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");

            var response = await Client.SendAsync(request, Ct);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
            var problem = (await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(Json, Ct))!;
            Assert.Equal("Ocorreram um ou mais erros de validação.", problem.Title);
            Assert.Contains(expectedMessage, problem.Errors.Values.SelectMany(v => v));
        }

        [Fact]
        public async Task ConflictAndNotFoundMessages_AreInPortuguese()
        {
            await CreateAsync(NewPartner());

            var conflict = await PostAsync(NewPartner(name: "Outra", taxNumber: "001.234.567"));
            var conflictProblem = (await conflict.Content.ReadFromJsonAsync<ProblemDetails>(Json, Ct))!;
            Assert.Equal("Conflito.", conflictProblem.Title);
            Assert.Equal("Já existe uma entidade com este NIF.", conflictProblem.Detail);

            var id = Guid.NewGuid();
            var notFound = await Client.GetAsync($"/api/partners/{id}", Ct);
            var notFoundProblem = (await notFound.Content.ReadFromJsonAsync<ProblemDetails>(Json, Ct))!;
            Assert.Equal("Recurso não encontrado.", notFoundProblem.Title);
            Assert.Equal($"A entidade '{id}' não foi encontrada.", notFoundProblem.Detail);
        }

        [Theory]
        [InlineData("GET")]
        [InlineData("PUT")]
        public async Task MalformedId_Returns400WithProblemDetails_AndWritesNothing(string method)
        {
            var created = await CreateAsync(NewPartner());

            using var request = new HttpRequestMessage(new HttpMethod(method), "/api/partners/not-a-guid");
            if (method == "PUT")
                request.Content = JsonContent.Create(UpdateBody(created, name: "Não deve gravar"), options: Json);

            var response = await Client.SendAsync(request, Ct);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
            var problem = (await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(Json, Ct))!;
            var idErrors = Assert.Single(problem.Errors, e => e.Key.Equals("id", StringComparison.OrdinalIgnoreCase)).Value;
            Assert.Contains("O valor 'not-a-guid' não é válido.", idErrors);
            Assert.Equivalent(created, await GetAsync(created.Id));
        }

        [Fact]
        public async Task CreditLimit_IsStoredWithTwoDecimalsWithoutRounding()
        {
            var created = await CreateAsync(NewPartner(creditLimit: 1234.56m));

            Assert.Equal(1234.56m, created.CreditLimit);
            Assert.Equal(1234.56m, (await GetAsync(created.Id)).CreditLimit);
        }
    }
}
