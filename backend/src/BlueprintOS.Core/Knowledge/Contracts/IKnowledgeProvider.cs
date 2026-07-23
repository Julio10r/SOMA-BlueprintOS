using BlueprintOS.Core.Knowledge.Models;

namespace BlueprintOS.Core.Knowledge.Contracts;

/// <summary>
/// Define o contrato para carregamento de documentos de conhecimento a partir de uma fonte externa.
/// </summary>
public interface IKnowledgeProvider
{
    /// <summary>
    /// Carrega de forma assíncrona todos os documentos de conhecimento disponíveis.
    /// </summary>
    /// <param name="cancellationToken">Token utilizado para cancelar a operação.</param>
    Task<IReadOnlyList<KnowledgeDocument>> LoadDocumentsAsync(CancellationToken cancellationToken = default);
}
