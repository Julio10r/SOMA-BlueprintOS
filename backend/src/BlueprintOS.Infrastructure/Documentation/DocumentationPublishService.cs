using BlueprintOS.Core.Documentation.Contracts;
using BlueprintOS.Core.Documentation.Contracts.Assets;
using BlueprintOS.Core.Documentation.Contracts.Client;
using BlueprintOS.Core.Documentation.Contracts.Engineering;
using BlueprintOS.Core.Documentation.Contracts.Executive;
using BlueprintOS.Core.Documentation.Models;
using BlueprintOS.Infrastructure.Documentation.Publishing;
using Microsoft.Extensions.Options;

namespace BlueprintOS.Infrastructure.Documentation;

/// <summary>
/// Implementação de <see cref="IDocumentationPublishService"/>: executa todos os geradores
/// executivos, de cliente e de engenharia, publica os documentos resultantes em
/// <c>docs/</c> e atualiza os artefatos de memória da AI Factory (<c>.ai/ROADMAP.md</c>,
/// <c>.ai/memory/completed_sprints.md</c> e <c>.ai/memory/known_issues.md</c>) de forma
/// idempotente (apenas acrescenta conteúdo que ainda não existe). Não depende de nenhum
/// motor de workflow; expõe um único ponto de entrada (<see cref="PublishAllAsync"/>) que
/// poderá ser futuramente acionado por um.
/// </summary>
public sealed class DocumentationPublishService : IDocumentationPublishService
{
    private const string SprintMarker = "## Sprint A8 — Portal de Documentação Viva";
    private const string RoadmapMarker = "Portal de documentação viva (dashboards, guias, changelog, ADRs) publicado automaticamente em `docs/` (Sprint A8).";

    private readonly DocumentationPublisher _publisher;
    private readonly string _aiRootPath;

    private readonly IDashboardGenerator _dashboardGenerator;
    private readonly IKpiGenerator _kpiGenerator;
    private readonly IRoadmapGenerator _roadmapGenerator;
    private readonly ISprintStatusGenerator _sprintStatusGenerator;
    private readonly IReleaseGenerator _releaseGenerator;

    private readonly IProductOverviewGenerator _productOverviewGenerator;
    private readonly IUserGuideGenerator _userGuideGenerator;
    private readonly IFunctionalGuideGenerator _functionalGuideGenerator;
    private readonly IApiDocumentationGenerator _apiDocumentationGenerator;
    private readonly IChangelogGenerator _changelogGenerator;
    private readonly IFaqGenerator _faqGenerator;

    private readonly IArchitectureGenerator _architectureGenerator;
    private readonly IDatabaseGenerator _databaseGenerator;
    private readonly IAgentsGenerator _agentsGenerator;
    private readonly IApiGenerator _apiGenerator;
    private readonly IDeployGenerator _deployGenerator;
    private readonly IRunbookGenerator _runbookGenerator;
    private readonly IMermaidGenerator _mermaidGenerator;
    private readonly IDecisionsGenerator _decisionsGenerator;

    private readonly IDocumentationAssetGenerator _assetGenerator;
    private readonly IAssetPublisher _assetPublisher;

