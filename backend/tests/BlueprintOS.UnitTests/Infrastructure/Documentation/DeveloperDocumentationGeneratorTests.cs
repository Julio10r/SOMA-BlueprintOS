using BlueprintOS.Core.Documentation.Models;
using BlueprintOS.Infrastructure.Documentation;

namespace BlueprintOS.UnitTests.Infrastructure.Documentation;

public class DeveloperDocumentationGeneratorTests
{
    [Fact]
    public void Generate_Should_Produce_Readme_Style_Markdown_With_All_Sections()
    {
        var generator = new DeveloperDocumentationGenerator();
        var input = new DeveloperDocumentationInput(
            "Guia do Módulo Documentation",
            "Visão geral do módulo de documentação.",
            new[] { "Clonar o repositório", "Executar dotnet restore" },
            new[] { "var doc = await service.CreateAsync(...);" },
            new[] { "Verifique se o diretório de ADRs existe." });

        var markdown = generator.Generate(input);

        Assert.Contains("# Guia do Módulo Documentation", markdown);
        Assert.Contains("## Setup", markdown);
        Assert.Contains("1. Clonar o repositório", markdown);
        Assert.Contains("## Exemplos de Uso", markdown);
        Assert.Contains("## Troubleshooting", markdown);
    }
}
