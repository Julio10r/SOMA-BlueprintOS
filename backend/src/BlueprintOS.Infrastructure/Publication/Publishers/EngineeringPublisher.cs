using System.Text;
using System.Text.RegularExpressions;
using BlueprintOS.Core.Documentation.Contracts.Engineering;
using BlueprintOS.Core.Publication.Contracts;
using BlueprintOS.Core.Publication.Models;
using Microsoft.Extensions.Options;

namespace BlueprintOS.Infrastructure.Publication.Publishers;

/// <summary>
/// Implementação de <see cref="IReportPublisher"/> para o Guia de Engenharia
/// (<c>dist/engineering/EngineeringGuide.*</c>): documentação técnica completa — arquitetura,
/// componentes, banco de dados, APIs, eventos, estrutura do código, dependências e decisões
/// técnicas.
/// </summary>
public sealed class EngineeringPublisher : IReportPublisher
{
    private static readonly Regex ProjectPattern = new(
        "Project\\(\"\\{[0-9A-Fa-f-]+\\}\"\\)\\s*=\\s*\"([^\"]+)\",\\s*\"([^\"]+)\"",
        RegexOptions.Compiled);

    private static readonly Regex ProjectReferencePattern = new(
        "<ProjectReference\\s+Include=\"([^\"]+)\"",
        RegexOptions.Compiled);

    private readonly IArchitectureGenerator _architectureGenerator;
    private readonly IDatabaseGenerator _databaseGenerator;
    private readonly IAgentsGenerator _agentsGenerator;
    private readonly IApiGenerator _apiGenerator;
    private readonly IMermaidGenerator _mermaidGenerator;
    private readonly IDecisionsGenerator _decisionsGenerator;
    private readonly IReadOnlyList<IContentRenderer> _renderers;
    private readonly string _distRootPath;
    private readonly string _solutionPath;
    private readonly string _projectVersion;

    public EngineeringPublisher(
        IArchitectureGenerator architectureGenerator,
        IDatabaseGenerator databaseGenerator,
        IAgentsGenerator agentsGenerator,
        IApiGenerator apiGenerator,
        IMermaidGenerator mermaidGenerator,
        IDecisionsGenerator decisionsGenerator,
        IEnumerable<IContentRenderer> renderers,
        IOptions<PublicationOptions> publicationOptions)
    {
        _architectureGenerator = architectureGenerator;
        _databaseGenerator = databaseGenerator;
        _agentsGenerator = agentsGenerator;
        _apiGenerator = apiGenerator;
        _mermaidGenerator = mermaidGenerator;
        _decisionsGenerator = decisionsGenerator;
        _renderers = renderers.ToList();
        _distRootPath = publicationOptions.Value.DistRootPath;
        _solutionPath = publicationOptions.Value.SolutionPath;
        _projectVersion = publicationOptions.Value.ProjectVersion;
    }

    /// <inheritdoc />
    public string Category => "engineering";

    /// <inheritdoc />
    public async Task<IReadOnlyList<PublishedArtifact>> PublishAsync(CancellationToken cancellationToken = default)
    {
        var sections = new List<PublicationSection>
        {
            ReportPublishingHelper.BuildSection("Arquitetura", await _architectureGenerator.GenerateAsync(cancellationToken)),
            ReportPublishingHelper.BuildSection("Componentes", await BuildComponentsSectionAsync(cancellationToken)),
            ReportPublishingHelper.BuildSection("Banco de Dados", await _databaseGenerator.GenerateAsync(cancellationToken)),
            ReportPublishingHelper.BuildSection("APIs", await _apiGenerator.GenerateAsync(cancellationToken)),
            ReportPublishingHelper.BuildSection("Eventos", BuildEventsMarkdown()),
            ReportPublishingHelper.BuildSection("Estrutura do Código", BuildCodeStructureSection()),
            ReportPublishingHelper.BuildSection("Dependências", await BuildDependenciesSectionAsync(cancellationToken)),
            ReportPublishingHelper.BuildSection("Decisões Técnicas", await _decisionsGenerator.GenerateAsync(cancellationToken)),
        };

        var metadata = PublicationMetadata.Create(
            title: "Guia de Engenharia — BlueprintOS",
            subtitle: "Documentação técnica completa: arquitetura, estrutura, APIs, dados e decisões",
            audience: "Equipe de Engenharia",
            version: _projectVersion,
            generatedAt: DateTimeOffset.UtcNow,
            tags: new[] { "engenharia", "arquitetura", "técnico" });

        var document = new PublicationDocument(
            Slug: "EngineeringGuide",
            Category: Category,
            Metadata: metadata,
            Sections: sections,
            Assets: PublicationAssets.Empty,
            Appendix: Array.Empty<PublicationSection>(),
            Theme: PublicationTheme.ForEngineering());

        return await ReportPublishingHelper.WriteAllFormatsAsync(document, Category, _distRootPath, _renderers, cancellationToken);
    }