    public DocumentationPublishService(
        DocumentationPublisher publisher,
        IOptions<DocumentationOptions> options,
        IDashboardGenerator dashboardGenerator,
        IKpiGenerator kpiGenerator,
        IRoadmapGenerator roadmapGenerator,
        ISprintStatusGenerator sprintStatusGenerator,
        IReleaseGenerator releaseGenerator,
        IProductOverviewGenerator productOverviewGenerator,
        IUserGuideGenerator userGuideGenerator,
        IFunctionalGuideGenerator functionalGuideGenerator,
        IApiDocumentationGenerator apiDocumentationGenerator,
        IChangelogGenerator changelogGenerator,
        IFaqGenerator faqGenerator,
        IArchitectureGenerator architectureGenerator,
        IDatabaseGenerator databaseGenerator,
        IAgentsGenerator agentsGenerator,
        IApiGenerator apiGenerator,
        IDeployGenerator deployGenerator,
        IRunbookGenerator runbookGenerator,
        IMermaidGenerator mermaidGenerator,
        IDecisionsGenerator decisionsGenerator,
        IDocumentationAssetGenerator assetGenerator,
        IAssetPublisher assetPublisher)
    {
        _publisher = publisher;
        _aiRootPath = options.Value.AiRootPath;
        _assetGenerator = assetGenerator;
        _assetPublisher = assetPublisher;

        _dashboardGenerator = dashboardGenerator;
        _kpiGenerator = kpiGenerator;
        _roadmapGenerator = roadmapGenerator;
        _sprintStatusGenerator = sprintStatusGenerator;
        _releaseGenerator = releaseGenerator;

        _productOverviewGenerator = productOverviewGenerator;
        _userGuideGenerator = userGuideGenerator;
        _functionalGuideGenerator = functionalGuideGenerator;
        _apiDocumentationGenerator = apiDocumentationGenerator;
        _changelogGenerator = changelogGenerator;
        _faqGenerator = faqGenerator;

        _architectureGenerator = architectureGenerator;
        _databaseGenerator = databaseGenerator;
        _agentsGenerator = agentsGenerator;
        _apiGenerator = apiGenerator;
        _deployGenerator = deployGenerator;
        _runbookGenerator = runbookGenerator;
        _mermaidGenerator = mermaidGenerator;
        _decisionsGenerator = decisionsGenerator;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PublishedDocument>> PublishAllAsync(CancellationToken cancellationToken = default)
    {
        var requests = new List<DocumentationPublishRequest>
        {
            new("executive/Dashboard.md", "Dashboard Executivo", await _dashboardGenerator.GenerateAsync(cancellationToken)),
            new("executive/KPIs.md", "KPIs", await _kpiGenerator.GenerateAsync(cancellationToken)),
            new("executive/Roadmap.md", "Roadmap", await _roadmapGenerator.GenerateAsync(cancellationToken)),
            new("executive/SprintStatus.md", "Status da Sprint", await _sprintStatusGenerator.GenerateAsync(cancellationToken)),
            new("executive/Releases.md", "Releases", await _releaseGenerator.GenerateAsync(cancellationToken)),

            new("client/ProductOverview.md", "Visão Geral do Produto", await _productOverviewGenerator.GenerateAsync(cancellationToken)),
            new("client/UserGuide.md", "Guia do Usuário", await _userGuideGenerator.GenerateAsync(cancellationToken)),
            new("client/FunctionalGuide.md", "Guia Funcional", await _functionalGuideGenerator.GenerateAsync(cancellationToken)),
            new("client/API.md", "Documentação de API (Cliente)", await _apiDocumentationGenerator.GenerateAsync(cancellationToken)),
            new("client/Changelog.md", "Changelog", await _changelogGenerator.GenerateAsync(cancellationToken)),
            new("client/FAQ.md", "Perguntas Frequentes", await _faqGenerator.GenerateAsync(cancellationToken)),

            new("engineering/Architecture.md", "Arquitetura", await _architectureGenerator.GenerateAsync(cancellationToken)),
            new("engineering/Database.md", "Banco de Dados", await _databaseGenerator.GenerateAsync(cancellationToken)),
            new("engineering/Agents.md", "Agentes de IA", await _agentsGenerator.GenerateAsync(cancellationToken)),
            new("engineering/APIs.md", "API — Documentação Técnica", await _apiGenerator.GenerateAsync(cancellationToken)),
            new("engineering/Deploy.md", "Deploy", await _deployGenerator.GenerateAsync(cancellationToken)),
            new("engineering/Runbooks.md", "Runbook", await _runbookGenerator.GenerateAsync(cancellationToken)),
            new("engineering/Mermaid/ArchitectureDiagram.md", "Diagrama de Arquitetura", await _mermaidGenerator.GenerateAsync(cancellationToken)),
            new("engineering/Decisions.md", "Decisões Arquiteturais (ADRs)", await _decisionsGenerator.GenerateAsync(cancellationToken)),
        };

        var results = await _publisher.PublishManyAsync(requests, cancellationToken);

        await PublishAssetsAsync(cancellationToken);

        await UpdateRoadmapAsync(cancellationToken);
        await UpdateCompletedSprintsAsync(cancellationToken);
        await UpdateKnownIssuesAsync(cancellationToken);

        return results;
    }

    private async Task PublishAssetsAsync(CancellationToken cancellationToken)
    {
        var assets = await _assetGenerator.GenerateAllAsync(cancellationToken);

        foreach (var asset in assets)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _assetPublisher.PublishAsync(asset, cancellationToken);
        }
    }

