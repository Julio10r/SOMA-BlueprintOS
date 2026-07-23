using BlueprintOS.Core.Publication.Models;

namespace BlueprintOS.Core.Publication.Contracts;

/// <summary>
/// Define o contrato de um publicador de relatório: monta um <see cref="PublicationDocument"/>
/// a partir de fontes reais do projeto e o publica em disco, em todos os formatos suportados
/// (Markdown, HTML e PDF), sob <c>dist/{Category}/</c>.
/// </summary>
public interface IReportPublisher
{
    /// <summary>
    /// Categoria de publicação (subpasta de <c>dist/</c>), ex.: <c>executive</c>, <c>client</c>, <c>engineering</c>.
    /// </summary>
    string Category { get; }

    /// <summary>
    /// Monta o documento e o publica em todos os formatos suportados.
    /// </summary>
    Task<IReadOnlyList<PublishedArtifact>> PublishAsync(CancellationToken cancellationToken = default);
}
