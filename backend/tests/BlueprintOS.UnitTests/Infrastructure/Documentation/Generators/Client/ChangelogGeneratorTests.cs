using BlueprintOS.Infrastructure.Documentation;
using BlueprintOS.Infrastructure.Documentation.Generators.Client;
using Microsoft.Extensions.Options;

namespace BlueprintOS.UnitTests.Infrastructure.Documentation.Generators.Client;

public class ChangelogGeneratorTests : IDisposable
{
    private readonly string _aiRoot;

    public ChangelogGeneratorTests()
    {
        _aiRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(Path.Combine(_aiRoot, "memory"));
    }

    [Fact]
    public async Task GenerateAsync_Should_List_Sprints_Newest_First()
    {
        File.WriteAllText(
            Path.Combine(_aiRoot, "memory", "completed_sprints.md"),
            "## Sprint A7 — Foo\n\n**Escopo:** escopo da sete\n\n## Sprint A8 — Bar\n\n**Escopo:** escopo da oito\n");
        var generator = new ChangelogGenerator(Options.Create(new DocumentationOptions { AiRootPath = _aiRoot }));

        var result = await generator.GenerateAsync();

        Assert.True(result.IndexOf("Sprint A8", StringComparison.Ordinal) < result.IndexOf("Sprint A7", StringComparison.Ordinal));
    }

    public void Dispose()
    {
        if (Directory.Exists(_aiRoot))
        {
            Directory.Delete(_aiRoot, recursive: true);
        }
    }
}
