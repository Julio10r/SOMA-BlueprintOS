using BlueprintOS.Core.Agents.Models;
using BlueprintOS.Core.Workflows.Contracts;
using BlueprintOS.Core.Workflows.Models;

namespace BlueprintOS.Core.Workflows;

/// <summary>
/// Executa um <see cref="IWorkflow"/> de forma sequencial, repassando a saída de cada agente
/// como entrada do próximo.
/// </summary>
public sealed class WorkflowRunner
{
    /// <summary>
    /// Executa cada etapa do fluxo, na ordem definida, encadeando o contexto entre os agentes.
    /// </summary>
    /// <param name="workflow">Fluxo a ser executado.</param>
    /// <param name="context">Contexto inicial fornecido ao primeiro agente.</param>
    /// <param name="cancellationToken">Token utilizado para cancelar a operação.</param>
    /// <returns>Resultado consolidado da execução do fluxo.</returns>
    public async Task<WorkflowResult> RunAsync(IWorkflow workflow, WorkflowContext context, CancellationToken cancellationToken = default)
    {
        var stepResults = new List<AgentResult>();
        var currentInput = context.Input;

        foreach (var step in workflow.Steps)
        {
            var agentContext = new AgentContext { Input = currentInput };
            var result = await step.Agent.ExecuteAsync(agentContext, cancellationToken);

            stepResults.Add(result);
            currentInput = result.Output;
        }

        return new WorkflowResult(currentInput, stepResults);
    }
}
