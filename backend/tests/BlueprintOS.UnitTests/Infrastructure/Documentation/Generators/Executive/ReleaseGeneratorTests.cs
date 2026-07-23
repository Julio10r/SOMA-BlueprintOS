using BlueprintOS.Infrastructure.Documentation;
using BlueprintOS.Infrastructure.Documentation.Generators.Executive;
using Microsoft.Extensions.Options;

namespace BlueprintOS.UnitTests.Infrastructure.Documentation.Generators.Executive;

public class ReleaseGeneratorTests : IDisposable
{
    private readonly string _aiRoot;

    public ReleaseGeneratorTests()
    {
        _aiRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(Path.Combine(_aiRoot, "memory"));
    }

    [Fact]
    public async Task GenerateAsync_Should_List_Each_Sprint_As_A_Release()
    {
        File.WriteAllText(
            Path.Combine(_aiRoot, "memory", "completed_sprints.md"),
            "## Sprint A7 — Foo\n\ntexto\n\n## Sprint A8 — Bar\n\ntexto\n");
        var generator = new ReleaseGenerator(Options.Create(new DocumentationOptions { AiRootPath = _aiRoot }));

        var result = await generator.GenerateAsync();

        Assert.Contains("Sprint A7 — Foo", result);
        Assert.Contains("Sprint A8 — Bar", result);
    }

    [Fact]
    public async Task GenerateAsync_Should_State_No_Releases_When_File_Missing()
    {
        var generator = new ReleaseGenerator(Options.Create(new DocumentationOptions { AiRootPath = _aiRoot }));

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
