using BlueprintOS.Core.Publication.Contracts;
using BlueprintOS.Core.Publication.Models;
using BlueprintOS.Infrastructure.Publication.Content;
using BlueprintOS.Infrastructure.Publication.Publishers;
using Microsoft.Extensions.Options;

namespace BlueprintOS.Infrastructure.Publication.Docs;

/// <summary>
/// Único ponto de entrada do Publication Engine a partir da ADR-0019: descobre os documentos
/// Markdown de <see cref="PublicationOptions.DocsRootPath"/>, monta um
/// <see cref="PublicationDocument"/> genérico por arquivo (sem nenhuma lógica de audiência) e
/// delega a renderização/gravação em <see cref="PublicationOptions.DistRootPath"/> aos
/// <see cref="IContentRenderer"/> existentes via <see cref="ReportPublishingHelper"/>. Substitui
/// conceitualmente <c>ExecutivePublisher</c>, <c>ClientPublisher</c>, <c>EngineeringPublisher</c>
/// e <c>ExecutiveBlueprintPublisher</c> — um único publisher funciona para qualquer diretório
/// presente ou futuro em <c>docs/</c>.
/// </summary>
public sealed class DocsPublisher : IPublicationService
{
    private readonly IDocsDiscoveryService _discovery;
    private readonly IDocumentationAssetsManager _assetsManager;
    private readonly IQualityMetricsProvider _qualityMetricsProvider;
    private readonly IReadOnlyList<IContentRenderer> _renderers;
    private readonly PublicationOptions _options;

    public DocsPublisher(
        IDocsDiscoveryService discovery,
        IDocumentationAssetsManager assetsManager,
        IQualityMetricsProvider qualityMetricsProvider,
        IEnumerable<IContentRenderer> renderers,
        IOptions<PublicationOptions> options)
    {
        _discovery = discovery;
        _assetsManager = assetsManager;
        _qualityMetricsProvider = qualityMetricsProvider;
        _renderers = renderers.ToList();
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PublishedArtifact>> PublishAllAsync(CancellationToken cancellationToken = default)
    {
        _options.ValidateSafePaths();

        var discovered = _discovery.Discover(_options.DocsRootPath);
        var generatedAt = DateTimeOffset.UtcNow;
        var theme = _assetsManager.GetTheme(PublicationDocumentClass.Engineering);

        // Coletado uma única vez por execução (não por documento) — dotnet build é custoso e o
        // indicador é o mesmo, independentemente de quantos documentos existam em docs/.
        var metrics = await _qualityMetricsProvider.GetMetricsAsync(cancellationToken);
        var sharedAssets = _assetsManager.BuildStandardAssets(metrics);

        var artifacts = new List<PublishedArtifact>();
        foreach (var doc in discovered)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var document = await BuildDocumentAsync(doc, theme, sharedAssets, generatedAt, cancellationToken);
                var written = await ReportPublishingHelper.WriteAllFormatsAsync(
                    document, doc.Category, _options.DistRootPath, _renderers, cancellationToken);
                artifacts.AddRange(written);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Falha isolada: um documento com problema (Markdown inválido, renderer
                // instável) não pode interromper a publicação dos demais.
                Console.Error.WriteLine($"Falha ao publicar '{doc.RelativePath}': {ex.Message}");
            }
        }

        try
        {
            var index = BuildIndexDocument(discovered, theme, generatedAt);
            var indexArtifacts = await ReportPublishingHelper.WriteAllFormatsAsync(
                index, category: string.Empty, _options.DistRootPath, _renderers, cancellationToken);
            artifacts.AddRange(indexArtifacts);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Console.Error.WriteLine($"Falha ao publicar o índice: {ex.Message}");
        }

        return artifacts;
    }

    private static async Task<PublicationDocument> BuildDocumentAsync(
        DiscoveredDocument doc,
        PublicationTheme theme,
        PublicationAssets sharedAssets,
        DateTimeOffset generatedAt,
        CancellationToken cancellationToken)
    {
        var rawContent = await File.ReadAllTextAsync(doc.AbsolutePath, cancellationToken);
        var (heading, body) = ReportPublishingHelper.SplitHeading(rawContent);
        var title = string.IsNullOrWhiteSpace(heading) || heading == "Seção" ? doc.Slug : heading;

        var blocks = string.IsNullOrWhiteSpace(body)
            ? new[] { ContentBlock.Paragraph("Documento sem conteúdo.") }
            : MarkdownContentParser.Parse(body);

        var section = new PublicationSection(title, blocks);
        var category = string.IsNullOrEmpty(doc.Category) ? "docs" : doc.Category;

        var metadata = PublicationMetadata.Create(
            title: title,
            subtitle: category,
            audience: category,
            version: "1.0.0",
            generatedAt: generatedAt,
            tags: new[] { category });

        return new PublicationDocument(
            Slug: doc.Slug,
            Category: doc.Category,
            Metadata: metadata,
            Sections: new[] { section },
            Assets: sharedAssets,
            Appendix: Array.Empty<PublicationSection>(),
            Theme: theme);
    }

    /// <summary>
    /// Monta o índice navegável de <c>dist/</c>: apenas a árvore de áreas e documentos
    /// descobertos, com link para cada artefato publicado — nenhum conteúdo editorial é
    /// inventado.
    /// </summary>
    private static PublicationDocument BuildIndexDocument(
        IReadOnlyList<DiscoveredDocument> discovered,
        PublicationTheme theme,
        DateTimeOffset generatedAt)
    {
        var byCategory = discovered
            .GroupBy(d => string.IsNullOrEmpty(d.Category) ? "(raiz)" : d.Category)
            .OrderBy(g => g.Key, StringComparer.Ordinal);

        var sections = new List<PublicationSection>();
        foreach (var group in byCategory)
        {
            var items = group
                .OrderBy(d => d.Slug, StringComparer.Ordinal)
                .Select(d => $"[{d.Slug}]({d.RelativePath})")
                .ToArray();

            sections.Add(new PublicationSection(group.Key, new[] { ContentBlock.BulletList(items) }));
        }

        var metadata = PublicationMetadata.Create(
            title: "Índice da Documentação Técnica",
            subtitle: "Áreas e documentos publicados a partir de docs/",
            audience: "Técnico",
            version: "1.0.0",
            generatedAt: generatedAt);

        return new PublicationDocument(
            Slug: "index",
            Category: string.Empty,
            Metadata: metadata,
            Sections: sections,
            Assets: PublicationAssets.Empty,
            Appendix: Array.Empty<PublicationSection>(),
            Theme: theme);
    }
}
