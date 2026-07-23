using BlueprintOS.Core.Documentation.Contracts.Engineering;
using BlueprintOS.Core.Documentation.Contracts.Executive;
using BlueprintOS.Core.Publication.Contracts;
using BlueprintOS.Core.Publication.Models;
using Microsoft.Extensions.Options;

namespace BlueprintOS.Infrastructure.Publication.Publishers;

/// <summary>
/// Implementação de <see cref="IReportPublisher"/> para o Relatório Executivo
/// (<c>dist/executive/ExecutiveReport.*</c>). O conteúdo estratégico (visão, problema de
/// negócio, capacidades, benefícios, roadmap narrativo, estado atual e próximos passos) é
/// autorado como Markdown em <c>.ai/content/executive/</c> e carregado via
/// <see cref="IExecutiveContentLoader"/>; a montagem do documento (capa, resumo executivo,
/// índice, tema, selos, QR Code, apêndice e rodapé) é delegada ao <see cref="DocumentAssembler"/>
/// e ao <see cref="IDocumentationAssetsManager"/> — este publisher não acessa nenhum asset
/// diretamente. Só define o <see cref="DocumentTemplate"/>, o roadmap automático e o diagrama de
/// arquitetura (renderizado para imagem, nunca publicado como código Mermaid).
/// </summary>
public sealed class ExecutivePublisher : IReportPublisher
{
    private static readonly DocumentTemplate Template = new(
        Slug: "ExecutiveReport",
        Category: "executive",
        Title: "Relatório Executivo — BlueprintOS",
        Subtitle: "Visão estratégica de negócio, capacidades e roadmap para a diretoria",
        Audience: "Diretoria",
        Tags: new[] { "executivo", "estratégia", "roadmap" },
        DocumentClass: PublicationDocumentClass.Executive);

    private readonly IExecutiveContentLoader _contentLoader;
    private readonly IRoadmapGenerator _roadmapGenerator;
    private readonly IMermaidGenerator _mermaidGenerator;
    private readonly IQualityMetricsProvider _qualityMetricsProvider;
    private readonly IDocumentationAssetsManager _assetsManager;
    private readonly IReadOnlyList<IContentRenderer> _renderers;
    private readonly string _distRootPath;
    private readonly string _projectVersion;

    public ExecutivePublisher(
        IExecutiveContentLoader contentLoader,
        IRoadmapGenerator roadmapGenerator,
        IMermaidGenerator mermaidGenerator,
        IQualityMetricsProvider qualityMetricsProvider,
        IDocumentationAssetsManager assetsManager,
        IEnumerable<IContentRenderer> renderers,
        IOptions<PublicationOptions> publicationOptions)
    {
        _contentLoader = contentLoader;
        _roadmapGenerator = roadmapGenerator;
        _mermaidGenerator = mermaidGenerator;
        _qualityMetricsProvider = qualityMetricsProvider;
        _assetsManager = assetsManager;
        _renderers = renderers.ToList();
        _distRootPath = publicationOptions.Value.DistRootPath;
        _projectVersion = publicationOptions.Value.ProjectVersion;
    }

    /// <inheritdoc />
    public string Category => Template.Category;

    /// <inheritdoc />
    public async Task<IReadOnlyList<PublishedArtifact>> PublishAsync(CancellationToken cancellationToken = default)
    {
        var metrics = await _qualityMetricsProvider.GetMetricsAsync(cancellationToken);
        var contentFiles = await _contentLoader.LoadAsync(cancellationToken);

        var dynamicSections = new[]
        {
            new DocumentSection("Roadmap Automático", _roadmapGenerator.GenerateAsync),
        };

        var diagram = await _assetsManager.RenderDiagramAsync(
            "Visão de Arquitetura", "diagram-architecture", _mermaidGenerator.GenerateAsync, cancellationToken);

        return await DocumentAssembler.AssembleAsync(
            Template,
            contentFiles.Select(f => (f.FileName, f.Content)).ToList(),
            dynamicSections,
            new[] { diagram },
            _assetsManager,
            metrics,
            DateTimeOffset.UtcNow,
            _projectVersion,
            _distRootPath,
            _renderers,
            cancellationToken);
    }
}
