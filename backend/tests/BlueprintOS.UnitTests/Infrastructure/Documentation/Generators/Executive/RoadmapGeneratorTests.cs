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

    [Fact]
    public async Task GenerateAsync_Should_Rewrite_Relative_Ai_Links_To_Be_Valid_From_Docs_Executive()
    {
        File.WriteAllText(
            Path.Combine(_aiRoot, "ROADMAP.md"),
            "# ROADMAP.md\n\n" +
            "Ver a [ADR-0013](./DECISIONS.md) para detalhes.\n" +
            "Ver também [PROJECT_STATE](PROJECT_STATE.md).\n" +
            "Link externo: [GitHub](https://github.com/example/repo).\n" +
            "Âncora local: [seção](#fase-0).\n");
        var generator = new RoadmapGenerator(Options.Create(new DocumentationOptions { AiRootPath = _aiRoot }));

        var result = await generator.GenerateAsync();

        Assert.Contains("[ADR-0013](../../.ai/DECISIONS.md)", result);
        Assert.Contains("[PROJECT_STATE](../../.ai/PROJECT_STATE.md)", result);
        Assert.Contains("[GitHub](https://github.com/example/repo)", result);
        Assert.Contains("[seção](#fase-0)", result);
        Assert.DoesNotContain("](./DECISIONS.md)", result);
        Assert.DoesNotContain("](PROJECT_STATE.md)", result);
    }

    public void Dispose()
    {
        if (Directory.Exists(_aiRoot))
        {
            Directory.Delete(_aiRoot, recursive: true);
        }
    }
}
