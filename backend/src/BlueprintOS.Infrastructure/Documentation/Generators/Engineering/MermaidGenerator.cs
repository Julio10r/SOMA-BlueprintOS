using System.Text;
using BlueprintOS.Core.Documentation.Contracts;
using BlueprintOS.Core.Documentation.Contracts.Engineering;
using BlueprintOS.Core.Documentation.Models;

namespace BlueprintOS.Infrastructure.Documentation.Generators.Engineering;

/// <summary>
/// Implementação de <see cref="IMermaidGenerator"/> que encapsula
/// <see cref="IMermaidDiagramGenerator"/> aplicado ao grafo real de dependências entre os
/// projetos do backend (derivado das referências de projeto reais nos arquivos <c>.csproj</c>).
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
        builder.AppendLine("Grafo real de referências de projeto (`ProjectReference`) entre os projetos");
        builder.AppendLine("`.csproj` do backend:");
        builder.AppendLine();
        builder.AppendLine("```mermaid");
        builder.AppendLine(diagram.TrimEnd());
        builder.AppendLine("```");

        return Task.FromResult(builder.ToString());
    }
}
