using BlueprintOS.Core.Agents.Contracts;
using BlueprintOS.Core.Workflows.Contracts;
using BlueprintOS.Core.Workflows.Models;

namespace BlueprintOS.Core.Workflows;

/// <summary>
/// Fluxo que executa uma sequência ordenada de agentes.
/// </summary>
public sealed class Workflow : IWorkflow
{
    /// <summary>
    /// Cria o fluxo a partir da sequência de agentes informada.
    /// </summary>
    /// <param name="agents">Agentes que compõem o fluxo, na ordem de execução.</param>
    public Workflow(IEnumerable<IAgent> agents)
    {
        Steps = agents.Select(agent => new WorkflowStep(agent)).ToList();
    }

    /// <inheritdoc />
    public IReadOnlyList<WorkflowStep> Steps { get; }
}
