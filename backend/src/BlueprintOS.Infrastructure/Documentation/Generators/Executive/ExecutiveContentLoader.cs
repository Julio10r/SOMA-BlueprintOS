using BlueprintOS.Core.Documentation.Contracts.Executive;
using Microsoft.Extensions.Options;

namespace BlueprintOS.Infrastructure.Documentation.Generators.Executive;

/// <summary>
/// Implementação de <see cref="IExecutiveContentLoader"/> que lê os arquivos Markdown de
/// <c>.ai/content/executive/</c>, ordenados alfabeticamente pelo nome do arquivo.
/// </summary>
public sealed class ExecutiveContentLoader : IExecutiveContentLoader
{
    private readonly string _contentDirectory;

    public ExecutiveContentLoader(IOptions<DocumentationOptions> options)
    {
        _contentDirectory = Path.Combine(options.Value.AiRootPath, "content", "executive");
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ExecutiveContentFile>> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(_contentDirectory))
        {
            return Array.Empty<ExecutiveContentFile>();
        }

        var filePaths = Directory.GetFiles(_contentDirectory, "*.md")
            .OrderBy(path => Path.GetFileName(path), StringComparer.Ordinal)
            .ToList();

        var files = new List<ExecutiveContentFile>(filePaths.Count);
        foreach (var filePath in filePaths)
        {
            var content = await File.ReadAllTextAsync(filePath, cancellationToken);
            files.Add(new ExecutiveContentFile(Path.GetFileName(filePath), content));
        }

        return files;
    }
}
