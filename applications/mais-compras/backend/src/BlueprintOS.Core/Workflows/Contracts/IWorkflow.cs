using BlueprintOS.Core.Workflows.Models;

namespace BlueprintOS.Core.Workflows.Contracts;

/// <summary>
/// Define um fluxo composto por uma sequência ordenada de agentes.
/// </summary>
public interface IWorkflow
{
    /// <summary>
    /// Etapas do fluxo, na ordem em que devem ser executadas.
    /// </summary>
    IReadOnlyList<WorkflowStep> Steps { get; }
}
