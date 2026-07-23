using System.Text;
using BlueprintOS.Core.Documentation.Contracts.Engineering;

namespace BlueprintOS.Infrastructure.Documentation.Generators.Engineering;

/// <summary>
/// Implementação de <see cref="IAgentsGenerator"/>, refletindo o conteúdo real do módulo
/// <c>BlueprintOS.Core.Agents</c>.
/// </summary>
public sealed class AgentsGenerator : IAgentsGenerator
{
    /// <inheritdoc />
    public Task<string> GenerateAsync(CancellationToken cancellationToken = default)
    {
        var builder = new StringBuilder();
        builder.AppendLine("## Agentes de IA");
        builder.AppendLine();
        builder.AppendLine("O módulo `BlueprintOS.Core.Agents` define o runtime de agentes especializados:");
        builder.AppendLine();
        builder.AppendLine("- `IAgent` — contrato base implementado por todos os agentes.");
        builder.AppendLine("- `BaseAgent` — classe base que injeta `IAIRuntime` (e, opcionalmente, `IKnowledgeService`)");
        builder.AppendLine("  nos agentes concretos.");
        builder.AppendLine("- `EchoAgent` — agente de referência/diagnóstico.");
        builder.AppendLine("- `KnowledgeAgent` — agente que consulta o módulo `Knowledge` para responder com base em");
        builder.AppendLine("  conhecimento organizacional indexado.");
        builder.AppendLine("- `AgentFactory` — fábrica que cria instâncias de agentes via reflexão, injetando o");
        builder.AppendLine("  runtime de IA e o serviço de conhecimento quando aplicável.");
        builder.AppendLine();
        builder.AppendLine("O módulo `AI.Negotiation` complementa o runtime de agentes com memória de negociação");
        builder.AppendLine("(`INegotiationMemory`) e um motor de estratégia baseado em regras (`INegotiationStrategy`),");
        builder.AppendLine("usados pelo agente Buyer sênior.");

        return Task.FromResult(builder.ToString());
    }
}
