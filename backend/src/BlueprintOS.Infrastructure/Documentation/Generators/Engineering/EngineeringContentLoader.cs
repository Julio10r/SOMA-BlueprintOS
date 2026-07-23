using BlueprintOS.Core.Documentation.Contracts.Engineering;
using Microsoft.Extensions.Options;

namespace BlueprintOS.Infrastructure.Documentation.Generators.Engineering;

/// <summary>
/// Implementação de <see cref="IEngineeringContentLoader"/> que lê os arquivos Markdown de
/// <c>.ai/content/engineering/</c>, ordenados alfabeticamente pelo nome do arquivo.
/// </summary>
public sealed class EngineeringContentLoader : IEngineeringContentLoader
{
    private readonly string _contentDirectory;

    public EngineeringContentLoader(IOptions<DocumentationOptions> options)
    {
        _contentDirectory = Path.Combine(options.Value.AiRootPath, "content", "engineering");
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EngineeringContentFile>> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(_contentDirectory))
        {
            return Array.Empty<EngineeringContentFile>();
        }

        var filePaths = Directory.GetFiles(_contentDirectory, "*.md")
            .OrderBy(path => Path.GetFileName(path), StringComparer.Ordinal)
            .ToList();

        var files = new List<EngineeringContentFile>(filePaths.Count);
        foreach (var filePath in filePaths)
        {
            var content = await File.ReadAllTextAsync(filePath, cancellationToken);
            files.Add(new EngineeringContentFile(Path.GetFileName(filePath), content));
        }

        return files;
    }
}
