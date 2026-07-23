using BlueprintOS.Core.Documentation.Contracts.Client;
using BlueprintOS.Core.Documentation.Contracts.Executive;
using BlueprintOS.Core.Publication.Contracts;
using BlueprintOS.Core.Publication.Models;
using Microsoft.Extensions.Options;

namespace BlueprintOS.Infrastructure.Publication.Publishers;

/// <summary>
/// Implementação de <see cref="IReportPublisher"/> para o Guia do Cliente
/// (<c>dist/client/ClientGuide.*</c>). O conteúdo estratégico (visão geral, valor de negócio,
/// plataforma, módulos, implantação, segurança, suporte, roadmap e próximos passos) é autorado
/// como Markdown em <c>.ai/content/client/</c> e carregado via <see cref="IClientContentLoader"/>;
/// este publisher é responsável apenas por acrescentar informações dinâmicas (versão, data de
/// geração, roadmap automático, métricas, anexos, QR Code, índice, capa e rodapé).
/// </summary>
public sealed class ClientPublisher : IReportPublisher
{
    private readonly IClientContentLoader _contentLoader;
    private readonly IRoadmapGenerator _roadmapGenerator;
    private readonly IQualityMetricsProvider _qualityMetricsProvider;
    private readonly IReadOnlyList<IContentRenderer> _renderers;
    private readonly string _distRootPath;
    private readonly string _projectVersion;

    public ClientPublisher(
        IClientContentLoader contentLoader,
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
    public string Category => "client";

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
            title: "Guia do Cliente — BlueprintOS",
            subtitle: "Visão de negócio, plataforma, módulos e roadmap para clientes",
            audience: "Clientes",
            version: _projectVersion,
            generatedAt: generatedAt,
            tags: new[] { "cliente", "produto", "guia" });

        var document = new PublicationDocument(
            Slug: "ClientGuide",
            Category: Category,
            Metadata: metadata,
            Sections: sections,
            Assets: ReportPublishingHelper.BuildStandardAssets(metrics),
            Appendix: ReportPublishingHelper.BuildStandardAppendix(metadata),
            Theme: PublicationTheme.ForClient());

        return await ReportPublishingHelper.WriteAllFormatsAsync(document, Category, _distRootPath, _renderers, cancellationToken);
    }
}
