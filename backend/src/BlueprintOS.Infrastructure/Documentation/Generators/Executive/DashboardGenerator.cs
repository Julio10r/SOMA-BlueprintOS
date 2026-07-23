using System.Text;
using System.Text.RegularExpressions;
using BlueprintOS.Core.Documentation.Contracts.Executive;
using Microsoft.Extensions.Options;

namespace BlueprintOS.Infrastructure.Documentation.Generators.Executive;

/// <summary>
/// Implementação de <see cref="IDashboardGenerator"/> que resume o estado atual do projeto
/// a partir de <c>.ai/ROADMAP.md</c> e <c>.ai/memory/completed_sprints.md</c>/<c>known_issues.md</c>.
/// </summary>
public sealed class DashboardGenerator : IDashboardGenerator
{
    private readonly string _aiRootPath;

    public DashboardGenerator(IOptions<DocumentationOptions> options)
    {
        _aiRootPath = options.Value.AiRootPath;
    }

    /// <inheritdoc />
    public async Task<string> GenerateAsync(CancellationToken cancellationToken = default)
    {
        var completedSprintsPath = Path.Combine(_aiRootPath, "memory", "completed_sprints.md");
        var knownIssuesPath = Path.Combine(_aiRootPath, "memory", "known_issues.md");

        var sprintCount = 0;
        string? lastSprintTitle = null;
        if (File.Exists(completedSprintsPath))
        {
            var content = await File.ReadAllTextAsync(completedSprintsPath, cancellationToken);
            var matches = Regex.Matches(content, @"^##\s+(Sprint .+)$", RegexOptions.Multiline);
            sprintCount = matches.Count;
            if (matches.Count > 0)
            {
                lastSprintTitle = matches[^1].Groups[1].Value.Trim();
            }
        }

        var knownIssueCount = 0;
        if (File.Exists(knownIssuesPath))
        {
            var content = await File.ReadAllTextAsync(knownIssuesPath, cancellationToken);
            knownIssueCount = Regex.Matches(content, @"^-\s+\*\*", RegexOptions.Multiline).Count;
        }

        var builder = new StringBuilder();
        builder.AppendLine($"- **Sprints concluídas:** {sprintCount}");
        builder.AppendLine($"- **Última sprint concluída:** {lastSprintTitle ?? "Nenhuma sprint concluída registrada."}");
        builder.AppendLine($"- **Dívidas técnicas em aberto (known_issues.md):** {knownIssueCount}");
        builder.AppendLine();
        builder.AppendLine("Para detalhes por fase, ver o Roadmap. Para o histórico completo de sprints, ver o Changelog.");

        return builder.ToString();
    }
}
