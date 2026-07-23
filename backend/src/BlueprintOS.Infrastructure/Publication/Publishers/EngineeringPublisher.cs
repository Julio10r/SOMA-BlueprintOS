using BlueprintOS.Core.Documentation.Contracts.Engineering;
using BlueprintOS.Core.Documentation.Contracts.Executive;
using BlueprintOS.Core.Publication.Contracts;
using BlueprintOS.Core.Publication.Models;
using Microsoft.Extensions.Options;

namespace BlueprintOS.Infrastructure.Publication.Publishers;

/// <summary>
/// Implementação de <see cref="IReportPublisher"/> para o Guia de Engenharia
/// (<c>dist/engineering/EngineeringGuide.*</c>). O conteúdo técnico (visão técnica, arquitetura,
/// componentes, runtime de IA, memória, conhecimento, desenvolvimento, DevOps, testes, segurança,
/// roadmap técnico e próximos passos) é autorado como Markdown em
/// <c>.ai/content/engineering/</c> e carregado via <see cref="IEngineeringContentLoader"/>; este
/// publisher é responsável apenas por acrescentar informações dinâmicas (versão, data de geração,
/// roadmap automático, badges, métricas, QR Code, anexos, índice, capa e rodapé).
/// </summary>
public sealed class EngineeringPublisher : IReportPublisher
{
    private readonly IEngineeringContentLoader _contentLoader;
    private readonly IRoadmapGenerator _roadmapGenerator;
    private readonly IQualityMetricsProvider _qualityMetricsProvider;
    private readonly IReadOnlyList<IContentRenderer> _renderers;
    private readonly string _distRootPath;
    private readonly string _projectVersion;

    public EngineeringPublisher(
        IEngineeringContentLoader contentLoader,
        IRoadmapGenerator roadmapGenerator,
        IQualityMetricsProvider qualityMetricsProvider,
        IEnumerable<IContentRenderer> renderers,
        IOptions<PublicationOptions> publicationOptions)
    {
        _contentLoader = contentLoader;
        _roadmapGenerator = roadmapGenerator;
        _qualityMetricsProvider = qualityMetricsProvider;
        _renderers = renderers.ToList();
        _distRootPath = publicationOptions.Value.DistRootPath;
        _projectVersion = publicationOptions.Value.ProjectVersion;
    }

    /// <inheritdoc />
    public string Category => "engineering";

    /// <inheritdoc />
    public async Task<IReadOnlyList<PublishedArtifact>> PublishAsync(CancellationToken cancellationToken = default)
    {
        var metrics = await _qualityMetricsProvider.GetMetricsAsync(cancellationToken);
        var generatedAt = DateTimeOffset.UtcNow;

        var contentFiles = await _contentLoader.LoadAsync(cancellationToken);
        var sections = new List<PublicationSection>(contentFiles.Count + 1);
        foreach (var file in contentFiles)
        {
            var (heading, body) = ReportPublishingHelper.SplitHeading(file.Content);
            sections.Add(ReportPublishingHelper.BuildSection(heading, body));
        }

        sections.Add(ReportPublishingHelper.BuildSection(
            "Roadmap Automático",
            await _roadmapGenerator.GenerateAsync(cancellationToken)));

        var metadata = PublicationMetadata.Create(
            title: "Guia de Engenharia — BlueprintOS",
            subtitle: "Visão técnica, arquitetura, componentes e roadmap técnico da plataforma",
            audience: "Equipe de Engenharia",
            version: _projectVersion,
            generatedAt: generatedAt,
            tags: new[] { "engenharia", "arquitetura", "técnico" });

        var document = new PublicationDocument(
            Slug: "EngineeringGuide",
            Category: Category,
            Metadata: metadata,
            Sections: sections,
            Assets: ReportPublishingHelper.BuildStandardAssets(metrics),
            Appendix: ReportPublishingHelper.BuildStandardAppendix(metadata),
            Theme: PublicationTheme.ForEngineering());

        return await ReportPublishingHelper.WriteAllFormatsAsync(document, Category, _distRootPath, _renderers, cancellationToken);
    }
}
