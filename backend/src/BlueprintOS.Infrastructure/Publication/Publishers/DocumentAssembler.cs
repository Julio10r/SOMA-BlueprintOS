using BlueprintOS.Core.Publication.Contracts;
using BlueprintOS.Core.Publication.Models;

namespace BlueprintOS.Infrastructure.Publication.Publishers;

/// <summary>
/// Motor único de montagem e publicação de documentos (Template Engine): dado um
/// <see cref="DocumentTemplate"/> (o que é fixo do documento), o conteúdo autoral carregado de
/// <c>.ai/content/{categoria}/</c>, as poucas <see cref="DocumentSection"/> dinâmicas que o
/// publisher ainda gera em código (roadmap automático, diagrama, etc.) e o
/// <see cref="IDocumentationAssetsManager"/> (tema, selos, QR Code, apêndice), monta o
/// <see cref="PublicationDocument"/> completo — capa, índice, cores, ativos e apêndice
/// inclusos — e o grava em disco em todos os formatos.
/// </summary>
/// <remarks>
/// Introduzido para eliminar a duplicação que existia entre <c>ExecutivePublisher</c>,
/// <c>ClientPublisher</c> e <c>EngineeringPublisher</c>: os três montavam, de forma quase
/// idêntica, seções a partir do conteúdo autoral, metadados, selos/QR Code e apêndice. Com este
/// assembler, um novo documento publicado passa a exigir apenas um <see cref="DocumentTemplate"/>
/// e a lista de seções dinâmicas específicas — sem repetir a lógica de montagem, e sem que
/// nenhum publisher acesse assets diretamente.
/// </remarks>
internal static class DocumentAssembler
{
    /// <summary>
    /// Monta o documento a partir do template, do conteúdo autoral (na ordem em que foi
    /// carregado) e das seções dinâmicas informadas, e grava os artefatos em disco. Tema,
    /// selos/QR Code e apêndice são obtidos exclusivamente via <paramref name="assetsManager"/>.
    /// </summary>
    public static async Task<IReadOnlyList<PublishedArtifact>> AssembleAsync(
        DocumentTemplate template,
        IReadOnlyList<(string FileName, string Content)> contentFiles,
        IReadOnlyList<DocumentSection> dynamicSections,
        IDocumentationAssetsManager assetsManager,
        QualityMetrics metrics,
        DateTimeOffset generatedAt,
        string projectVersion,
        string distRootPath,
        IReadOnlyList<IContentRenderer> renderers,
        CancellationToken cancellationToken = default)
    {
        var sections = new List<PublicationSection>(contentFiles.Count + dynamicSections.Count);
        foreach (var file in contentFiles)
        {
            var (heading, body) = ReportPublishingHelper.SplitHeading(file.Content);
            sections.Add(ReportPublishingHelper.BuildSection(heading, body));
        }

        foreach (var dynamicSection in dynamicSections)
        {
            var markdown = await dynamicSection.BuildMarkdownAsync(cancellationToken);
            sections.Add(ReportPublishingHelper.BuildSection(dynamicSection.Title, markdown));
        }

        var metadata = PublicationMetadata.Create(
            title: template.Title,
            subtitle: template.Subtitle,
            audience: template.Audience,
            version: projectVersion,
            generatedAt: generatedAt,
            tags: template.Tags);

        var document = new PublicationDocument(
            Slug: template.Slug,
            Category: template.Category,
            Metadata: metadata,
            Sections: sections,
            Assets: assetsManager.BuildStandardAssets(metrics),
            Appendix: assetsManager.BuildStandardAppendix(metadata),
            Theme: assetsManager.GetTheme(template.DocumentClass));

        return await ReportPublishingHelper.WriteAllFormatsAsync(
            document, template.Category, distRootPath, renderers, cancellationToken);
    }
}
