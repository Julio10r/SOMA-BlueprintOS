using System.Text;
using System.Text.RegularExpressions;
using BlueprintOS.Core.Documentation.Contracts.Executive;
using Microsoft.Extensions.Options;

namespace BlueprintOS.Infrastructure.Documentation.Generators.Executive;

/// <summary>
/// Implementação de <see cref="ISprintStatusGenerator"/> que extrai a última entrada de
/// <c>.ai/memory/completed_sprints.md</c>.
/// </summary>
public sealed class SprintStatusGenerator : ISprintStatusGenerator
{
    private readonly string _completedSprintsPath;

    public SprintStatusGenerator(IOptions<DocumentationOptions> options)
    {
        _completedSprintsPath = Path.Combine(options.Value.AiRootPath, "memory", "completed_sprints.md");
    }

    /// <inheritdoc />
    public async Task<string> GenerateAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_completedSprintsPath))
        {
            return "Nenhuma sprint concluída registrada ainda.";
        }

        var content = await File.ReadAllTextAsync(_completedSprintsPath, cancellationToken);
        var sections = Regex.Split(content, @"(?=^##\s+Sprint )", RegexOptions.Multiline)
            .Select(s => s.Trim())
            .Where(s => s.StartsWith("## Sprint ", StringComparison.Ordinal))
            .ToList();

        if (sections.Count == 0)
        {
            return "Nenhuma sprint concluída registrada ainda.";
        }

        var builder = new StringBuilder();
        builder.AppendLine("## Status da sprint mais recente");
        builder.AppendLine();
        builder.AppendLine(sections[^1]);

        return builder.ToString();
    }
}
