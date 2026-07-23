using System.Collections.Concurrent;
using BlueprintOS.Core.Documentation.Contracts;
using BlueprintOS.Core.Documentation.Models;

namespace BlueprintOS.Infrastructure.Documentation;

/// <summary>
/// Implementação de <see cref="IDocumentationRepository"/> que mantém os documentos em memória,
/// seguindo o estilo simples adotado pelos providers de conhecimento (ex.: MarkdownKnowledgeProvider)
/// já existentes na camada Infrastructure.
/// </summary>
public sealed class InMemoryDocumentationRepository : IDocumentationRepository
{
    private readonly ConcurrentDictionary<string, DocumentationEntry> _entries = new();

    /// <inheritdoc />
    public Task<DocumentationEntry> AddAsync(DocumentationEntry entry, CancellationToken cancellationToken = default)
    {
        if (!_entries.TryAdd(entry.Id, entry))
        {
            throw new InvalidOperationException($"Já existe um documento com o Id '{entry.Id}'.");
        }

        return Task.FromResult(entry);
    }

    /// <inheritdoc />
    public Task<DocumentationEntry?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        _entries.TryGetValue(id, out var entry);
        return Task.FromResult(entry);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<DocumentationEntry>> ListAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<DocumentationEntry> result = _entries.Values.ToList();
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<DocumentationEntry> UpdateAsync(DocumentationEntry entry, CancellationToken cancellationToken = default)
    {
        if (!_entries.ContainsKey(entry.Id))
        {
            throw new InvalidOperationException($"Documento com Id '{entry.Id}' não encontrado.");
        }

        _entries[entry.Id] = entry;
        return Task.FromResult(entry);
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_entries.TryRemove(id, out _));
    }
}
