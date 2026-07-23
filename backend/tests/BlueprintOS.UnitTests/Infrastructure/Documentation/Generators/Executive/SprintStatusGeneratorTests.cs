using BlueprintOS.Infrastructure.Documentation;
using BlueprintOS.Infrastructure.Documentation.Generators.Executive;
using Microsoft.Extensions.Options;

namespace BlueprintOS.UnitTests.Infrastructure.Documentation.Generators.Executive;

public class SprintStatusGeneratorTests : IDisposable
{
    private readonly string _aiRoot;

    public SprintStatusGeneratorTests()
    {
        _aiRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(Path.Combine(_aiRoot, "memory"));
    }

    [Fact]
    public async Task GenerateAsync_Should_Return_Only_Last_Sprint_Section()
    {
        File.WriteAllText(
            Path.Combine(_aiRoot, "memory", "completed_sprints.md"),
            "# completed_sprints.md\n\n## Sprint A7 — Primeira\n\n**Status:** Concluída\n\n## Sprint A8 — Segunda\n\n**Status:** Concluída\n");
        var generator = new SprintStatusGenerator(Options.Create(new DocumentationOptions { AiRootPath = _aiRoot }));

        var result = await generator.GenerateAsync();

        Assert.Contains("Sprint A8 — Segunda", result);
        Assert.DoesNotContain("Sprint A7", result);
    }

    [Fact]
    public async Task GenerateAsync_Should_Return_Honest_Message_When_No_File()
    {
        var generator = new SprintStatusGenerator(Options.Create(new DocumentationOptions { AiRootPath = _aiRoot }));

        var result = await generator.GenerateAsync();

        Assert.Contains("Nenhuma sprint concluída registrada", result);
    }

    public void Dispose()
    {
        if (Directory.Exists(_aiRoot))
        {
            Directory.Delete(_aiRoot, recursive: true);
        }
    }
}
