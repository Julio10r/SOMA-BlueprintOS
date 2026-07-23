using BlueprintOS.Infrastructure.Documentation;
using BlueprintOS.Infrastructure.Documentation.Generators.Engineering;
using Microsoft.Extensions.Options;

namespace BlueprintOS.UnitTests.Infrastructure.Documentation.Generators.Engineering;

public class DecisionsGeneratorTests : IDisposable
{
    private readonly string _aiRoot;

    public DecisionsGeneratorTests()
    {
        _aiRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_aiRoot);
    }

    [Fact]
    public async Task GenerateAsync_Should_List_Real_Adr_Titles()
    {
        File.WriteAllText(
            Path.Combine(_aiRoot, "DECISIONS.md"),
            "## ADR-0001: Adoção de Modular Monolith\n\ntexto\n\n## ADR-0002: Seleção da stack\n\ntexto\n");
        var generator = new DecisionsGenerator(Options.Create(new DocumentationOptions { AiRootPath = _aiRoot }));

        var result = await generator.GenerateAsync();

        Assert.Contains("ADR-0001: Adoção de Modular Monolith", result);
        Assert.Contains("ADR-0002: Seleção da stack", result);
    }

    [Fact]
    public async Task GenerateAsync_Should_State_No_Adrs_When_File_Missing()
    {
        var generator = new DecisionsGenerator(Options.Create(new DocumentationOptions { AiRootPath = _aiRoot }));

        var result = await generator.GenerateAsync();

        Assert.Contains("Nenhuma ADR registrada", result);
    }

    public void Dispose()
    {
        if (Directory.Exists(_aiRoot))
        {
            Directory.Delete(_aiRoot, recursive: true);
        }
    }
}
