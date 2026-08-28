namespace BlueprintOS.Core.Agents.Models;

/// <summary>
/// Representa o resultado produzido pela execução de um agente.
/// </summary>
/// <param name="Output">Saída em linguagem natural produzida pelo agente.</param>
public sealed record AgentResult(string Output);
