using BlueprintOS.Core.Knowledge.Contracts;
using BlueprintOS.Core.Knowledge.Models;

namespace BlueprintOS.Infrastructure.Services;

/// <summary>
/// Implementação de <see cref="IKnowledgeService"/> que realiza busca textual simples
/// (correspondência de substring, sem distinção de maiúsculas/minúsculas) sobre os documentos
/// carregados por um <see cref="IKnowledgeProvider"/>.
/// </summary>
public sealed class KnowledgeService : IKnowledgeService
{
    private const int SnippetContextLength = 80;

    private readonly IKnowledgeProvider _provider;

    public KnowledgeService(IKnowledgeProvider provider)
    {
        _provider = provider;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<KnowledgeSearchResult>> SearchAsync(
        string query,
        int maxResults = 5,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Array.Empty<KnowledgeSearchResult>();
        }

        var documents = await _provider.LoadDocumentsAsync(cancellationToken);
        var results = new List<KnowledgeSearchResult>();

        foreach (var document in documents)
        {
            var occurrences = CountOccurrences(document.Content, query);
            if (occurrences == 0)
            {
                continue;
            }

            var snippet = ExtractSnippet(document.Content, query);
            results.Add(new KnowledgeSearchResult(document, snippet, occurrences));
        }

        return results
            .OrderByDescending(r => r.Score)
            .Take(maxResults)
            .ToList();
    }

    private static int CountOccurrences(string content, string query)
    {
        var count = 0;
        var index = 0;

        while ((index = content.IndexOf(query, index, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            count++;
            index += query.Length;
        }

        return count;
    }

    private static string ExtractSnippet(string content, string query)
    {
        var matchIndex = content.IndexOf(query, StringComparison.OrdinalIgnoreCase);
        if (matchIndex < 0)
        {
            return string.Empty;
        }

        var start = Math.Max(0, matchIndex - SnippetContextLength);
        var end = Math.Min(content.Length, matchIndex + query.Length + SnippetContextLength);
        var snippet = content[start..end].Trim();

        return start > 0 ? $"...{snippet}" : snippet;
    }
}
