using BlueprintOS.Core.Documentation.Models.Assets;

namespace BlueprintOS.Core.Documentation.Contracts.Assets;

/// <summary>
/// Define o contrato de publicação de um ativo de documentação em disco. Diferente de
/// <see cref="IDocumentPublisher"/>, não adiciona nenhum envelope de cabeçalho: o ativo é
/// escrito exatamente como gerado, pois é consumido por ferramentas (Mermaid) e por futuros
/// Publishers, não lido diretamente como documento.
/// </summary>
public interface IAssetPublisher
{
    /// <summary>
    /// Publica o ativo informado no caminho relativo indicado, dentro da raiz de ativos.
    /// </summary>
    Task PublishAsync(DocumentationAsset asset, CancellationToken cancellationToken = default);
}
