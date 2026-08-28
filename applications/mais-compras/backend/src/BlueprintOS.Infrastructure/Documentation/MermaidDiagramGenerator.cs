using System.Text;
using BlueprintOS.Core.Documentation.Contracts;
using BlueprintOS.Core.Documentation.Models;

namespace BlueprintOS.Infrastructure.Documentation;

/// <summary>
/// Implementação de <see cref="IMermaidDiagramGenerator"/> que gera diagramas Mermaid a partir
/// de um <see cref="ModuleDependencyGraph"/>.
/// </summary>
public sealed class MermaidDiagramGenerator : IMermaidDiagramGenerator
{
    /// <inheritdoc />
    public string Generate(ModuleDependencyGraph graph, MermaidDiagramType diagramType = MermaidDiagramType.FlowChart)
    {
        return diagramType switch
        {
            MermaidDiagramType.SequenceDiagram => GenerateSequenceDiagram(graph),
            MermaidDiagramType.ClassDiagram => GenerateClassDiagram(graph),
            _ => GenerateFlowChart(graph),
        };
    }

    private static string GenerateFlowChart(ModuleDependencyGraph graph)
    {
        var builder = new StringBuilder();
        builder.AppendLine("graph TD");

        foreach (var node in graph.Nodes)
        {
            builder.AppendLine($"    {node.Id}[{node.Label}]");
        }

        foreach (var relation in graph.Relations)
        {
            var arrow = string.IsNullOrWhiteSpace(relation.Label)
                ? $"{relation.FromId} --> {relation.ToId}"
                : $"{relation.FromId} -->|{relation.Label}| {relation.ToId}";
            builder.AppendLine($"    {arrow}");
        }

        return builder.ToString();
    }

    private static string GenerateSequenceDiagram(ModuleDependencyGraph graph)
    {
        var builder = new StringBuilder();
        builder.AppendLine("sequenceDiagram");

        foreach (var node in graph.Nodes)
        {
            builder.AppendLine($"    participant {node.Id} as {node.Label}");
        }

        foreach (var relation in graph.Relations)
        {
            var label = string.IsNullOrWhiteSpace(relation.Label) ? string.Empty : $": {relation.Label}";
            builder.AppendLine($"    {relation.FromId}->>{relation.ToId}{label}");
        }

        return builder.ToString();
    }

    private static string GenerateClassDiagram(ModuleDependencyGraph graph)
    {
        var builder = new StringBuilder();
        builder.AppendLine("classDiagram");

        foreach (var node in graph.Nodes)
        {
            builder.AppendLine($"    class {node.Id} {{");
            builder.AppendLine($"        {node.Label}");
            builder.AppendLine("    }");
        }

        foreach (var relation in graph.Relations)
        {
            var label = string.IsNullOrWhiteSpace(relation.Label) ? string.Empty : $" : {relation.Label}";
            builder.AppendLine($"    {relation.FromId} --> {relation.ToId}{label}");
        }

        return builder.ToString();
    }
}
