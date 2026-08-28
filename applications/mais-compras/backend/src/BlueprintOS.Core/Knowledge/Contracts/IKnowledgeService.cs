using BlueprintOS.Core.Knowledge.Models;

namespace BlueprintOS.Core.Knowledge.Contracts;

/// <summary>
/// Define o contrato do serviço responsável por buscar trechos relevantes
/// nos documentos de conhecimento a partir de uma consulta textual.
/// </summary>
public interface IKnowledgeService
{
    /// <summary>
    /// Busca de forma assíncrona os trechos mais relevantes para a consulta informada.
    /// </summary>
    /// <param name="query">Termo ou expressão buscada nos documentos de conhecimento.</param>
    /// <param name="maxResults">Número máximo de resultados a serem retornados.</param>
    /// <param name="cancellationToken">Token utilizado para cancelar a operação.</param>
    Task<IReadOnlyList<KnowledgeSearchResult>> SearchAsync(
        string query,
        int maxResults = 5,
        CancellationToken cancellationToken = default);
}
