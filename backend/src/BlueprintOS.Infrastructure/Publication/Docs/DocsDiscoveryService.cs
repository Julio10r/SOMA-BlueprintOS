using BlueprintOS.Core.Publication.Contracts;
using BlueprintOS.Core.Publication.Models;

namespace BlueprintOS.Infrastructure.Publication.Docs;

/// <summary>
/// Implementação de <see cref="IDocsDiscoveryService"/>: percorre <c>docs/</c> em disco,
/// ignorando arquivos que não sejam Markdown e os diretórios de topo configurados em
/// <see cref="PublicationOptions.ExcludedTopLevelDirectories"/> (ex.: <c>audits</c>,
/// <c>demo</c>). Categoria e slug derivam exclusivamente do caminho relativo — nenhum nome de
/// audiência é usado ou assumido.
/// </summary>
public sealed class DocsDiscoveryService : IDocsDiscoveryService
{
    private readonly IReadOnlySet<string> _excludedTopLevelDirectories;

    public DocsDiscoveryService(IReadOnlySet<string> excludedTopLevelDirectories)
    {
        _excludedTopLevelDirectories = excludedTopLevelDirectories;
    }

    /// <inheritdoc />
    public IReadOnlyList<DiscoveredDocument> Discover(string docsRootPath)
    {
        var fullRoot = Path.GetFullPath(docsRootPath);
        if (!Directory.Exists(fullRoot))
        {
            throw new DirectoryNotFoundException($"O diretório-fonte de documentação não existe: {fullRoot}");
        }

        var documents = new List<DiscoveredDocument>();

        foreach (var filePath in Directory.EnumerateFiles(fullRoot, "*.md", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(fullRoot, filePath).Replace(Path.DirectorySeparatorChar, '/');
            var firstSegment = relativePath.Split('/')[0];

            // O primeiro segmento só representa um diretório de topo quando o caminho relativo
            // tem mais de um segmento; um arquivo direto na raiz de docs/ nunca é excluído por
            // nome de diretório.
            var isInsideExcludedTopLevelDirectory =
                relativePath.Contains('/') && _excludedTopLevelDirectories.Contains(firstSegment);

            if (isInsideExcludedTopLevelDirectory)
            {
                continue;
            }

            var directory = Path.GetDirectoryName(relativePath)?.Replace(Path.DirectorySeparatorChar, '/') ?? string.Empty;
            var slug = Path.GetFileNameWithoutExtension(relativePath);

            documents.Add(new DiscoveredDocument(filePath, relativePath, directory, slug));
        }

        return documents
            .OrderBy(d => d.RelativePath, StringComparer.Ordinal)
            .ToList();
    }
}
