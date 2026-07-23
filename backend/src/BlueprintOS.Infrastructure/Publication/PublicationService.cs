using BlueprintOS.Core.Publication.Contracts;
using BlueprintOS.Core.Publication.Models;

namespace BlueprintOS.Infrastructure.Publication;

/// <summary>
/// Implementação de <see cref="IPublicationService"/>: ponto único de entrada do Publication
/// Engine. Executa, em sequência, todos os <see cref="IReportPublisher"/> registrados
/// (executivo, cliente e engenharia), gerando a pasta <c>dist/</c> completa em Markdown, HTML
/// e PDF.
/// </summary>
public sealed class PublicationService : IPublicationService
{
    private readonly IEnumerable<IReportPublisher> _publishers;

    public PublicationService(IEnumerable<IReportPublisher> publishers)
    {
        _publishers = publishers;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PublishedArtifact>> PublishAllAsync(CancellationToken cancellationToken = default)
    {
        var artifacts = new List<PublishedArtifact>();

        foreach (var publisher in _publishers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var published = await publisher.PublishAsync(cancellationToken);
            artifacts.AddRange(published);
        }

        return artifacts;
    }
}
