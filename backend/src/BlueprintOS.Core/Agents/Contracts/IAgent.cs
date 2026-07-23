using BlueprintOS.Core.Agents.Models;

namespace BlueprintOS.Core.Agents.Contracts;

/// <summary>
/// Define o contrato mínimo de um agente capaz de processar um <see cref="AgentContext"/>
/// e produzir um <see cref="AgentResult"/>.
/// </summary>
public interface IAgent
{
    /// <summary>
    /// Executa o agente de forma assíncrona a partir do contexto informado.
    /// </summary>
    /// <param name="context">Contexto de execução contendo a entrada do agente.</param>
    /// <param name="cancellationToken">Token utilizado para cancelar a operação.</param>
    /// <returns>Resultado produzido pela execução do agente.</returns>
    Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken cancellationToken = default);
}
