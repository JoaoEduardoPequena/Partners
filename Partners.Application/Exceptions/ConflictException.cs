namespace Partners.Application.Exceptions
{
    // Conflito conhecido (NIF duplicado, versão desactualizada) — convertido em HTTP 409.
    public class ConflictException(string message) : Exception(message);
}
