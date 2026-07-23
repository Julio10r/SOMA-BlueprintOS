using BlueprintOS.Core.AI.Contracts;
using BlueprintOS.Core.AI.Models;
using BlueprintOS.Core.Agents.Models;

namespace BlueprintOS.Core.Agents;

/// <summary>
/// Agente de referência que encaminha a entrada diretamente ao <see cref="IAIRuntime"/>
/// e devolve a resposta obtida, sem qualquer processamento adicional.
/// </summary>
public sealed class EchoAgent : BaseAgent
{
    /// <summary>
    /// Inicializa o agente com o runtime de IA a ser utilizado.
    /// </summary>
    /// <param name="runtime">Runtime de IA utilizado pelo agente.</param>
    public EchoAgent(IAIRuntime runtime)
        : base(runtime)
    {
    }

    /// <inheritdoc />
    public override async Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken cancellationToken = default)
    {
        var response = await Runtime.ExecuteAsync(new AIRequest(context.Input), cancellationToken);
        return new AgentResult(response.Text);
    }
}
