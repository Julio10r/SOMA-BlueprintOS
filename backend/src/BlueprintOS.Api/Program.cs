using BlueprintOS.Core.Documentation.Contracts;
using BlueprintOS.Core.Publication.Contracts;
using BlueprintOS.Api.Identity;
using BlueprintOS.Api.Negotiations;
using BlueprintOS.Api.Suppliers;
using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Infrastructure.DependencyInjection;
using BlueprintOS.Infrastructure.Persistence;
using BlueprintOS.Infrastructure.Publication.Publishers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text.RegularExpressions;

if (args.Length > 0 && args[0] == "publish")
{
    return await RunPublicationEngineAsync(args);
}

if (args.Length > 0 && args[0] == "publish-docs")
{
    return await RunDocumentationPublishServiceAsync();
}

if (args.Length > 0 && args[0] == "publish-executive-blueprint")
{
    return await RunExecutiveBlueprintAsync();
}

if (args.Length > 0 && args[0] == "migrate")
{
    return await RunMigrationsAsync();
}

if (args.Length > 0 && args[0] == "validate-maiscompras")
{
    return await ValidateMaisComprasAsync();
}

if (args.Length > 0 && args[0] == "validate-b1-connectivity")
{
    return await ValidateB1ConnectivityAsync();
}

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentIdentity, DevelopmentRequestIdentity>();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Health Endpoint
app.MapGet("/health", () =>
{
    return Results.Ok(new
    {
        Status = "Healthy",
        Application = "BlueprintOS",
        Environment = app.Environment.EnvironmentName,
        Version = "1.0.0"
    });
});

app.MapNegotiationRecommendation();
app.MapFornecedores();
app.MapFornecedorDiscovery();

app.Run();
return 0;

static async Task<int> RunPublicationEngineAsync(string[] args)
{
    var repoRoot = FindRepoRoot(AppContext.BaseDirectory) ?? Directory.GetCurrentDirectory();
    Directory.SetCurrentDirectory(repoRoot);

    var configuration = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: true)
        .AddEnvironmentVariables()
        .Build();

    var services = new ServiceCollection();
    services.AddInfrastructure(configuration);

#pragma warning disable ASP0000 // ponto de entrada isolado para o CLI de publicação, sem relação com o host web.
    await using var provider = services.BuildServiceProvider();
#pragma warning restore ASP0000

    var publicationService = provider.GetRequiredService<IPublicationService>();
    var artifacts = await publicationService.PublishAllAsync();

    Console.WriteLine($"Publication Engine: {artifacts.Count} artefato(s) publicado(s) em dist/.");
    foreach (var artifact in artifacts)
    {
        Console.WriteLine($"  - {artifact.RelativePath}");
    }

    var healthService = provider.GetRequiredService<IDocumentationHealthService>();
    var healthReport = await healthService.AnalyzeAsync(artifacts);
    var healthReportPath = await healthService.WriteReportAsync(healthReport);

    Console.WriteLine(
        $"Documentation Health: {healthReport.HealthyCount} saudável(is), {healthReport.WarningCount} aviso(s), {healthReport.ErrorCount} erro(s). Relatório em {healthReportPath}.");

    return 0;
}

static async Task<int> RunDocumentationPublishServiceAsync()
{
    var repoRoot = FindRepoRoot(AppContext.BaseDirectory) ?? Directory.GetCurrentDirectory();
    Directory.SetCurrentDirectory(repoRoot);

    var configuration = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: true)
        .AddEnvironmentVariables()
        .Build();

    var services = new ServiceCollection();
    services.AddInfrastructure(configuration);

#pragma warning disable ASP0000 // ponto de entrada isolado para o CLI de publicação, sem relação com o host web.
    await using var provider = services.BuildServiceProvider();
#pragma warning restore ASP0000

    var documentationPublishService = provider.GetRequiredService<IDocumentationPublishService>();
    var documents = await documentationPublishService.PublishAllAsync();

    Console.WriteLine($"Portal de Documentação Viva: {documents.Count} documento(s) publicado(s) em docs/.");

    return 0;
}

static string? FindRepoRoot(string startDirectory)
{
    var directory = startDirectory;
    while (directory is not null)
    {
        if (Directory.Exists(Path.Combine(directory, ".git")))
        {
            return directory;
        }

        directory = Path.GetDirectoryName(directory.TrimEnd(Path.DirectorySeparatorChar));
    }

    return null;
}

