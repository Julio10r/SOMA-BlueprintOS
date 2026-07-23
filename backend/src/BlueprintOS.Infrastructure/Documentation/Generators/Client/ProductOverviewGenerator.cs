using System.Text;
using System.Text.RegularExpressions;
using BlueprintOS.Core.Documentation.Contracts.Client;
using Microsoft.Extensions.Options;

namespace BlueprintOS.Infrastructure.Documentation.Generators.Client;

/// <summary>
/// Implementação de <see cref="IProductOverviewGenerator"/> que resume, para o público
/// cliente, as fases descritas em <c>.ai/ROADMAP.md</c>.
/// </summary>
public sealed class ProductOverviewGenerator : IProductOverviewGenerator
{
    private readonly string _roadmapPath;

    public ProductOverviewGenerator(IOptions<DocumentationOptions> options)
    {
        _roadmapPath = Path.Combine(options.Value.AiRootPath, "ROADMAP.md");
    }

    /// <inheritdoc />
    public async Task<string> GenerateAsync(CancellationToken cancellationToken = default)
    {
        var builder = new StringBuilder();
        builder.AppendLine("## Visão geral do produto");
        builder.AppendLine();
        builder.AppendLine("O BlueprintOS é uma plataforma modular para automação de processos de negócio,");
        builder.AppendLine("organizada em fases de entrega. As fases planejadas, conforme o roadmap oficial, são:");
        builder.AppendLine();

        if (!File.Exists(_roadmapPath))
        {
            builder.AppendLine("_Roadmap não encontrado._");
            return builder.ToString();
        }

        var content = await File.ReadAllTextAsync(_roadmapPath, cancellationToken);
        var phaseTitles = Regex.Matches(content, @"^##\s+(Fase .+)$", RegexOptions.Multiline);
        foreach (Match phase in phaseTitles)
        {
            builder.AppendLine($"- {phase.Groups[1].Value.Trim()}");
        }

        return builder.ToString();
    }
}
