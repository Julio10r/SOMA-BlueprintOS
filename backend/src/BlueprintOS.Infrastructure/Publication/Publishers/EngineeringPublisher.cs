using System.Text;
using System.Text.RegularExpressions;
using BlueprintOS.Core.Documentation.Contracts.Engineering;
using BlueprintOS.Core.Publication.Contracts;
using BlueprintOS.Core.Publication.Models;
using Microsoft.Extensions.Options;

namespace BlueprintOS.Infrastructure.Publication.Publishers;

/// <summary>
/// Implementação de <see cref="IReportPublisher"/> para o Guia de Engenharia
/// (<c>dist/engineering/EngineeringGuide.*</c>): reaproveita os geradores de documentação
/// técnica já existentes e acrescenta uma seção de estrutura da solução (projetos,
/// dependências e pastas), lida diretamente do <c>.sln</c>, dos <c>.csproj</c> e do sistema de
/// arquivos do repositório.
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
    private readonly IDeployGenerator _deployGenerator;
    private readonly IRunbookGenerator _runbookGenerator;
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
        IDeployGenerator deployGenerator,
        IRunbookGenerator runbookGenerator,
        IMermaidGenerator mermaidGenerator,
        IDecisionsGenerator decisionsGenerator,
        IEnumerable<IContentRenderer> renderers,
        IOptions<PublicationOptions> publicationOptions)
    {
        _architectureGenerator = architectureGenerator;
        _databaseGenerator = databaseGenerator;
        _agentsGenerator = agentsGenerator;
        _apiGenerator = apiGenerator;
        _deployGenerator = deployGenerator;
        _runbookGenerator = runbookGenerator;
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
            ReportPublishingHelper.BuildSection("Estrutura da Solução, Projetos e Dependências", await BuildSolutionStructureSectionAsync(cancellationToken)),
            ReportPublishingHelper.BuildSection("Diagramas Mermaid e Fluxos", await _mermaidGenerator.GenerateAsync(cancellationToken)),
            ReportPublishingHelper.BuildSection("Banco de Dados", await _databaseGenerator.GenerateAsync(cancellationToken)),
            ReportPublishingHelper.BuildSection("APIs", await _apiGenerator.GenerateAsync(cancellationToken)),
            ReportPublishingHelper.BuildSection("Agentes", await _agentsGenerator.GenerateAsync(cancellationToken)),
            ReportPublishingHelper.BuildSection("ADRs (Decisões Arquiteturais)", await _decisionsGenerator.GenerateAsync(cancellationToken)),
            ReportPublishingHelper.BuildSection("Deploy", await _deployGenerator.GenerateAsync(cancellationToken)),
            ReportPublishingHelper.BuildSection("Observabilidade e Testes (Runbook)", await _runbookGenerator.GenerateAsync(cancellationToken)),
        };

        var document = new PublicationDocument(
            Slug: "EngineeringGuide",
            Title: "Guia de Engenharia — BlueprintOS",
            Subtitle: "Documentação técnica completa: arquitetura, estrutura, APIs, dados e operação",
            Category: Category,
            Sections: sections,
            ProjectVersion: _projectVersion,
            GeneratedAt: DateTimeOffset.UtcNow);

        return await ReportPublishingHelper.WriteAllFormatsAsync(document, Category, _distRootPath, _renderers, cancellationToken);
    }

    private async Task<string> BuildSolutionStructureSectionAsync(CancellationToken cancellationToken)
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

        builder.AppendLine();
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
