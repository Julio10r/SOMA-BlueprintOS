using BlueprintOS.Core.AI.Contracts;

namespace BlueprintOS.Core.Agents;

/// <summary>
/// Cria instâncias de agentes derivados de <see cref="BaseAgent"/>, injetando o
/// <see cref="IAIRuntime"/> utilizado por eles.
/// </summary>
public sealed class AgentFactory
{
    private readonly IAIRuntime _runtime;

    /// <summary>
    /// Cria a fábrica utilizando o runtime de IA informado para instanciar os agentes.
    /// </summary>
    /// <param name="runtime">Runtime de IA a ser injetado nos agentes criados.</param>
    public AgentFactory(IAIRuntime runtime)
    {
        _runtime = runtime;
    }

    /// <summary>
    /// Cria uma nova instância do agente do tipo informado.
    /// </summary>
    /// <typeparam name="TAgent">Tipo do agente a ser criado, derivado de <see cref="BaseAgent"/>.</typeparam>
    public TAgent Create<TAgent>() where TAgent : BaseAgent
        => (TAgent)Activator.CreateInstance(typeof(TAgent), _runtime)!;
}
