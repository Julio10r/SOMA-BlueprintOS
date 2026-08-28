using BlueprintOS.Core.AI.Contracts;
using BlueprintOS.Core.AI.Models;
using BlueprintOS.Core.Agents.Models;
using BlueprintOS.Core.Agents.Observability;

namespace BlueprintOS.Core.Agents;

/// <summary>
/// Agente de referência que encaminha a entrada diretamente ao <see cref="IAIRuntime"/>
/// e devolve a resposta obtida, sem qualquer processamento adicional.
/// </summary>
public sealed class EchoAgent : BaseAgent
{
    private const string AgentId = "echo-agent";
    private readonly IAgentExecutionObserver _observer;

    /// <summary>
    /// Inicializa o agente com o runtime de IA a ser utilizado.
    /// </summary>
    /// <param name="runtime">Runtime de IA utilizado pelo agente.</param>
    public EchoAgent(IAIRuntime runtime)
        : this(runtime, DiagnosticAgentExecutionObserver.Instance)
    {
    }

    /// <summary>Inicializa o agente com runtime e observer redigido opcional.</summary>
    /// <param name="runtime">Runtime de IA utilizado pelo agente.</param>
    /// <param name="observer">Observer que recebe somente metadata minima da execucao.</param>
    public EchoAgent(IAIRuntime runtime, IAgentExecutionObserver observer)
        : base(runtime)
    {
        _observer = observer;
    }

    /// <inheritdoc />
    public override async Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken cancellationToken = default)
    {
        Record("agent.execution.started", AgentExecutionOutcome.Started);
        try
        {
            var response = await Runtime.ExecuteAsync(new AIRequest(context.Input), cancellationToken);
            Record("agent.execution.completed", AgentExecutionOutcome.Succeeded);
            return new AgentResult(response.Text);
        }
        catch
        {
            Record("agent.execution.failed", AgentExecutionOutcome.Failed, "ai-runtime");
            throw;
        }
    }

    private void Record(string eventName, AgentExecutionOutcome outcome, string? failureCategory = null)
        => AgentExecutionObservationRecorder.RecordSafely(
            _observer,
            new AgentExecutionObservation(AgentId, eventName, outcome, failureCategory));
}
