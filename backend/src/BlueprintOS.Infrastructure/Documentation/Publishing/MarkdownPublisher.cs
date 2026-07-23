using BlueprintOS.Core.Documentation.Contracts;
using BlueprintOS.Core.Documentation.Models;
using Microsoft.Extensions.Options;

namespace BlueprintOS.Infrastructure.Documentation.Publishing;

/// <summary>
/// Implementação de <see cref="IDocumentPublisher"/> que escreve o documento Markdown,
/// já envelopado pelo cabeçalho padrão, em disco, sob a raiz de documentação configurada
/// (<see cref="DocumentationOptions.DocsRootPath"/>), criando diretórios conforme necessário.
/// </summary>
public sealed class MarkdownPublisher : IDocumentPublisher
{
    private readonly string _docsRootPath;
    private readonly string _projectVersion;

    public MarkdownPublisher(IOptions<DocumentationOptions> options)
    {
        _docsRootPath = options.Value.DocsRootPath;
        _projectVersion = options.Value.ProjectVersion;
    }

    /// <inheritdoc />
    public async Task<PublishedDocument> PublishAsync(
        string relativePath,
        string title,
        string body,
        CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(_docsRootPath, relativePath);
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var generatedAt = DateTimeOffset.UtcNow;
        var content = MarkdownDocumentTemplate.Render(title, _projectVersion, generatedAt, body);

        await File.WriteAllTextAsync(filePath, content, cancellationToken);

        return new PublishedDocument(relativePath, filePath, title, generatedAt);
    }
}
