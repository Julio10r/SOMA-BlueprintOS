using BlueprintOS.Core.Publication.Models;

namespace BlueprintOS.Core.Publication.Contracts;

/// <summary>
/// Define o contrato de renderização de um <see cref="PublicationDocument"/> em um formato de
/// saída específico (Markdown, HTML ou PDF).
/// </summary>
public interface IContentRenderer
{
    /// <summary>
    /// Formato de saída produzido por este renderizador.
    /// </summary>
    PublicationFormat Format { get; }

    /// <summary>
    /// Renderiza o documento informado, retornando o conteúdo final pronto para ser escrito em disco.
    /// </summary>
    /// <param name="document">Documento a ser renderizado.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task<byte[]> RenderAsync(PublicationDocument document, CancellationToken cancellationToken = default);
}
