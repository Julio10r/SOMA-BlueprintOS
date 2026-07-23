using BlueprintOS.Infrastructure.Documentation;
using BlueprintOS.Infrastructure.Documentation.Generators.Client;
using Microsoft.Extensions.Options;

namespace BlueprintOS.UnitTests.Infrastructure.Documentation.Generators.Client;

public class ProductOverviewGeneratorTests : IDisposable
{
    private readonly string _aiRoot;

    public ProductOverviewGeneratorTests()
    {
        _aiRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_aiRoot);
    }

    [Fact]
    public async Task GenerateAsync_Should_List_Real_Phases_From_Roadmap()
    {
        File.WriteAllText(Path.Combine(_aiRoot, "ROADMAP.md"), "## Fase 0 - Fundação\n\ntexto\n\n## Fase 1 - Módulos Core\n\ntexto\n");
        var generator = new ProductOverviewGenerator(Options.Create(new DocumentationOptions { AiRootPath = _aiRoot }));

        var result = await generator.GenerateAsync();

        Assert.Contains("Fase 0 - Fundação", result);
        Assert.Contains("Fase 1 - Módulos Core", result);
    }

    public void Dispose()
    {
        if (Directory.Exists(_aiRoot))
        {
            Directory.Delete(_aiRoot, recursive: true);
        }
    }
}
