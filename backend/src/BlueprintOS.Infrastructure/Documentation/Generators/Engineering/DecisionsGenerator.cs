using System.Text;
using System.Text.RegularExpressions;
using BlueprintOS.Core.Documentation.Contracts.Engineering;
using Microsoft.Extensions.Options;

namespace BlueprintOS.Infrastructure.Documentation.Generators.Engineering;

/// <summary>
/// Implementação de <see cref="IDecisionsGenerator"/> que reflete o conteúdo real de
/// <c>.ai/DECISIONS.md</c>, listando os títulos das ADRs registradas.
/// </summary>
public sealed class DecisionsGenerator : IDecisionsGenerator
{
    private readonly string _decisionsPath;

    public DecisionsGenerator(IOptions<DocumentationOptions> options)
    {
        _decisionsPath = Path.Combine(options.Value.AiRootPath, "DECISIONS.md");
    }

    /// <inheritdoc />
    public async Task<string> GenerateAsync(CancellationToken cancellationToken = default)
    {
        var builder = new StringBuilder();
        builder.AppendLine("## Architecture Decision Records (ADRs)");
        builder.AppendLine();

        if (!File.Exists(_decisionsPath))
        {
            builder.AppendLine("_Nenhuma ADR registrada._");
            return builder.ToString();
        }

        var content = await File.ReadAllTextAsync(_decisionsPath, cancellationToken);
        var titles = Regex.Matches(content, @"^##\s+(ADR-\d{4}:.+)$", RegexOptions.Multiline);

        if (titles.Count == 0)
        {
            builder.AppendLine("_Nenhuma ADR registrada._");
            return builder.ToString();
        }

        foreach (Match title in titles)
        {
            builder.AppendLine($"- {title.Groups[1].Value.Trim()}");
        }

        builder.AppendLine();
        builder.AppendLine("Ver `.ai/DECISIONS.md` para o texto completo de contexto, decisão e consequências de cada ADR.");

        return builder.ToString();
    }
}
