using BlueprintOS.Core.Documentation.Contracts.Client;
using Microsoft.Extensions.Options;

namespace BlueprintOS.Infrastructure.Documentation.Generators.Client;

/// <summary>
/// Implementação de <see cref="IClientContentLoader"/> que lê os arquivos Markdown de
/// <c>.ai/content/client/</c>, ordenados alfabeticamente pelo nome do arquivo.
/// </summary>
public sealed class ClientContentLoader : IClientContentLoader
{
    private readonly string _contentDirectory;

    public ClientContentLoader(IOptions<DocumentationOptions> options)
    {
        _contentDirectory = Path.Combine(options.Value.AiRootPath, "content", "client");
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ClientContentFile>> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(_contentDirectory))
        {
            return Array.Empty<ClientContentFile>();
        }

        var filePaths = Directory.GetFiles(_contentDirectory, "*.md")
            .OrderBy(path => Path.GetFileName(path), StringComparer.Ordinal)
            .ToList();

        var files = new List<ClientContentFile>(filePaths.Count);
        foreach (var filePath in filePaths)
        {
            var content = await File.ReadAllTextAsync(filePath, cancellationToken);
            files.Add(new ClientContentFile(Path.GetFileName(filePath), content));
        }

        return files;
    }
}
