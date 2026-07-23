using BlueprintOS.Core.AI.Models;

namespace BlueprintOS.Core.AI.Contracts;

/// <summary>
/// Define o contrato do runtime responsável por orquestrar a execução de requisições de IA,
/// selecionando o provedor apropriado sem expor detalhes de implementação aos consumidores.
/// </summary>
public interface IAIRuntime
{
    /// <summary>
    /// Executa uma requisição de IA de forma assíncrona, delegando o processamento ao provedor apropriado.
    /// </summary>
    /// <param name="request">Requisição contendo o modelo, as mensagens e os parâmetros de execução.</param>
    /// <param name="cancellationToken">Token utilizado para cancelar a operação.</param>
    /// <returns>Resposta gerada pelo modelo de IA.</returns>
    Task<AIResponse> ExecuteAsync(AIRequest request, CancellationToken cancellationToken = default);
}
