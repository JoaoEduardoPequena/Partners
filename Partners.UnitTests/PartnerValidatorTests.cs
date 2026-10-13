using Partners.Application.UseCases.Partners.Commands.CreatePartnerCommand;
using Partners.Application.UseCases.Partners.Commands.UpdatePartnerCommand;
using Partners.Application.UseCases.Partners.Queries.GetPartnersQuery;

namespace Partners.UnitTests
{
    public class PartnerValidatorTests
    {
        private const string ValidVersion = "AAAAAAAAB9E=";

        private static CreatePartnerCommand ValidCreate() => new()
        {
            Name = " Ana Silva ",
            TaxNumber = "001.234.567",
            Type = "CUSTOMER",
            Email = " ana@example.com ",
            CreditLimit = 1000.50m,
        };

        private static UpdatePartnerCommand ValidUpdate() =>
            new(Guid.NewGuid(), "Ana Silva", "001234567", "BOTH", null, 0m, false, ValidVersion);

        private static string[] Errors(FluentValidation.Results.ValidationResult result) =>
            result.Errors.Select(e => e.PropertyName).Distinct().ToArray();

        [Fact]
        public void Create_AcceptsValidInputBeforeNormalization()
        {
            var result = new CreatePartnerCommandValidator().Validate(ValidCreate());

            Assert.True(result.IsValid, string.Join("; ", result.Errors));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(" A ")]       // 1 carácter depois do Trim
        [InlineData("Ana\u0000")] // carácter de controlo
        public void Create_RejectsInvalidName(string? name)
        {
            var result = new CreatePartnerCommandValidator().Validate(ValidCreate() with { Name = name });

            Assert.Equal(["Name"], Errors(result));
        }

        [Fact]
        public void Create_RejectsNameLongerThan100AfterTrim()
        {
            var validator = new CreatePartnerCommandValidator();

            Assert.True(validator.Validate(ValidCreate() with { Name = "  " + new string('a', 100) + "  " }).IsValid);
            Assert.Equal(["Name"], Errors(validator.Validate(ValidCreate() with { Name = new string('a', 101) })));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("12345678A")]
        [InlineData("12345678")]
        [InlineData("1234567890")]
        [InlineData("...")]
        public void Create_RejectsInvalidTaxNumber(string? taxNumber)
        {
            var result = new CreatePartnerCommandValidator().Validate(ValidCreate() with { TaxNumber = taxNumber });

            Assert.Equal(["TaxNumber"], Errors(result));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("customer")]
        [InlineData("PARTNER")]
        [InlineData("2")]
        public void Create_RejectsInvalidType(string? type)
        {
            var result = new CreatePartnerCommandValidator().Validate(ValidCreate() with { Type = type });

            Assert.Equal(["Type"], Errors(result));
        }

        [Theory]
        [InlineData("not-an-email")]
        [InlineData("ana@example")]
        public void Create_RejectsInvalidEmail(string email)
        {
            var result = new CreatePartnerCommandValidator().Validate(ValidCreate() with { Email = email });

            Assert.Equal(["Email"], Errors(result));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_AcceptsMissingOrBlankEmail(string? email)
        {
            Assert.True(new CreatePartnerCommandValidator().Validate(ValidCreate() with { Email = email }).IsValid);
        }

        [Theory]
        [InlineData("10.999")]
        [InlineData("-0.01")]
        [InlineData("1000000.01")]
        public void Create_RejectsInvalidCreditLimit(string creditLimit)
        {
            var command = ValidCreate() with { CreditLimit = decimal.Parse(creditLimit, System.Globalization.CultureInfo.InvariantCulture) };

            Assert.Equal(["CreditLimit"], Errors(new CreatePartnerCommandValidator().Validate(command)));
        }

        [Theory]
        [InlineData("0")]
        [InlineData("10.99")]
        [InlineData("10.990")] // mesma quantidade, só com um zero à direita
        [InlineData("1000000")]
        public void Create_AcceptsValidCreditLimit(string creditLimit)
        {
            var command = ValidCreate() with { CreditLimit = decimal.Parse(creditLimit, System.Globalization.CultureInfo.InvariantCulture) };

            Assert.True(new CreatePartnerCommandValidator().Validate(command).IsValid);
        }

        [Fact]
        public void Update_AcceptsValidCommand()
        {
            Assert.True(new UpdatePartnerCommandValidator().Validate(ValidUpdate()).IsValid);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("abc")]
        [InlineData("AAAA")]
        public void Update_RejectsMissingOrMalformedVersion(string? version)
        {
            var result = new UpdatePartnerCommandValidator().Validate(ValidUpdate() with { Version = version });

            Assert.Equal(["Version"], Errors(result));
        }

        [Theory]
        [InlineData(0, 20, "Page")]
        [InlineData(-1, 20, "Page")]
        [InlineData(1, 0, "PageSize")]
        [InlineData(1, 101, "PageSize")]
        public void List_RejectsInvalidPaging(int page, int pageSize, string property)
        {
            var result = new GetPartnersQueryValidator().Validate(new GetPartnersQuery { Page = page, PageSize = pageSize });

            Assert.Equal([property], Errors(result));
        }

        [Theory]
        [InlineData("customer")]
        [InlineData("ALL")]
        [InlineData("1")]
        public void List_RejectsInvalidType(string type)
        {
            var result = new GetPartnersQueryValidator().Validate(new GetPartnersQuery { Type = type });

            Assert.Equal(["Type"], Errors(result));
        }

        [Fact]
        public void List_DefaultsAreValid()
        {
            var query = new GetPartnersQuery();

            Assert.Equal(1, query.Page);
            Assert.Equal(20, query.PageSize);
            Assert.False(query.IncludeInactive);
            Assert.True(new GetPartnersQueryValidator().Validate(query).IsValid);
        }
    }
}
