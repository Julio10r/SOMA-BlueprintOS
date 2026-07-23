namespace BlueprintOS.Core.Workflows.Models;

/// <summary>
/// Representa o contexto de entrada fornecido a um fluxo no início de sua execução.
/// </summary>
public sealed record WorkflowContext
{
    /// <summary>
    /// Entrada em linguagem natural a ser processada pelo primeiro agente do fluxo.
    /// </summary>
    public required string Input { get; init; }
}
