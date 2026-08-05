using System.Text.RegularExpressions;
using BlueprintOS.Core.Documentation.Contracts.Executive;
using Microsoft.Extensions.Options;

namespace BlueprintOS.Infrastructure.Documentation.Generators.Executive;

/// <summary>
/// Implementação de <see cref="IRoadmapGenerator"/> que reflete o conteúdo real de
/// <c>.ai/ROADMAP.md</c>.
/// </summary>
public sealed partial class RoadmapGenerator : IRoadmapGenerator
{
    /// <summary>
    /// Casa links Markdown relativos como <c>](./DECISIONS.md)</c> ou <c>](DECISIONS.md)</c>,
    /// exceto links externos (<c>http(s)://</c>), âncoras locais (<c>#...</c>) e caminhos já
    /// relativos a outro diretório (<c>../</c>). Todo link relativo restante em
    /// <c>.ai/ROADMAP.md</c> aponta, por construção, para outro arquivo dentro de <c>.ai/</c>.
    /// </summary>
    [GeneratedRegex(@"\]\((?!https?://|#|\.\./)\.?/?([^)]+)\)")]
    private static partial Regex RelativeAiLinkPattern();

    private readonly string _roadmapPath;

    public RoadmapGenerator(IOptions<DocumentationOptions> options)
    {
        _roadmapPath = Path.Combine(options.Value.AiRootPath, "ROADMAP.md");
    }

    /// <inheritdoc />
    public async Task<string> GenerateAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_roadmapPath))
        {
            return "Nenhum roadmap registrado (`.ai/ROADMAP.md` não encontrado).";
        }

        var content = await File.ReadAllTextAsync(_roadmapPath, cancellationToken);

        // Remove o título de nível 1 original, pois o publicador já adiciona seu próprio cabeçalho.
        var firstLineBreak = content.IndexOf('\n');
        if (firstLineBreak >= 0 && content.TrimStart().StartsWith("# ROADMAP", StringComparison.OrdinalIgnoreCase))
        {
            content = content[(firstLineBreak + 1)..].TrimStart('\n', '\r');
        }

        // O documento gerado vive em docs/executive/, dois níveis abaixo da raiz do repositório,
        // enquanto a fonte (.ai/ROADMAP.md) usa caminhos relativos à própria pasta .ai/. Reescreve
        // esses links para permanecerem válidos a partir de docs/executive/.
        content = RelativeAiLinkPattern().Replace(content, match => $"](../../.ai/{match.Groups[1].Value})");

        return content;
    }
}