    /// <summary>
    /// Combina o diagrama de dependências entre projetos (visão de componentes) com a
    /// descrição do subsistema de agentes, reaproveitando dois geradores já existentes em vez
    /// de introduzir um novo.
    /// </summary>
    private async Task<string> BuildComponentsSectionAsync(CancellationToken cancellationToken)
    {
        var builder = new StringBuilder();
        builder.AppendLine(ReportPublishingHelper.StripFirstHeadingLine(await _mermaidGenerator.GenerateAsync(cancellationToken)));
        builder.AppendLine();
        builder.AppendLine(ReportPublishingHelper.StripFirstHeadingLine(await _agentsGenerator.GenerateAsync(cancellationToken)));

        return builder.ToString();
    }

    /// <summary>
    /// O BlueprintOS ainda não implementa um barramento de eventos/mensageria (ver
    /// ARCHITECTURE.md §13 - Escalabilidade); Domain Events é um padrão previsto (ARCHITECTURE.md
    /// §11) mas ainda não adotado pelos módulos atuais. Refletido honestamente, sem inventar
    /// eventos inexistentes.
    /// </summary>
    private static string BuildEventsMarkdown() =>
        """
        Nenhum barramento de eventos ou mensageria está implementado até o momento.

        - **Domain Events** é um padrão de arquitetura previsto (ver `.ai/ARCHITECTURE.md`, seção de padrões adotados), mas ainda não foi adotado por nenhum dos módulos atuais.
        - **Mensageria** entre serviços é um item previsto para a fase de escalabilidade (`.ai/ARCHITECTURE.md`, seção 13), condicionado a uma eventual separação em microsserviços — ainda não implementado no monólito modular atual.

        Este documento será atualizado assim que eventos de domínio ou mensageria forem efetivamente implementados.
        """;

    private string BuildCodeStructureSection()
    {
        var builder = new StringBuilder();
        builder.AppendLine("### Estrutura de Pastas");
        builder.AppendLine();

        var repoRoot = FindRepoRoot(_solutionPath);
        if (repoRoot is not null)
        {
            foreach (var directory in Directory.EnumerateDirectories(repoRoot).OrderBy(d => d, StringComparer.Ordinal))
            {
                var name = Path.GetFileName(directory);
                if (name.StartsWith('.') && name is not ".ai" and not ".github")
                {
                    continue;
                }

                builder.AppendLine($"- `{name}/`");
            }
        }

        return builder.ToString();
    }

    private async Task<string> BuildDependenciesSectionAsync(CancellationToken cancellationToken)
    {
        var builder = new StringBuilder();
        builder.AppendLine("### Projetos e Dependências");
        builder.AppendLine();

        if (!File.Exists(_solutionPath))
        {
            builder.AppendLine("_Solution não encontrada; estrutura de projetos não pôde ser coletada automaticamente._");
            return builder.ToString();
        }

        var solutionContent = await File.ReadAllTextAsync(_solutionPath, cancellationToken);
        var solutionDirectory = Path.GetDirectoryName(Path.GetFullPath(_solutionPath)) ?? ".";

        builder.AppendLine("| Projeto | Dependências (ProjectReference) |");
        builder.AppendLine("|---|---|");

        foreach (Match projectMatch in ProjectPattern.Matches(solutionContent))
        {
            var projectName = projectMatch.Groups[1].Value;
            var relativeProjectPath = projectMatch.Groups[2].Value;
            if (!relativeProjectPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var csprojPath = Path.Combine(solutionDirectory, relativeProjectPath);
            var dependencies = "—";
            if (File.Exists(csprojPath))
            {
                var csprojContent = await File.ReadAllTextAsync(csprojPath, cancellationToken);
                var references = ProjectReferencePattern.Matches(csprojContent)
                    .Select(m => Path.GetFileNameWithoutExtension(m.Groups[1].Value))
                    .ToList();
                if (references.Count > 0)
                {
                    dependencies = string.Join(", ", references);
                }
            }

            builder.AppendLine($"| {projectName} | {dependencies} |");
        }

        return builder.ToString();
    }

    private static string? FindRepoRoot(string solutionPath)
    {
        var directory = File.Exists(solutionPath)
            ? Path.GetDirectoryName(Path.GetFullPath(solutionPath))
            : null;

        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory, ".git")))
            {
                return directory;
            }

            directory = Path.GetDirectoryName(directory);
        }

        return null;
    }
}
