using BlueprintOS.Core.Documentation.Contracts;
using BlueprintOS.Core.Documentation.Models;

namespace BlueprintOS.Infrastructure.Documentation.Publishing;

/// <summary>
/// Orquestra a publicação de múltiplos documentos Markdown, delegando a escrita de cada
/// um a um <see cref="IDocumentPublisher"/>. Não conhece a origem do conteúdo (geradores);
/// apenas recebe as solicitações já com o corpo Markdown pronto e as publica em sequência.
/// </summary>
public sealed class DocumentationPublisher
{
    private readonly IDocumentPublisher _documentPublisher;

    public DocumentationPublisher(IDocumentPublisher documentPublisher)
    {
        _documentPublisher = documentPublisher;
    }

    /// <summary>
    /// Publica todas as solicitações informadas, retornando o resultado de cada publicação
    /// na mesma ordem em que foram recebidas.
    /// </summary>
    public async Task<IReadOnlyList<PublishedDocument>> PublishManyAsync(
        IReadOnlyList<DocumentationPublishRequest> requests,
        CancellationToken cancellationToken = default)
    {
        var results = new List<PublishedDocument>(requests.Count);

        foreach (var request in requests)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var published = await _documentPublisher.PublishAsync(
                request.RelativePath,
                request.Title,
                request.Body,
                cancellationToken);
            results.Add(published);
        }

        return results;
    }
}
