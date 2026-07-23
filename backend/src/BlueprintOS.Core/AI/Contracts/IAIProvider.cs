using BlueprintOS.Core.AI.Models;

namespace BlueprintOS.Core.AI.Contracts;

/// <summary>
/// Define o contrato que um provedor de IA deve implementar para ser executado pelo runtime do Core.AI.
/// Implementações concretas residem fora deste módulo e são responsáveis por integrar SDKs externos.
/// </summary>
public interface IAIProvider
{
    /// <summary>
    /// Nome único do provedor (ex.: "openai", "anthropic", "azure-openai").
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Executa uma requisição de IA de forma assíncrona e retorna a resposta gerada.
    /// </summary>
    /// <param name="request">Requisição contendo o modelo, as mensagens e os parâmetros de execução.</param>
    /// <param name="cancellationToken">Token utilizado para cancelar a operação.</param>
    /// <returns>Resposta gerada pelo modelo de IA.</returns>
    Task<AIResponse> CompleteAsync(AIRequest request, CancellationToken cancellationToken = default);
}
