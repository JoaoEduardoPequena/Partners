using Partners.Application.DTOs;
using Partners.Application.UseCases.Partners.Commands.CreatePartnerCommand;
using System.Text.Json;

namespace Partners.UnitTests
{
    // Mesmas opções que o ASP.NET Core usa por defeito (camelCase, case-insensitive).
    public class ContractSerializationTests
    {
        private static readonly JsonSerializerOptions Options = JsonSerializerOptions.Web;

        private const string FullUpdateBody = """
            {"name":"Ana","taxNumber":"001234567","type":"BOTH","email":null,"creditLimit":10.5,"isActive":true,"version":"AAAAAAAAB9E="}
            """;

        [Fact]
        public void UpdateRequest_AcceptsExplicitNullEmail()
        {
            var request = JsonSerializer.Deserialize<UpdatePartnerRequest>(FullUpdateBody, Options)!;

            Assert.Null(request.Email);
            Assert.Equal(10.5m, request.CreditLimit);
            Assert.Equal("AAAAAAAAB9E=", request.Version);
        }

        [Theory]
        [InlineData("email")]
        [InlineData("version")]
        [InlineData("isActive")]
        [InlineData("creditLimit")]
        [InlineData("name")]
        public void UpdateRequest_RejectsMissingProperty(string property)
        {
            var node = System.Text.Json.Nodes.JsonNode.Parse(FullUpdateBody)!.AsObject();
            node.Remove(property);

            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<UpdatePartnerRequest>(node.ToJsonString(), Options));
        }

        [Fact]
        public void CreateCommand_EmailIsOptional_CreditLimitIsRequired()
        {
            var command = JsonSerializer.Deserialize<CreatePartnerCommand>(
                """{"name":"Ana","taxNumber":"001234567","type":"CUSTOMER","creditLimit":0}""", Options)!;
            Assert.Null(command.Email);

            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<CreatePartnerCommand>(
                """{"name":"Ana","taxNumber":"001234567","type":"CUSTOMER"}""", Options));
        }

        [Fact]
        public void PartnerDto_RoundTrips()
        {
            var dto = new PartnerDto
            {
                Id = Guid.NewGuid(),
                Name = "Ana Silva",
                TaxNumber = "001234567",
                Type = "CUSTOMER",
                Email = null,
                CreditLimit = 1234.56m,
                IsActive = true,
                Version = "AAAAAAAAB9E=",
                CreatedAt = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 1, 2, 3, 4, 6, DateTimeKind.Utc),
            };

            var json = JsonSerializer.Serialize(dto, Options);
            var back = JsonSerializer.Deserialize<PartnerDto>(json, Options)!;

            Assert.Contains("\"taxNumber\":\"001234567\"", json);
            Assert.Contains("\"createdAt\":\"2026-01-02T03:04:05Z\"", json);
            Assert.Equivalent(dto, back);
        }
    }
}
