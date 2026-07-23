using BlueprintOS.Core.Documentation.Models;
using BlueprintOS.Infrastructure.Documentation;

namespace BlueprintOS.UnitTests.Infrastructure.Documentation;

public class AiDocumentationGeneratorTests
{
    [Fact]
    public void Generate_Should_Produce_Context_Style_Markdown_With_All_Sections()
    {
        var generator = new AiDocumentationGenerator();
        var input = new AiDocumentationInput(
            "documentation",
            "documento de contexto para o módulo Documentation.",
            "Explicar como gerar e versionar documentação.",
            new[] { "Documento", "ADR", "Changelog" },
            new[] { "Sempre versionar mudanças relevantes." },
            new[] { "PROJECT.md" });

        var markdown = generator.Generate(input);

        Assert.Contains("# context/documentation.md", markdown);
        Assert.Contains("## Propósito", markdown);
        Assert.Contains("## Conceitos-Chave", markdown);
        Assert.Contains("## Diretrizes", markdown);
        Assert.Contains("## Referências", markdown);
        Assert.Contains("PROJECT.md", markdown);
    }
}
