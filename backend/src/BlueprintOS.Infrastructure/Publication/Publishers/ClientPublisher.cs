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
/// a montagem do documento (capa, índice, selos, métricas, QR Code, apêndice e rodapé) é
/// delegada ao <see cref="DocumentAssembler"/>. Este publisher só define o
/// <see cref="DocumentTemplate"/> e a única seção dinâmica específica (roadmap automático).
/// </summary>
public sealed class ClientPublisher : IReportPublisher
{
    private static readonly DocumentTemplate Template = new(
        Slug: "ClientGuide",
        Category: "client",
        Title: "Guia do Cliente — BlueprintOS",
        Subtitle: "Visão de negócio, plataforma, módulos e roadmap para clientes",
        Audience: "Clientes",
        Tags: new[] { "cliente", "produto", "guia" },
        Theme: PublicationTheme.ForClient());

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

        return await DocumentAssembler.AssembleAsync(
            Template,
            contentFiles.Select(f => (f.FileName, f.Content)).ToList(),
            dynamicSections,
            metrics,
            DateTimeOffset.UtcNow,
            _projectVersion,
            _distRootPath,
            _renderers,
            cancellationToken);
    }
}
