using Partners.Application;
using Microsoft.Extensions.Logging.Console;
using Partners.Persistence;
using Partners.Persistence.DBContexts;
using Partners.Persistence.Seed;
using Scalar.AspNetCore;
using System.Text.Json.Serialization;
using Partners.Api.Configuration;
using Partners.Api.Exceptions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(
        options => options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles
    )
    .AddPortugueseValidationMessages();
builder.Services.AddEndpointsApiExplorer();
builder.Services.ConfigureSwagger();
builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "CORS",
                      policy =>
                      {
                          policy.AllowAnyOrigin()
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                      });
});

builder.Services.AddPersistenceServices(builder.Configuration);
builder.Services.AddApplicationServices();

builder.Services.AddLogging(log => log.AddFilter<ConsoleLoggerProvider>(
    level => level == LogLevel.Warning || level == LogLevel.Error));

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors("CORS");

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "v1"));

app.MapOpenApi();
app.MapScalarApiReference();

app.MapControllers();

// Dados de demonstração: só em Development e só com DemoData:SeedOnStartup=true.
// Não aplica migrações — a base de dados tem de ter sido criada com "dotnet ef database update".
if (app.Environment.IsDevelopment() && app.Configuration.GetValue<bool>("DemoData:SeedOnStartup"))
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        await DemoDataSeeder.SeedAsync(scope.ServiceProvider.GetRequiredService<AppDbContext>(), logger);
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Demo data was not seeded. Make sure the database exists and migrations were applied.");
    }
}

app.Run();

// Exposto para os testes de integração (WebApplicationFactory<Program>).
public partial class Program { }
