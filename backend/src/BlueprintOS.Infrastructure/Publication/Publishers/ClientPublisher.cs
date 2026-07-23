using BlueprintOS.Core.Documentation.Contracts.Client;
using BlueprintOS.Core.Documentation.Contracts.Executive;
using BlueprintOS.Core.Publication.Contracts;
using BlueprintOS.Core.Publication.Models;
using BlueprintOS.Infrastructure.Documentation;
using Microsoft.Extensions.Options;

namespace BlueprintOS.Infrastructure.Publication.Publishers;

/// <summary>
/// Implementação de <see cref="IReportPublisher"/> para o Guia do Cliente
/// (<c>dist/client/ClientGuide.*</c>): conteúdo funcional para clientes — visão geral,
/// funcionalidades, fluxo do processo, benefícios, casos de uso e roadmap funcional —, sem
/// nenhum detalhe de arquitetura, APIs, banco de dados ou componentes internos.
/// </summary>
public sealed class ClientPublisher : IReportPublisher
{
    private readonly IProductOverviewGenerator _productOverviewGenerator;
    private readonly IFunctionalGuideGenerator _functionalGuideGenerator;
    private readonly IUserGuideGenerator _userGuideGenerator;
    private readonly IRoadmapGenerator _roadmapGenerator;
    private readonly IReadOnlyList<IContentRenderer> _renderers;
    private readonly string _distRootPath;
    private readonly string _projectVersion;
    private readonly string _aiRootPath;

    public ClientPublisher(
        IProductOverviewGenerator productOverviewGenerator,
        IFunctionalGuideGenerator functionalGuideGenerator,
        IUserGuideGenerator userGuideGenerator,
        IRoadmapGenerator roadmapGenerator,
        IEnumerable<IContentRenderer> renderers,
        IOptions<PublicationOptions> publicationOptions,
        IOptions<DocumentationOptions> documentationOptions)
    {
        _productOverviewGenerator = productOverviewGenerator;
        _functionalGuideGenerator = functionalGuideGenerator;
        _userGuideGenerator = userGuideGenerator;
        _roadmapGenerator = roadmapGenerator;
        _renderers = renderers.ToList();
        _distRootPath = publicationOptions.Value.DistRootPath;
        _projectVersion = publicationOptions.Value.ProjectVersion;
        _aiRootPath = documentationOptions.Value.AiRootPath;
    }

    /// <inheritdoc />
    public string Category => "client";

    /// <inheritdoc />
    public async Task<IReadOnlyList<PublishedArtifact>> PublishAsync(CancellationToken cancellationToken = default)
    {
        var sections = new List<PublicationSection>
        {
            ReportPublishingHelper.BuildSection("Visão Geral", await _productOverviewGenerator.GenerateAsync(cancellationToken)),
            ReportPublishingHelper.BuildSection("Funcionalidades", await _functionalGuideGenerator.GenerateAsync(cancellationToken)),
            ReportPublishingHelper.BuildSection("Fluxo do Processo", BuildProcessFlowMarkdown()),
            ReportPublishingHelper.BuildSection(
                "Benefícios",
                await ReportPublishingHelper.BuildExpectedBenefitsMarkdownAsync(_aiRootPath, cancellationToken)),
            ReportPublishingHelper.BuildSection("Casos de Uso", await _userGuideGenerator.GenerateAsync(cancellationToken)),
            ReportPublishingHelper.BuildSection("Roadmap Funcional", await _roadmapGenerator.GenerateAsync(cancellationToken)),
        };

        var metadata = PublicationMetadata.Create(
            title: "Guia do Cliente — BlueprintOS",
            subtitle: "Visão funcional do produto, fluxo do processo e roadmap para clientes",
            audience: "Clientes",
            version: _projectVersion,
            generatedAt: DateTimeOffset.UtcNow,
            tags: new[] { "cliente", "produto", "guia" });

        var document = new PublicationDocument(
            Slug: "ClientGuide",
            Category: Category,
            Metadata: metadata,
            Sections: sections,
            Assets: PublicationAssets.Empty,
            Appendix: Array.Empty<PublicationSection>(),
            Theme: PublicationTheme.ForClient());

        return await ReportPublishingHelper.WriteAllFormatsAsync(document, Category, _distRootPath, _renderers, cancellationToken);
    }

    /// <summary>
    /// Descreve, em linguagem funcional (sem arquitetura ou nomes de classes), a ordem em que
    /// as capacidades reais dos módulos do BlueprintOS se encadeiam num processo de negócio.
    /// </summary>
    private static string BuildProcessFlowMarkdown() =>
        """
        O processo de negócio percorre as capacidades atuais do BlueprintOS nesta ordem:

        - **1. Conhecimento** — o conteúdo organizacional é ingerido e indexado para consulta.
        - **2. Documentação viva** — a documentação do próprio produto é gerada e mantida automaticamente a partir do estado real do projeto.
        - **3. Agentes de IA** — agentes especializados consultam o conhecimento indexado para apoiar decisões e responder perguntas.
        - **4. Negociação assistida** — para o processo de compras, um agente aplica estratégia de negociação com base em regras de negócio.
        """;
}
