using BlueprintOS.Core.Agents.Contracts;

namespace BlueprintOS.Core.Workflows.Models;

/// <summary>
/// Representa uma etapa de um fluxo, associada ao agente que a executa.
/// </summary>
/// <param name="Agent">Agente responsável por executar a etapa.</param>
public sealed record WorkflowStep(IAgent Agent);
