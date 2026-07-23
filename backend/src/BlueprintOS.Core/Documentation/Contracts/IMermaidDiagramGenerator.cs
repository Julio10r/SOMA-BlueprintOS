using BlueprintOS.Core.Documentation.Models;

namespace BlueprintOS.Core.Documentation.Contracts;

/// <summary>
/// Define o contrato do gerador de diagramas Mermaid a partir de um grafo de dependências.
/// </summary>
public interface IMermaidDiagramGenerator
{
    /// <summary>
    /// Gera a string de um diagrama Mermaid do tipo informado a partir do grafo de dependências.
    /// </summary>
    string Generate(ModuleDependencyGraph graph, MermaidDiagramType diagramType = MermaidDiagramType.FlowChart);
}
