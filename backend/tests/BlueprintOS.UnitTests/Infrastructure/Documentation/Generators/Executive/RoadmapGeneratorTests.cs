using BlueprintOS.Infrastructure.Documentation;
using BlueprintOS.Infrastructure.Documentation.Generators.Executive;
using Microsoft.Extensions.Options;

namespace BlueprintOS.UnitTests.Infrastructure.Documentation.Generators.Executive;

public class RoadmapGeneratorTests : IDisposable
{
    private readonly string _aiRoot;

    public RoadmapGeneratorTests()
    {
        _aiRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_aiRoot);
    }

    [Fact]
    public async Task GenerateAsync_Should_Reflect_Real_Roadmap_Content()
    {
        File.WriteAllText(Path.Combine(_aiRoot, "ROADMAP.md"), "# ROADMAP.md\n\n## Fase 0 - Fundação\n\nConteúdo real.\n");
        var generator = new RoadmapGenerator(Options.Create(new DocumentationOptions { AiRootPath = _aiRoot }));

        var result = await generator.GenerateAsync();

        Assert.Contains("Fase 0 - Fundação", result);
        Assert.Contains("Conteúdo real.", result);
    }

    [Fact]
    public async Task GenerateAsync_Should_Return_Honest_Message_When_Roadmap_Missing()
    {
        var generator = new RoadmapGenerator(Options.Create(new DocumentationOptions { AiRootPath = _aiRoot }));

        var result = await generator.GenerateAsync();

        Assert.Contains("Nenhum roadmap registrado", result);
    }

    public void Dispose()
    {
        if (Directory.Exists(_aiRoot))
        {
            Directory.Delete(_aiRoot, recursive: true);
        }
    }
}
