using System.Text;
using System.Text.RegularExpressions;
using BlueprintOS.Core.Documentation.Contracts.Client;
using BlueprintOS.Core.Documentation.Contracts.Executive;
using BlueprintOS.Core.Publication.Contracts;
using BlueprintOS.Core.Publication.Models;
using BlueprintOS.Core.Publication.Models.Assets;
using BlueprintOS.Infrastructure.Documentation;
using BlueprintOS.Infrastructure.Publication.Content;
using Microsoft.Extensions.Options;

namespace BlueprintOS.Infrastructure.Publication.Publishers;

/// <summary>
/// Implementação de <see cref="IReportPublisher"/> para o Relatório Executivo
/// (<c>dist/executive/ExecutiveReport.*</c>): conteúdo estratégico para a diretoria — resumo,
/// objetivos, valor de negócio, benefícios esperados, KPIs, roadmap e próximos passos —, sem
/// nenhum detalhe técnico (APIs, banco, classes, código ou eventos).
/// </summary>
public sealed class ExecutivePublisher : IReportPublisher
{
    private const string RepositoryUrl = "https://github.com/Julio10r/SOMA-BlueprintOS";

    private static readonly Regex PendingPhasePattern =
        new(@"^##\s+(Fase .+\(status:\s*(?!concluíd).+\))$", RegexOptions.Multiline | RegexOptions.IgnoreCase);

    private readonly IDashboardGenerator _dashboardGenerator;
    private readonly IProductOverviewGenerator _productOverviewGenerator;
    private readonly IFunctionalGuideGenerator _functionalGuideGenerator;
    private readonly IKpiGenerator _kpiGenerator;
    private readonly IRoadmapGenerator _roadmapGenerator;
    private readonly IQualityMetricsProvider _qualityMetricsProvider;
    private readonly IReadOnlyList<IContentRenderer> _renderers;
    private readonly string _distRootPath;
    private readonly string _projectVersion;
    private readonly string _aiRootPath;

    public ExecutivePublisher(
        IDashboardGenerator dashboardGenerator,
        IProductOverviewGenerator productOverviewGenerator,
        IFunctionalGuideGenerator functionalGuideGenerator,
        IKpiGenerator kpiGenerator,
        IRoadmapGenerator roadmapGenerator,
        IQualityMetricsProvider qualityMetricsProvider,
        IEnumerable<IContentRenderer> renderers,
        IOptions<PublicationOptions> publicationOptions,
        IOptions<DocumentationOptions> documentationOptions)
    {
        _dashboardGenerator = dashboardGenerator;
        _productOverviewGenerator = productOverviewGenerator;
        _functionalGuideGenerator = functionalGuideGenerator;
        _kpiGenerator = kpiGenerator;
        _roadmapGenerator = roadmapGenerator;
        _qualityMetricsProvider = qualityMetricsProvider;
        _renderers = renderers.ToList();
        _distRootPath = publicationOptions.Value.DistRootPath;
        _projectVersion = publicationOptions.Value.ProjectVersion;
        _aiRootPath = documentationOptions.Value.AiRootPath;
    }

    /// <inheritdoc />
    public string Category => "executive";

    /// <inheritdoc />
    public async Task<IReadOnlyList<PublishedArtifact>> PublishAsync(CancellationToken cancellationToken = default)
    {
        var metrics = await _qualityMetricsProvider.GetMetricsAsync(cancellationToken);
        var generatedAt = DateTimeOffset.UtcNow;

        var sections = new List<PublicationSection>
        {
            ReportPublishingHelper.BuildSection("Resumo Executivo", await _dashboardGenerator.GenerateAsync(cancellationToken)),
            ReportPublishingHelper.BuildSection(
                "Objetivos",
                ReportPublishingHelper.StripFirstHeadingLine(await _productOverviewGenerator.GenerateAsync(cancellationToken))),
            ReportPublishingHelper.BuildSection(
                "Valor de Negócio",
                ReportPublishingHelper.StripFirstHeadingLine(await _functionalGuideGenerator.GenerateAsync(cancellationToken))),
            ReportPublishingHelper.BuildSection(
                "Benefícios Esperados",
                await ReportPublishingHelper.BuildExpectedBenefitsMarkdownAsync(_aiRootPath, cancellationToken)),
            ReportPublishingHelper.BuildSection(
                "KPIs",
                ReportPublishingHelper.StripFirstHeadingLine(await _kpiGenerator.GenerateAsync(cancellationToken))),
            ReportPublishingHelper.BuildSection("Roadmap", await _roadmapGenerator.GenerateAsync(cancellationToken)),
            ReportPublishingHelper.BuildSection("Próximos Passos", await BuildNextStepsSectionAsync(cancellationToken)),
        };

        var metadata = PublicationMetadata.Create(
            title: "Relatório Executivo — BlueprintOS",
            subtitle: "Visão estratégica de objetivos, valor de negócio e roadmap para a diretoria",
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

    private async Task<string> BuildNextStepsSectionAsync(CancellationToken cancellationToken)
    {
        var path = Path.Combine(_aiRootPath, "ROADMAP.md");
        if (!File.Exists(path))
        {
            return "Roadmap não encontrado; próximos passos não puderam ser derivados.";
        }

        var content = await File.ReadAllTextAsync(path, cancellationToken);
        var matches = PendingPhasePattern.Matches(content);
        if (matches.Count == 0)
        {
            return "Nenhuma fase pendente identificada em `.ai/ROADMAP.md`.";
        }

        var builder = new StringBuilder();
        builder.AppendLine("Próximas fases do roadmap ainda não concluídas:");
        builder.AppendLine();
        foreach (Match match in matches)
        {
            builder.AppendLine($"- {match.Groups[1].Value.Trim()}");
        }

        return builder.ToString();
    }
}
