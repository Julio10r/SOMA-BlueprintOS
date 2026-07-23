using BlueprintOS.Core.Documentation.Models;

namespace BlueprintOS.Core.Documentation.Contracts;

/// <summary>
/// Define o contrato de publicação de um único documento Markdown em disco, incluindo o
/// envelope de cabeçalho (título, data de geração, versão e última atualização) comum a
/// todos os documentos gerados pelo portal de documentação viva.
/// </summary>
public interface IDocumentPublisher
{
    /// <summary>
    /// Publica o conteúdo informado como um arquivo Markdown no caminho relativo indicado,
    /// adicionando o envelope de cabeçalho padrão antes do corpo.
    /// </summary>
    /// <param name="relativePath">Caminho relativo do arquivo de destino (dentro da raiz de documentação).</param>
    /// <param name="title">Título do documento, usado no cabeçalho.</param>
    /// <param name="body">Corpo Markdown do documento, sem o envelope de cabeçalho.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task<PublishedDocument> PublishAsync(
        string relativePath,
        string title,
        string body,
        CancellationToken cancellationToken = default);
}
