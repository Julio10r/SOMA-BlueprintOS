using BlueprintOS.Core.Agents.Models;

namespace BlueprintOS.Core.Workflows.Models;

/// <summary>
/// Representa o resultado produzido pela execução completa de um fluxo.
/// </summary>
/// <param name="Output">Saída final produzida pelo último agente executado.</param>
/// <param name="StepResults">Resultados produzidos por cada agente, na ordem de execução.</param>
public sealed record WorkflowResult(string Output, IReadOnlyList<AgentResult> StepResults);
