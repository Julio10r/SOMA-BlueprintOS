using BlueprintOS.Core.Knowledge.Contracts;
using BlueprintOS.Core.Knowledge.Models;
using Microsoft.Extensions.Options;

namespace BlueprintOS.Infrastructure.Knowledge;

/// <summary>
/// Implementação de <see cref="IKnowledgeProvider"/> que carrega documentos de conhecimento
/// a partir de arquivos Markdown (.md) presentes em um diretório configurado.
/// </summary>
public sealed class MarkdownKnowledgeProvider : IKnowledgeProvider
{
    private readonly string _directoryPath;

    public MarkdownKnowledgeProvider(IOptions<KnowledgeOptions> options)
    {
        _directoryPath = options.Value.DirectoryPath;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<KnowledgeDocument>> LoadDocumentsAsync(CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(_directoryPath))
        {
            return Array.Empty<KnowledgeDocument>();
        }

        var files = Directory.GetFiles(_directoryPath, "*.md", SearchOption.AllDirectories);
        var documents = new List<KnowledgeDocument>(files.Length);

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var content = await File.ReadAllTextAsync(file, cancellationToken);
            var id = Path.GetRelativePath(_directoryPath, file).Replace('\\', '/');
            var title = ExtractTitle(content) ?? Path.GetFileNameWithoutExtension(file);

            documents.Add(new KnowledgeDocument(id, title, content, file));
        }

        return documents;
    }

    private static string? ExtractTitle(string content)
    {
        using var reader = new StringReader(content);
        while (reader.ReadLine() is { } line)
        {
            var trimmed = line.TrimStart('#', ' ').Trim();
            if (line.TrimStart().StartsWith('#') && trimmed.Length > 0)
            {
                return trimmed;
            }
        }

        return null;
    }
}
