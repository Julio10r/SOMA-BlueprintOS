namespace BlueprintOS.Core.Documentation.Models;

/// <summary>
/// Representa um nó de um diagrama Mermaid.
/// </summary>
/// <param name="Id">Identificador único do nó dentro do diagrama.</param>
/// <param name="Label">Rótulo exibido para o nó.</param>
public sealed record MermaidNode(string Id, string Label);

/// <summary>
/// Representa uma relação (aresta) entre dois nós de um diagrama Mermaid.
/// </summary>
/// <param name="FromId">Identificador do nó de origem.</param>
/// <param name="ToId">Identificador do nó de destino.</param>
/// <param name="Label">Rótulo opcional descrevendo a relação.</param>
public sealed record MermaidRelation(string FromId, string ToId, string Label = "");

/// <summary>
/// Representa o grafo de dependências entre módulos ou componentes, usado como entrada
/// para geração de diagramas Mermaid.
/// </summary>
/// <param name="Nodes">Nós do grafo.</param>
/// <param name="Relations">Relações entre os nós do grafo.</param>
public sealed record ModuleDependencyGraph(
    IReadOnlyList<MermaidNode> Nodes,
    IReadOnlyList<MermaidRelation> Relations);

/// <summary>
/// Tipos de diagrama Mermaid suportados pelo gerador.
/// </summary>
public enum MermaidDiagramType
{
    /// <summary>
    /// Diagrama de fluxo (<c>graph TD</c>).
    /// </summary>
    FlowChart,

    /// <summary>
    /// Diagrama de sequência (<c>sequenceDiagram</c>).
    /// </summary>
    SequenceDiagram,

    /// <summary>
    /// Diagrama de classes (<c>classDiagram</c>).
    /// </summary>
    ClassDiagram,
}
