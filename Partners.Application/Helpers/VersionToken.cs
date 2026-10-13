namespace Partners.Application.Helpers
{
    // A coluna rowversion do SQL Server (8 bytes) é exposta na API como um token opaco em Base64.
    public static class VersionToken
    {
        // Tamanho fixo de um rowversion do SQL Server.
        public const int RowVersionLength = 8;

        // Converte o rowversion lido da base de dados no texto Base64 devolvido ao cliente
        // no campo "version" (ex.: [0,0,0,0,0,0,7,209] → "AAAAAAAAB9E=").
        public static string Encode(byte[] rowVersion) => Convert.ToBase64String(rowVersion);

        // Converte a "version" enviada pelo cliente no PUT de volta para os 8 bytes do rowversion.
        // Devolve false (e rowVersion vazio) se o token estiver vazio, não for Base64 válido ou
        // não corresponder a exactamente 8 bytes — o validador transforma isso num 400.
        public static bool TryDecode(string? token, out byte[] rowVersion)
        {
            rowVersion = [];
            if (string.IsNullOrWhiteSpace(token))
                return false;

            // O buffer tem só 8 bytes: um token mais longo também falha aqui.
            var buffer = new byte[RowVersionLength];
            if (!Convert.TryFromBase64String(token, buffer, out var written) || written != RowVersionLength)
                return false;

            rowVersion = buffer;
            return true;
        }
    }
}
