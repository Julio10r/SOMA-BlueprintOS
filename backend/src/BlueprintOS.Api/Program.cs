using BlueprintOS.Core.Publication.Contracts;
using BlueprintOS.Infrastructure.DependencyInjection;
using BlueprintOS.Infrastructure.Publication.Publishers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

if (args.Length > 0 && args[0] == "publish")
{
    return await RunPublicationEngineAsync(args);
}

if (args.Length > 0 && args[0] == "publish-executive-blueprint")
{
    return await RunExecutiveBlueprintAsync();
}

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddOpenApi();
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
        provider.GetRequiredService<IEnumerable<IContentRenderer>>());
    Console.WriteLine("Executive Blueprint: HTML e PDF publicados em docs/executive/.");
    return 0;
}
