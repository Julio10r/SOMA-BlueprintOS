using System.Text;
using System.Text.RegularExpressions;
using BlueprintOS.Core.Documentation.Contracts.Client;
using Microsoft.Extensions.Options;

namespace BlueprintOS.Infrastructure.Documentation.Generators.Client;

/// <summary>
/// Implementação de <see cref="IChangelogGenerator"/> voltada a clientes, refletindo o
/// histórico real de sprints concluídas registrado em <c>.ai/memory/completed_sprints.md</c>.
/// </summary>
public sealed class ChangelogGenerator : IChangelogGenerator
{
    private readonly string _completedSprintsPath;

    public ChangelogGenerator(IOptions<DocumentationOptions> options)
    {
        _completedSprintsPath = Path.Combine(options.Value.AiRootPath, "memory", "completed_sprints.md");
    }

    /// <inheritdoc />
    public async Task<string> GenerateAsync(CancellationToken cancellationToken = default)
    {
        var builder = new StringBuilder();
        builder.AppendLine("## Changelog");
        builder.AppendLine();

        if (!File.Exists(_completedSprintsPath))
        {
            builder.AppendLine("_Nenhuma alteração registrada._");
            return builder.ToString();
        }

        var content = await File.ReadAllTextAsync(_completedSprintsPath, cancellationToken);
        var sections = Regex.Split(content, @"(?=^##\s+Sprint )", RegexOptions.Multiline)
            .Select(s => s.Trim())
            .Where(s => s.StartsWith("## Sprint ", StringComparison.Ordinal))
            .Reverse();

        var any = false;
        foreach (var section in sections)
        {
            any = true;
            var titleMatch = Regex.Match(section, @"^##\s+(.+)$", RegexOptions.Multiline);
            var scopeMatch = Regex.Match(section, @"\*\*Escopo:\*\*\s*(.+)", RegexOptions.None);
            builder.AppendLine($"### {(titleMatch.Success ? titleMatch.Groups[1].Value.Trim() : "Sprint")}");
            builder.AppendLine();
            if (scopeMatch.Success)
            {
                builder.AppendLine(scopeMatch.Groups[1].Value.Trim());
            }

            builder.AppendLine();
        }

        if (!any)
        {
            builder.AppendLine("_Nenhuma alteração registrada._");
        }

        return builder.ToString();
    }
}
