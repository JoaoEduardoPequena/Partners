using Partners.Api.Configuration;
using Partners.Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Partners.Api.Exceptions
{
    // Converte excepções em ProblemDetails (RFC 9457), sem expor stack traces, SQL ou connection strings.
    public class GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger,
        IProblemDetailsService problemDetailsService) : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            ProblemDetails problemDetails;

            switch (exception)
            {
                case FluentValidation.ValidationException validationException:
                    var errors = validationException.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray());
                    problemDetails = new ValidationProblemDetails(errors)
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = ValidationMessagesConfig.ValidationTitle,
                    };
                    logger.LogInformation("Validation failed for {Path}: {@Errors}", httpContext.Request.Path, errors);
                    break;

                case NotFoundException:
                    problemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status404NotFound,
                        Title = "Recurso não encontrado.",
                        Detail = exception.Message,
                    };
                    break;

                case ConflictException:
                    problemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status409Conflict,
                        Title = "Conflito.",
                        Detail = exception.Message,
                    };
                    break;

                default:
                    logger.LogError(exception, "Unhandled exception for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
                    problemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status500InternalServerError,
                        Title = "Ocorreu um erro inesperado.",
                    };
                    break;
            }

            problemDetails.Instance = httpContext.Request.Path;
            httpContext.Response.StatusCode = problemDetails.Status!.Value;

            return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = problemDetails,
                Exception = exception,
            });
        }
    }
}
