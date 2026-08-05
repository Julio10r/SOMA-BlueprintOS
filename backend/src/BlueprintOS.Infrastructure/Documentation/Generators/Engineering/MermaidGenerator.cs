using System.Text;
using BlueprintOS.Core.Documentation.Contracts;
using BlueprintOS.Core.Documentation.Contracts.Engineering;
using BlueprintOS.Core.Documentation.Models;

namespace BlueprintOS.Infrastructure.Documentation.Generators.Engineering;

/// <summary>
/// Implementação de <see cref="IMermaidGenerator"/> que encapsula
/// <see cref="IMermaidDiagramGenerator"/> aplicado a uma representação do grafo de dependências
/// entre os projetos do backend. O grafo é mantido manualmente por este gerador (não é parseado
/// a partir dos arquivos <c>.csproj</c>) e deve ser atualizado sempre que uma referência de
/// projeto (<c>ProjectReference</c>) for adicionada, removida ou alterada.
/// </summary>
public sealed class MermaidGenerator : IMermaidGenerator
{
    private static readonly ModuleDependencyGraph ProjectDependencyGraph = new(
        new[]
        {
            new MermaidNode("Api", "BlueprintOS.Api"),
            new MermaidNode("Application", "BlueprintOS.Application"),
            new MermaidNode("Domain", "BlueprintOS.Domain"),
            new MermaidNode("Infrastructure", "BlueprintOS.Infrastructure"),
            new MermaidNode("Core", "BlueprintOS.Core"),
            new MermaidNode("Shared", "BlueprintOS.Shared"),
        },
        new[]
        {
            new MermaidRelation("Api", "Application", "referencia"),
            new MermaidRelation("Api", "Infrastructure", "referencia"),
            new MermaidRelation("Api", "Shared", "referencia"),
            new MermaidRelation("Application", "Domain", "referencia"),
            new MermaidRelation("Application", "Shared", "referencia"),
            new MermaidRelation("Domain", "Shared", "referencia"),
            new MermaidRelation("Infrastructure", "Application", "referencia"),
            new MermaidRelation("Infrastructure", "Core", "referencia"),
            new MermaidRelation("Infrastructure", "Domain", "referencia"),
            new MermaidRelation("Infrastructure", "Shared", "referencia"),
        });

    private readonly IMermaidDiagramGenerator _mermaidDiagramGenerator;

    public MermaidGenerator(IMermaidDiagramGenerator mermaidDiagramGenerator)
    {
        _mermaidDiagramGenerator = mermaidDiagramGenerator;
    }

    /// <inheritdoc />
    public Task<string> GenerateAsync(CancellationToken cancellationToken = default)
    {
        var diagram = _mermaidDiagramGenerator.Generate(ProjectDependencyGraph, MermaidDiagramType.FlowChart);

        var builder = new StringBuilder();
        builder.AppendLine("## Diagrama de dependências entre projetos");
        builder.AppendLine();
        builder.AppendLine("Representação mantida manualmente das referências de projeto (`ProjectReference`)");
        builder.AppendLine("entre os projetos `.csproj` do backend; deve ser atualizada quando essas referências mudarem:");
        builder.AppendLine();
        builder.AppendLine("```mermaid");
        builder.AppendLine(diagram.TrimEnd());
        builder.AppendLine("```");

        return Task.FromResult(builder.ToString());
    }
}
