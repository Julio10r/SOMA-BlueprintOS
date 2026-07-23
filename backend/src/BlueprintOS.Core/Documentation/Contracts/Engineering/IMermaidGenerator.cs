using BlueprintOS.Core.Documentation.Contracts;

namespace BlueprintOS.Core.Documentation.Contracts.Engineering;

/// <summary>
/// Define o contrato do gerador de diagramas Mermaid do portal de documentação viva,
/// que encapsula <see cref="IMermaidDiagramGenerator"/> aplicado ao grafo real de
/// dependências entre os projetos do backend.
/// </summary>
public interface IMermaidGenerator
{
    /// <summary>
    /// Gera o corpo Markdown contendo o(s) diagrama(s) Mermaid de arquitetura.
    /// </summary>
    Task<string> GenerateAsync(CancellationToken cancellationToken = default);
}
