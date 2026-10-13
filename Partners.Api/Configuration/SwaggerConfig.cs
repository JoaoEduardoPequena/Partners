using Microsoft.OpenApi.Models;

namespace Partners.Api.Configuration
{
    public static class SwaggerConfig
    {
        public static void ConfigureSwagger(this IServiceCollection services)
        {
            services.AddSwaggerGen(d =>
            {
                d.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title   = "Partners API - Clientes e Fornecedores",
                    Version = "v1",
                    Description =
                        "API REST para gerir os clientes e fornecedores (partners) de uma empresa.",
                });

                // Lê os comentários XML (/// <summary>, <remarks>, <response>) gerados a partir do
                // código dos controllers e junta-os à documentação do Swagger (título/descrição de
                // cada endpoint). Requer <GenerateDocumentationFile>true</GenerateDocumentationFile>
                // no Partners.Api.csproj — sem isso este ficheiro não existe e a chamada falha em runtime.
                var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                    d.IncludeXmlComments(xmlPath);
            });
        }
    }
}
