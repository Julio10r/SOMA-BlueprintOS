using BlueprintOS.Infrastructure.Documentation;
using BlueprintOS.Infrastructure.Documentation.Generators.Executive;
using Microsoft.Extensions.Options;

namespace BlueprintOS.UnitTests.Infrastructure.Documentation.Generators.Executive;

public class DashboardGeneratorTests : IDisposable
{
    private readonly string _aiRoot;

    public DashboardGeneratorTests()
    {
        _aiRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(Path.Combine(_aiRoot, "memory"));
    }

    private DashboardGenerator CreateGenerator() =>
        new(Options.Create(new DocumentationOptions { AiRootPath = _aiRoot }));

    [Fact]
    public async Task GenerateAsync_Should_Count_Sprints_From_CompletedSprintsFile()
    {
        File.WriteAllText(
            Path.Combine(_aiRoot, "memory", "completed_sprints.md"),
            "# completed_sprints.md\n\n## Sprint A7 — Foo\n\ntexto\n\n## Sprint A8 — Bar\n\ntexto\n");

        var result = await CreateGenerator().GenerateAsync();

        Assert.Contains("**Sprints concluídas:** 2", result);
        Assert.Contains("Sprint A8", result);
    }

    [Fact]
    public async Task GenerateAsync_Should_Report_No_Sprints_When_File_Missing()
    {
        var result = await CreateGenerator().GenerateAsync();

        Assert.Contains("**Sprints concluídas:** 0", result);
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
