using BlueprintOS.Core.AI.Contracts;
using BlueprintOS.Core.Agents.Contracts;
using BlueprintOS.Core.Agents.Models;

namespace BlueprintOS.Core.Agents;

/// <summary>
/// Fornece a base comum para implementações de <see cref="IAgent"/>, expondo o
/// <see cref="IAIRuntime"/> utilizado para executar requisições de IA.
/// </summary>
public abstract class BaseAgent : IAgent
{
    /// <summary>
    /// Inicializa o agente com o runtime de IA a ser utilizado durante sua execução.
    /// </summary>
    /// <param name="runtime">Runtime de IA utilizado pelo agente.</param>
    protected BaseAgent(IAIRuntime runtime)
    {
        Runtime = runtime;
    }

    /// <summary>
    /// Runtime de IA utilizado pelo agente para processar sua entrada.
    /// </summary>
    protected IAIRuntime Runtime { get; }

    /// <inheritdoc />
    public abstract Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken cancellationToken = default);
}
