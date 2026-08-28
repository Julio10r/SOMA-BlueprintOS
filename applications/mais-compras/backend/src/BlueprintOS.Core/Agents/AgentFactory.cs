using BlueprintOS.Core.AI.Contracts;
using BlueprintOS.Core.Knowledge.Contracts;

namespace BlueprintOS.Core.Agents;

/// <summary>
/// Cria instâncias de agentes derivados de <see cref="BaseAgent"/>, injetando o
/// <see cref="IAIRuntime"/> e, quando necessário, o <see cref="IKnowledgeService"/> utilizados por eles.
/// </summary>
public sealed class AgentFactory
{
    private readonly IAIRuntime _runtime;
    private readonly IKnowledgeService? _knowledgeService;

    /// <summary>
    /// Cria a fábrica utilizando o runtime de IA informado para instanciar os agentes.
    /// </summary>
    /// <param name="runtime">Runtime de IA a ser injetado nos agentes criados.</param>
    /// <param name="knowledgeService">Serviço de conhecimento a ser injetado nos agentes que o exigirem.</param>
    public AgentFactory(IAIRuntime runtime, IKnowledgeService? knowledgeService = null)
    {
        _runtime = runtime;
        _knowledgeService = knowledgeService;
    }

    /// <summary>
    /// Cria uma nova instância do agente do tipo informado.
    /// </summary>
    /// <typeparam name="TAgent">Tipo do agente a ser criado, derivado de <see cref="BaseAgent"/>.</typeparam>
    public TAgent Create<TAgent>() where TAgent : BaseAgent
    {
        var knowledgeConstructor = typeof(TAgent).GetConstructor(new[] { typeof(IAIRuntime), typeof(IKnowledgeService) });
        if (knowledgeConstructor is not null && _knowledgeService is not null)
        {
            return (TAgent)knowledgeConstructor.Invoke(new object[] { _runtime, _knowledgeService });
        }

        return (TAgent)Activator.CreateInstance(typeof(TAgent), _runtime)!;
    }
}