static async Task<int> RunExecutiveBlueprintAsync()
{
    var repoRoot = FindRepoRoot(AppContext.BaseDirectory) ?? Directory.GetCurrentDirectory();
    var configuration = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: true)
        .AddEnvironmentVariables()
        .Build();

    var services = new ServiceCollection();
    services.AddInfrastructure(configuration);

#pragma warning disable ASP0000
    await using var provider = services.BuildServiceProvider();
#pragma warning restore ASP0000

    await ExecutiveBlueprintPublisher.PublishAsync(
        repoRoot,
        provider.GetRequiredService<IEnumerable<IContentRenderer>>(),
        provider.GetRequiredService<IDocumentThemeProvider>());
    Console.WriteLine("Executive Blueprint: HTML e PDF publicados em docs/executive/.");
    return 0;
}

static async Task<int> RunMigrationsAsync()
{
    var configuration = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: true)
        .AddJsonFile("appsettings.Development.json", optional: true)
        .AddUserSecrets<Program>(optional: true)
        .AddEnvironmentVariables()
        .Build();

    var services = new ServiceCollection();
    services.AddInfrastructure(configuration);
#pragma warning disable ASP0000 // Isolated CLI composition root; no ASP.NET host is created.
    await using var provider = services.BuildServiceProvider();
#pragma warning restore ASP0000
    await DatabaseMigrationService.ApplyPendingMigrationsAsync(provider);
    Console.WriteLine("Connectivity confirmed and pending migrations applied.");
    return 0;
}

static async Task<int> ValidateMaisComprasAsync()
{
    var configuration = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: true)
        .AddJsonFile("appsettings.Development.json", optional: true)
        .AddUserSecrets<Program>(optional: true)
        .AddEnvironmentVariables()
        .Build();

    var services = new ServiceCollection();
    services.AddInfrastructure(configuration);
#pragma warning disable ASP0000 // Isolated CLI composition root; no ASP.NET host is created.
    await using var provider = services.BuildServiceProvider();
#pragma warning restore ASP0000
    var result = await provider.GetRequiredService<B1ConnectivityValidator>().ValidateMaisComprasAsync();
    WriteConnectivityResult(result);
    return result.IsSuccess ? 0 : 1;
}

static async Task<int> ValidateB1ConnectivityAsync()
{
    var configuration = BuildDatabaseConfiguration();
    var services = new ServiceCollection();
    services.AddInfrastructure(configuration);
#pragma warning disable ASP0000 // Isolated CLI composition root; no ASP.NET host is created.
    await using var provider = services.BuildServiceProvider();
#pragma warning restore ASP0000
    var validator = provider.GetRequiredService<B1ConnectivityValidator>();
    var maisCompras = await validator.ValidateMaisComprasAsync();
    var erp = await validator.ValidateErpAsync();

    Console.WriteLine($"+Compras ........ {(maisCompras.IsSuccess ? "SUCESSO" : "FALHA")}");
    Console.WriteLine($"ERP SOMA_DESENV . {(erp.IsSuccess ? "SUCESSO" : "FALHA")}");
    WriteConnectivityError(maisCompras);
    WriteConnectivityError(erp);
    return maisCompras.IsSuccess && erp.IsSuccess ? 0 : 1;
}

static IConfiguration BuildDatabaseConfiguration() => new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile("appsettings.Development.json", optional: true)
    .AddUserSecrets<Program>(optional: true)
    .AddEnvironmentVariables()
    .Build();

static void WriteConnectivityResult(DatabaseConnectivityResult result)
{
    Console.WriteLine($"{result.Label} ........ {(result.IsSuccess ? "SUCESSO" : "FALHA")}");
    WriteConnectivityError(result);
}

static void WriteConnectivityError(DatabaseConnectivityResult result)
{
    if (result.IsSuccess || result.Exception is null) return;
    Console.WriteLine($"  Exceção: {result.Exception.GetType().FullName}");
    Console.WriteLine($"  Mensagem: {SanitizeConnectivityMessage(result.Exception.Message)}");
    Console.WriteLine($"  Servidor: {result.Server ?? "não disponível"}");
    Console.WriteLine($"  Banco: {result.Database ?? "não disponível"}");
}

static string SanitizeConnectivityMessage(string message)
{
    var sanitized = Regex.Replace(message, "(?i)(login failed for user\\s*)'[^']*'", "$1'[REDACTED]'");
    return Regex.Replace(sanitized, "(?i)(user id|uid|password|pwd)\\s*=\\s*[^;\\r\\n]*", "$1=[REDACTED]");
}
