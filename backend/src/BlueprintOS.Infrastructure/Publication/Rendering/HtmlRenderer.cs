using System.Text;
using BlueprintOS.Core.Publication.Contracts;
using BlueprintOS.Core.Publication.Models;

namespace BlueprintOS.Infrastructure.Publication.Rendering;

/// <summary>
/// Implementação de <see cref="IContentRenderer"/> que produz um documento HTML com aparência
/// moderna, responsiva e sem frameworks (apenas HTML + CSS embutido), com capa, cabeçalho,
/// rodapé, índice, cards e tabelas.
/// </summary>
public sealed class HtmlRenderer : IContentRenderer
{
    /// <inheritdoc />
    public PublicationFormat Format => PublicationFormat.Html;

    /// <inheritdoc />
    public Task<byte[]> RenderAsync(PublicationDocument document, CancellationToken cancellationToken = default)
    {
        var html = HtmlDocumentTemplate.Render(document);
        return Task.FromResult(Encoding.UTF8.GetBytes(html));
    }
}