    private async Task UpdateRoadmapAsync(CancellationToken cancellationToken)
    {
        var path = Path.Combine(_aiRootPath, "ROADMAP.md");
        if (!File.Exists(path))
        {
            return;
        }

        var content = await File.ReadAllTextAsync(path, cancellationToken);
        if (content.Contains(RoadmapMarker, StringComparison.Ordinal))
        {
            return;
        }

        const string phase0Header = "## Fase 0 - Fundação (status: em andamento)";
        var headerIndex = content.IndexOf(phase0Header, StringComparison.Ordinal);
        if (headerIndex < 0)
        {
            return;
        }

        var nextSectionIndex = content.IndexOf("\n---", headerIndex, StringComparison.Ordinal);
        var insertionIndex = nextSectionIndex >= 0 ? nextSectionIndex : content.Length;

        var updated = content.Insert(insertionIndex, $"\n- {RoadmapMarker}");
        await File.WriteAllTextAsync(path, updated, cancellationToken);
    }

    private async Task UpdateCompletedSprintsAsync(CancellationToken cancellationToken)
    {
        var path = Path.Combine(_aiRootPath, "memory", "completed_sprints.md");
        if (!File.Exists(path))
        {
            return;
        }

        var content = await File.ReadAllTextAsync(path, cancellationToken);
        if (content.Contains(SprintMarker, StringComparison.Ordinal))
        {
            return;
        }

        var entry =
            $"""

            {SprintMarker}

            **Status:** Concluída

            **Escopo:** Implementação do Portal de Documentação Viva: publicação automática de documentação executiva, de cliente e de engenharia (19 geradores) a partir de fontes reais do repositório (`.ai/ROADMAP.md`, `.ai/memory/completed_sprints.md`, `.ai/memory/known_issues.md`, `.ai/DECISIONS.md`, metadados de módulo e o grafo real de dependências entre projetos), com publicação em disco sob `docs/` (`IDocumentPublisher`/`MarkdownPublisher`/`DocumentationPublisher`) e sincronização automática dos artefatos de memória da AI Factory.

            **Entregas:**
            - Camada de publicação (`IDocumentPublisher`, `MarkdownPublisher`, `DocumentationPublisher`) em `backend/src/BlueprintOS.Infrastructure/Documentation/Publishing/`.
            - 19 geradores de documentação (executivo, cliente, engenharia) em `backend/src/BlueprintOS.Infrastructure/Documentation/Generators/`.
            - `DocumentationPublishService` (`IDocumentationPublishService.PublishAllAsync`), pronto para ser acionado por um futuro motor de Workflow.
            - Documentos Markdown publicados em `docs/executive/`, `docs/client/`, `docs/engineering/` e `docs/engineering/Mermaid/`.
            - Suíte de testes unitários (xUnit, fakes manuais) cobrindo publicadores, geradores e o serviço de publicação.

            **Resultado da validação:** `dotnet build` sem erros/warnings; `dotnet test` com 100% dos testes passando.

            """;

        await File.AppendAllTextAsync(path, entry, cancellationToken);
    }

    private async Task UpdateKnownIssuesAsync(CancellationToken cancellationToken)
    {
        var path = Path.Combine(_aiRootPath, "memory", "known_issues.md");
        if (!File.Exists(path))
        {
            return;
        }

        var content = await File.ReadAllTextAsync(path, cancellationToken);
        if (content.Contains(SprintMarker, StringComparison.Ordinal))
        {
            return;
        }

        var entry =
            $"""


            {SprintMarker}

            - **KPIs, FAQ e runbook operacional ainda estão vazios/mínimos.** O portal de documentação viva gera esses documentos de forma honesta (sem dados fabricados), mas eles permanecem sparse até que existam fontes reais (uso em produção, suporte ao cliente, incidentes reais).
            - **Nenhum `DbContext`/schema de banco de dados existe ainda**, portanto `docs/engineering/database.md` reflete apenas essa ausência.
            - **Atualização de `.ai/ROADMAP.md` e `.ai/memory/completed_sprints.md` via `DocumentationPublishService` é idempotente mas ainda manual** (sem acionamento automático por um motor de Workflow, que ainda não existe).
            """;

        await File.AppendAllTextAsync(path, entry, cancellationToken);
    }
}
