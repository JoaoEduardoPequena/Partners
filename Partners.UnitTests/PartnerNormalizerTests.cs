using Partners.Application.Helpers;
using Partners.Domain.Enums;

namespace Partners.UnitTests
{
    public class PartnerNormalizerTests
    {
        [Theory]
        [InlineData("001.234.567")]
        [InlineData("001 234 567")]
        [InlineData("001-234-567")]
        [InlineData(" 001234567 ")]
        [InlineData("0 0 1.2-3 4.5 6-7")]
        public void NormalizeTaxNumber_RemovesSpacesDotsAndHyphens(string input)
        {
            var normalized = PartnerNormalizer.NormalizeTaxNumber(input);

            Assert.Equal("001234567", normalized);
            Assert.True(PartnerNormalizer.IsValidTaxNumber(normalized));
        }

        [Theory]
        [InlineData("00123456")]       // 8 dígitos
        [InlineData("0012345678")]     // 10 dígitos
        [InlineData("00123456A")]      // letra
        [InlineData("001/234/567")]    // separador não suportado
        [InlineData("００１２３４５６７")] // dígitos Unicode não ASCII
        [InlineData("")]
        public void IsValidTaxNumber_RejectsAnythingButNineAsciiDigits(string input)
        {
            Assert.False(PartnerNormalizer.IsValidTaxNumber(PartnerNormalizer.NormalizeTaxNumber(input)));
        }

        [Fact]
        public void NormalizeName_Trims()
        {
            Assert.Equal("Ana Silva", PartnerNormalizer.NormalizeName("  Ana Silva \t"));
            Assert.Null(PartnerNormalizer.NormalizeName(null));
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("", null)]
        [InlineData("   ", null)]
        [InlineData("  ana@example.com ", "ana@example.com")]
        public void NormalizeEmail_TurnsBlankIntoNullAndTrims(string? input, string? expected)
        {
            Assert.Equal(expected, PartnerNormalizer.NormalizeEmail(input));
        }

        [Theory]
        [InlineData("ana@example.com", true)]
        [InlineData("ana.silva+tag@sub.example.co.ao", true)]
        [InlineData("ana", false)]
        [InlineData("ana@", false)]
        [InlineData("ana@example", false)]
        [InlineData("ana@@example.com", false)]
        [InlineData("ana silva@example.com", false)]
        [InlineData("ana@example..com", false)]
        public void IsValidEmail(string email, bool expected)
        {
            Assert.Equal(expected, PartnerNormalizer.IsValidEmail(email));
        }

        [Theory]
        [InlineData("CUSTOMER", PartnerType.Customer)]
        [InlineData("SUPPLIER", PartnerType.Supplier)]
        [InlineData("BOTH", PartnerType.Both)]
        public void PartnerTypeCodes_ParsesExactCodes(string code, PartnerType expected)
        {
            Assert.True(PartnerTypeCodes.TryParse(code, out var type));
            Assert.Equal(expected, type);
            Assert.Equal(code, PartnerTypeCodes.ToCode(type));
        }

        [Theory]
        [InlineData("customer")]
        [InlineData("Customer")]
        [InlineData(" CUSTOMER")]
        [InlineData("1")]
        [InlineData("CUSTOMER,SUPPLIER")]
        [InlineData("")]
        [InlineData(null)]
        public void PartnerTypeCodes_RejectsPermissiveConversions(string? code)
        {
            Assert.False(PartnerTypeCodes.TryParse(code, out _));
        }
    }
}
