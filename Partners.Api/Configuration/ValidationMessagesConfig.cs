using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Partners.Api.Configuration
{
    // Mensagens em português para os erros detectados pelo ASP.NET Core antes dos validadores
    // (JSON malformado, propriedades obrigatórias em falta, tipos errados, parâmetros de query).
    // As regras de negócio (FluentValidation) já têm mensagens próprias em português.
    public static partial class ValidationMessagesConfig
    {
        public const string ValidationTitle = "Ocorreram um ou mais erros de validação.";

        public static IMvcBuilder AddPortugueseValidationMessages(this IMvcBuilder builder)
        {
            builder.AddMvcOptions(options =>
            {
                // Sem isto, um corpo inválido gera também "The command field is required." (em inglês).
                options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;

                var messages = options.ModelBindingMessageProvider;
                messages.SetMissingBindRequiredValueAccessor(name => $"Não foi indicado um valor para '{name}'.");
                messages.SetMissingKeyOrValueAccessor(() => "É obrigatório indicar um valor.");
                messages.SetMissingRequestBodyRequiredValueAccessor(() => "O corpo do pedido é obrigatório.");
                messages.SetValueMustNotBeNullAccessor(value => $"O valor '{value}' não é válido.");
                messages.SetAttemptedValueIsInvalidAccessor((value, name) => $"O valor '{value}' não é válido para '{name}'.");
                messages.SetNonPropertyAttemptedValueIsInvalidAccessor(value => $"O valor '{value}' não é válido.");
                messages.SetUnknownValueIsInvalidAccessor(name => $"O valor indicado não é válido para '{name}'.");
                messages.SetNonPropertyUnknownValueIsInvalidAccessor(() => "O valor indicado não é válido.");
                messages.SetValueIsInvalidAccessor(value => $"O valor '{value}' não é válido.");
                messages.SetValueMustBeANumberAccessor(name => $"O campo '{name}' tem de ser um número.");
                messages.SetNonPropertyValueMustBeANumberAccessor(() => "O campo tem de ser um número.");
            });

            // A mensagem original do System.Text.Json (em inglês, com detalhes internos) não é exposta;
            // a excepção fica no ModelState e é traduzida em CreateValidationResponse.
            builder.AddJsonOptions(options => options.AllowInputFormatterExceptionMessages = false);

            builder.ConfigureApiBehaviorOptions(options =>
                options.InvalidModelStateResponseFactory = CreateValidationResponse);

            return builder;
        }

        private static IActionResult CreateValidationResponse(ActionContext context)
        {
            var problemDetailsFactory = context.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();
            var problemDetails = problemDetailsFactory.CreateValidationProblemDetails(
                context.HttpContext, new ModelStateDictionary(), StatusCodes.Status400BadRequest, ValidationTitle);
            problemDetails.Instance = context.HttpContext.Request.Path;

            foreach (var (key, entry) in context.ModelState)
            {
                if (entry.Errors.Count == 0)
                    continue;

                problemDetails.Errors[key] = entry.Errors
                    .Select(error => Translate(key, error))
                    .Distinct()
                    .ToArray();
            }

            return new BadRequestObjectResult(problemDetails)
            {
                ContentTypes = { "application/problem+json" },
            };
        }

        private static string Translate(string key, ModelError error)
        {
            if (!string.IsNullOrEmpty(error.ErrorMessage))
                return error.ErrorMessage;

            if (error.Exception is JsonException jsonException)
            {
                var missing = MissingRequiredPropertiesRegex().Match(jsonException.Message);
                if (missing.Success)
                    return $"Propriedades obrigatórias em falta: {missing.Groups["names"].Value.Replace("'", string.Empty)}.";

                var field = key.StartsWith("$.", StringComparison.Ordinal) ? key[2..] : key;
                return string.IsNullOrEmpty(field) || field == "$"
                    ? "O corpo do pedido não é um JSON válido."
                    : $"O valor indicado para '{field}' não é válido.";
            }

            return "O valor indicado não é válido.";
        }

        // Ex.: "JSON deserialization for type 'X' was missing required properties including: 'email', 'version'."
        [GeneratedRegex(@"missing required properties(?: including)?: (?<names>.+?)\.?$", RegexOptions.CultureInvariant)]
        private static partial Regex MissingRequiredPropertiesRegex();
    }
}
