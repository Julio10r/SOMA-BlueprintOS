using BlueprintOS.Core.Documentation.Contracts;
using BlueprintOS.Core.Documentation.Models;

namespace BlueprintOS.Infrastructure.Documentation;

/// <summary>
/// Implementação de <see cref="IDocumentationSyncService"/> que compara o timestamp de escrita
/// (<see cref="File.GetLastWriteTimeUtc(string)"/>) do arquivo de documentação com o dos arquivos
/// de código-fonte relacionados.
/// </summary>
public sealed class DocumentationSyncService : IDocumentationSyncService
{
    /// <inheritdoc />
    public Task<StaleDocumentationInfo> CheckAsync(DocumentationSyncCheck check, CancellationToken cancellationToken = default)
    {
        var docLastWrite = File.Exists(check.DocPath) ? File.GetLastWriteTimeUtc(check.DocPath) : (DateTime?)null;

        var sourceTimestamps = check.SourcePaths
            .Where(File.Exists)
            .Select(File.GetLastWriteTimeUtc)
            .ToList();

        var newestSource = sourceTimestamps.Count > 0 ? sourceTimestamps.Max() : (DateTime?)null;

        var isStale = docLastWrite is null && newestSource is not null
            || (docLastWrite is not null && newestSource is not null && newestSource > docLastWrite);

        var result = new StaleDocumentationInfo(check.DocPath, docLastWrite, newestSource, isStale);
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StaleDocumentationInfo>> CheckAllAsync(
        IReadOnlyList<DocumentationSyncCheck> checks,
        CancellationToken cancellationToken = default)
    {
        var results = new List<StaleDocumentationInfo>(checks.Count);

        foreach (var check in checks)
        {
            cancellationToken.ThrowIfCancellationRequested();
            results.Add(await CheckAsync(check, cancellationToken));
        }

        return results;
    }
}
