using BlueprintOS.Core.Publication.Models.Assets;

namespace BlueprintOS.Core.Publication.Models;

/// <summary>
/// Todos os ativos (assets) visuais de um <see cref="PublicationDocument"/>, agrupados por
/// tipo. Cada grupo é uma lista independente para permitir que novos tipos de asset sejam
/// adicionados no futuro (ex.: assinatura eletrônica) sem afetar os já existentes. Publishers
/// populam apenas os grupos para os quais têm dados reais; grupos vazios são ignorados
/// silenciosamente pelos renderizadores.
/// </summary>
/// <param name="Images">Imagens/ilustrações/capturas de tela.</param>
/// <param name="Logos">Logotipos de capa/cabeçalho/rodapé.</param>
/// <param name="Icons">Ícones SVG.</param>
/// <param name="Charts">Gráficos simples (KPIs).</param>
/// <param name="Mermaid">Diagramas Mermaid (fonte, e imagem quando já rasterizada).</param>
/// <param name="Attachments">Arquivos anexados ao documento.</param>
/// <param name="QrCodes">QR Codes.</param>
/// <param name="Badges">Selos curtos (build/testes/cobertura).</param>
public sealed record PublicationAssets(
    IReadOnlyList<ImageAsset> Images,
    IReadOnlyList<LogoAsset> Logos,
    IReadOnlyList<IconAsset> Icons,
    IReadOnlyList<ChartAsset> Charts,
    IReadOnlyList<MermaidAsset> Mermaid,
    IReadOnlyList<AttachmentAsset> Attachments,
    IReadOnlyList<QrCodeAsset> QrCodes,
    IReadOnlyList<BadgeAsset> Badges)
{
    /// <summary>Conjunto de assets vazio, para documentos que ainda não populam nenhum ativo visual.</summary>
    public static PublicationAssets Empty { get; } = new(
        Array.Empty<ImageAsset>(),
        Array.Empty<LogoAsset>(),
        Array.Empty<IconAsset>(),
        Array.Empty<ChartAsset>(),
        Array.Empty<MermaidAsset>(),
        Array.Empty<AttachmentAsset>(),
        Array.Empty<QrCodeAsset>(),
        Array.Empty<BadgeAsset>());

    /// <summary>
    /// Localiza uma imagem embutível (para um <c>ContentBlock</c> de imagem) pelo id, entre
    /// <see cref="Images"/>, diagramas <see cref="Mermaid"/> já rasterizados e <see cref="QrCodes"/>.
    /// </summary>
    public (byte[] Bytes, string MediaType, string AltText)? FindEmbeddableImage(string assetId)
    {
        var image = Images.FirstOrDefault(i => i.Id == assetId);
        if (image is not null)
        {
            return (image.Bytes, image.MediaType, image.AltText);
        }

        var mermaid = Mermaid.FirstOrDefault(m => m.Id == assetId && m.RenderedImageBytes is not null);
        if (mermaid is not null)
        {
            return (mermaid.RenderedImageBytes!, mermaid.RenderedMediaType ?? "image/png", mermaid.Title);
        }

        var qrCode = QrCodes.FirstOrDefault(q => q.Id == assetId);
        if (qrCode is not null)
        {
            return (qrCode.ImageBytes, "image/png", qrCode.Label);
        }

        return null;
    }
}
