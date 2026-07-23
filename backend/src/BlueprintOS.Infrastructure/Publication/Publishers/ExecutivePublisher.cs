using System.Text;
using BlueprintOS.Core.Documentation.Contracts.Engineering;
using BlueprintOS.Core.Documentation.Contracts.Executive;
using BlueprintOS.Core.Publication.Contracts;
using BlueprintOS.Core.Publication.Models;
using BlueprintOS.Core.Publication.Models.Assets;
using BlueprintOS.Infrastructure.Publication.Content;
using Microsoft.Extensions.Options;

namespace BlueprintOS.Infrastructure.Publication.Publishers;

/// <summary>
/// Implementação de <see cref="IReportPublisher"/> para o Relatório Executivo
/// (<c>dist/executive/ExecutiveReport.*</c>). O conteúdo estratégico (visão, problema de
/// negócio, capacidades, benefícios, roadmap narrativo, estado atual e próximos passos) é
/// autorado como Markdown em <c>.ai/content/executive/</c> e carregado via
/// <see cref="IExecutiveContentLoader"/>; este publisher é responsável apenas por acrescentar
/// informações dinâmicas (versão, data de geração, roadmap automático, diagrama de arquitetura,
/// indicadores, anexos, índice, capa e rodapé).
/// </summary>
public sealed class ExecutivePublisher : IReportPublisher
{
    private const string RepositoryUrl = "https://github.com/Julio10r/SOMA-BlueprintOS";

    private readonly IExecutiveContentLoader _contentLoader;
    private readonly IRoadmapGenerator _roadmapGenerator;
    private readonly IMermaidGenerator _mermaidGenerator;
    private readonly IQualityMetricsProvider _qualityMetricsProvider;
    private readonly IReadOnlyList<IContentRenderer> _renderers;
    private readonly string _distRootPath;
    private readonly string _projectVersion;

    public ExecutivePublisher(
        IExecutiveContentLoader contentLoader,
        IRoadmapGenerator roadmapGenerator,
        IMermaidGenerator mermaidGenerator,
        IQualityMetricsProvider qualityMetricsProvider,
        IEnumerable<IContentRenderer> renderers,
        IOptions<PublicationOptions> publicationOptions)
    {
        _contentLoader = contentLoader;
        _roadmapGenerator = roadmapGenerator;
        _mermaidGenerator = mermaidGenerator;
        _qualityMetricsProvider = qualityMetricsProvider;
        _renderers = renderers.ToList();
        _distRootPath = publicationOptions.Value.DistRootPath;
        _projectVersion = publicationOptions.Value.ProjectVersion;
    }

    /// <inheritdoc />
    public string Category => "executive";

    /// <inheritdoc />
    public async Task<IReadOnlyList<PublishedArtifact>> PublishAsync(CancellationToken cancellationToken = default)
    {
        var metrics = await _qualityMetricsProvider.GetMetricsAsync(cancellationToken);
        var generatedAt = DateTimeOffset.UtcNow;

        var contentFiles = await _contentLoader.LoadAsync(cancellationToken);
        var sections = new List<PublicationSection>(contentFiles.Count + 2);
        foreach (var file in contentFiles)
        {
            var (heading, body) = ReportPublishingHelper.SplitHeading(file.Content);
            sections.Add(ReportPublishingHelper.BuildSection(heading, body));
        }

        sections.Add(ReportPublishingHelper.BuildSection(
            "Roadmap Automático",
            await _roadmapGenerator.GenerateAsync(cancellationToken)));
        sections.Add(ReportPublishingHelper.BuildSection(
            "Visão de Arquitetura",
            ReportPublishingHelper.StripFirstHeadingLine(await _mermaidGenerator.GenerateAsync(cancellationToken))));

        var metadata = PublicationMetadata.Create(
            title: "Relatório Executivo — BlueprintOS",
            subtitle: "Visão estratégica de negócio, capacidades e roadmap para a diretoria",
            audience: "Diretoria",
            version: _projectVersion,
            generatedAt: generatedAt,
            tags: new[] { "executivo", "estratégia", "roadmap" });

        var document = new PublicationDocument(
            Slug: "ExecutiveReport",
            Category: Category,
            Metadata: metadata,
            Sections: sections,
            Assets: BuildAssets(metrics),
            Appendix: BuildAppendix(metadata),
            Theme: PublicationTheme.ForExecutive());

        return await ReportPublishingHelper.WriteAllFormatsAsync(document, Category, _distRootPath, _renderers, cancellationToken);
    }

    private static PublicationAssets BuildAssets(QualityMetrics metrics)
    {
        var badges = new List<BadgeAsset>
        {
            new(
                "badge-build",
                "Build",
                metrics.BuildSucceeded ? "passing" : "failing",
                metrics.BuildSucceeded ? BadgeStatus.Success : BadgeStatus.Failure),
            new(
                "badge-tests",
                "Testes",
                metrics.TestCount.ToString(),
                metrics.TestCount > 0 ? BadgeStatus.Success : BadgeStatus.Neutral),
        };

        var qrCode = new QrCodeAsset(
            "qr-repository",
            RepositoryUrl,
            "Repositório no GitHub",
            QrCodeImageGenerator.GeneratePng(RepositoryUrl));

        return PublicationAssets.Empty with { Badges = badges, QrCodes = new[] { qrCode } };
    }

    private static IReadOnlyList<PublicationSection> BuildAppendix(PublicationMetadata metadata)
    {
        var builder = new StringBuilder();
        builder.AppendLine("| Versão | Data | Autor | Resumo |");
        builder.AppendLine("|---|---|---|---|");
        foreach (var revision in metadata.RevisionHistory)
        {
            builder.AppendLine($"| {revision.Version} | {revision.Date:yyyy-MM-dd} | {revision.Author} | {revision.Summary} |");
        }

        var repositorySection = new PublicationSection(
            "Repositório",
            new[]
            {
                ContentBlock.Paragraph($"Código-fonte do BlueprintOS: {RepositoryUrl}"),
                ContentBlock.Image("qr-repository", "Acesse o repositório escaneando o QR Code."),
            });

        return new[]
        {
            ReportPublishingHelper.BuildSection("Histórico de Versões", builder.ToString()),
            repositorySection,
        };
    }
}
