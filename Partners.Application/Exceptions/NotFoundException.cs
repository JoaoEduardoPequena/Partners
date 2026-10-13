namespace Partners.Application.Exceptions
{
    // Recurso inexistente — convertido em HTTP 404 pelo GlobalExceptionHandler.
    public class NotFoundException(string message) : Exception(message);
}
