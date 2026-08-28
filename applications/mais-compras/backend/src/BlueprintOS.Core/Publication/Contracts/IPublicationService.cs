using BlueprintOS.Core.Publication.Models;

namespace BlueprintOS.Core.Publication.Contracts;

/// <summary>
/// Define o contrato do Publication Engine: ponto único de entrada que executa todos os
/// publicadores de relatório (executivo, cliente e engenharia) e gera a pasta <c>dist/</c>
/// completa, em Markdown, HTML e PDF.
/// </summary>
public interface IPublicationService
{
    /// <summary>
    /// Executa todos os publicadores registrados e gera todos os artefatos de <c>dist/</c>.
    /// </summary>
    Task<IReadOnlyList<PublishedArtifact>> PublishAllAsync(CancellationToken cancellationToken = default);
}
