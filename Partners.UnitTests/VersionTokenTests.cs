using Partners.Application.Helpers;

namespace Partners.UnitTests
{
    public class VersionTokenTests
    {
        [Fact]
        public void EncodeAndDecode_RoundTrip()
        {
            byte[] rowVersion = [0, 0, 0, 0, 0, 0, 0x07, 0xD1];

            var token = VersionToken.Encode(rowVersion);

            Assert.Equal("AAAAAAAAB9E=", token);
            Assert.True(VersionToken.TryDecode(token, out var decoded));
            Assert.Equal(rowVersion, decoded);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("not-base64!")]
        [InlineData("AAAA")]              // Base64 válido, mas só 3 bytes
        [InlineData("AAAAAAAAAAAAAA==")]  // Base64 válido, mas 10 bytes
        public void TryDecode_RejectsMalformedTokens(string? token)
        {
            Assert.False(VersionToken.TryDecode(token, out _));
        }
    }
}
