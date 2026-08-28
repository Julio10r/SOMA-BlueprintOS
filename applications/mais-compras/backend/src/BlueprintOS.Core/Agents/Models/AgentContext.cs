namespace BlueprintOS.Core.Agents.Models;

/// <summary>
/// Representa o contexto de execução fornecido a um agente.
/// </summary>
public sealed record AgentContext
{
    /// <summary>
    /// Entrada em linguagem natural a ser processada pelo agente.
    /// </summary>
    public required string Input { get; init; }
}
