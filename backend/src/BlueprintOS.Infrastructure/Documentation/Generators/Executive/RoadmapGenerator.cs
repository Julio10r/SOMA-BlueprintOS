using BlueprintOS.Core.Documentation.Contracts.Executive;
using Microsoft.Extensions.Options;

namespace BlueprintOS.Infrastructure.Documentation.Generators.Executive;

/// <summary>
/// Implementação de <see cref="IRoadmapGenerator"/> que reflete o conteúdo real de
/// <c>.ai/ROADMAP.md</c>.
/// </summary>
public sealed class RoadmapGenerator : IRoadmapGenerator
{
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

        return content;
    }
}
