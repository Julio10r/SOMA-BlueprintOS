using System.Text;
using System.Text.RegularExpressions;
using BlueprintOS.Core.Documentation.Contracts.Executive;
using Microsoft.Extensions.Options;

namespace BlueprintOS.Infrastructure.Documentation.Generators.Executive;

/// <summary>
/// Implementação de <see cref="IReleaseGenerator"/>. O repositório não possui tags de
/// release nem arquivo <c>CHANGELOG</c> dedicado; na ausência dessas fontes, cada sprint
/// concluída (registrada em <c>.ai/memory/completed_sprints.md</c>) é tratada como a
/// unidade real de release do BlueprintOS.
/// </summary>
public sealed class ReleaseGenerator : IReleaseGenerator
{
    private readonly string _completedSprintsPath;

    public ReleaseGenerator(IOptions<DocumentationOptions> options)
    {
        _completedSprintsPath = Path.Combine(options.Value.AiRootPath, "memory", "completed_sprints.md");
    }

    /// <inheritdoc />
    public async Task<string> GenerateAsync(CancellationToken cancellationToken = default)
    {
        var builder = new StringBuilder();
        builder.AppendLine("## Releases");
        builder.AppendLine();
        builder.AppendLine("O BlueprintOS ainda não possui tags de release nem arquivo `CHANGELOG` dedicado.");
        builder.AppendLine("Cada sprint concluída é tratada, até o momento, como a unidade real de entrega/release:");
        builder.AppendLine();

        if (!File.Exists(_completedSprintsPath))
        {
            builder.AppendLine("_Nenhuma sprint concluída registrada._");
            return builder.ToString();
        }

        var content = await File.ReadAllTextAsync(_completedSprintsPath, cancellationToken);
        var titles = Regex.Matches(content, @"^##\s+(Sprint .+)$", RegexOptions.Multiline);
        if (titles.Count == 0)
        {
            builder.AppendLine("_Nenhuma sprint concluída registrada._");
            return builder.ToString();
        }

        foreach (Match title in titles)
        {
            builder.AppendLine($"- {title.Groups[1].Value.Trim()}");
        }

        return builder.ToString();
    }
}
