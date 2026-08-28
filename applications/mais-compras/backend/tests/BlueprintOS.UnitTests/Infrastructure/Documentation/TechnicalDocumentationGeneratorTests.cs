using BlueprintOS.Core.Documentation.Models;
using BlueprintOS.Infrastructure.Documentation;

namespace BlueprintOS.UnitTests.Infrastructure.Documentation;

public class TechnicalDocumentationGeneratorTests
{
    [Fact]
    public void Generate_Should_Include_Module_Name_Contracts_And_Classes()
    {
        var generator = new TechnicalDocumentationGenerator();
        var metadata = new ModuleMetadata(
            "Knowledge",
            "Módulo responsável pela busca de conhecimento.",
            new[] { "IKnowledgeService", "IKnowledgeProvider" },
            new[] { "KnowledgeService", "MarkdownKnowledgeProvider" });

        var markdown = generator.Generate(metadata);

        Assert.Contains("Módulo Knowledge", markdown);
        Assert.Contains("IKnowledgeService", markdown);
        Assert.Contains("MarkdownKnowledgeProvider", markdown);
    }

    [Fact]
    public void Generate_Should_Indicate_When_No_Contracts_Or_Classes_Registered()
    {
        var generator = new TechnicalDocumentationGenerator();
        var metadata = new ModuleMetadata("Vazio", "Sem itens.", Array.Empty<string>(), Array.Empty<string>());

        var markdown = generator.Generate(metadata);

        Assert.Contains("Nenhum item registrado.", markdown);
    }
}
