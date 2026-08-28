namespace BlueprintOS.Core.Publication.Models.Assets;

/// <summary>
/// Categoria visual de uma <see cref="ImageAsset"/>, usada apenas para intenção/legibilidade
/// (todos os tipos são renderizados da mesma forma hoje). <c>Screenshot</c> é o ponto de
/// extensão para a futura captura automática da aplicação.
/// </summary>
public enum ImageAssetKind
{
    /// <summary>Ilustração ou imagem de apoio genérica.</summary>
    Illustration,

    /// <summary>Captura de tela da aplicação.</summary>
    Screenshot,

    /// <summary>Diagrama já renderizado como imagem (ex.: Mermaid pré-renderizado).</summary>
    Diagram,
}

/// <summary>
/// Uma imagem embutida em um documento (ilustração, captura de tela ou diagrama já
/// renderizado). Distinta de <see cref="LogoAsset"/> (identidade visual) e de
/// <see cref="MermaidAsset"/> (fonte do diagrama, ainda não necessariamente renderizada).
/// </summary>
/// <param name="Id">Identificador estável, referenciado por um <c>ContentBlock</c> de imagem via <c>AssetId</c>.</param>
/// <param name="Title">Título/legenda da imagem.</param>
/// <param name="Bytes">Conteúdo binário da imagem (PNG/JPEG).</param>
/// <param name="MediaType">Tipo MIME do conteúdo (ex.: <c>image/png</c>).</param>
/// <param name="AltText">Texto alternativo, para acessibilidade e para o Markdown.</param>
/// <param name="Kind">Categoria visual da imagem.</param>
public sealed record ImageAsset(
    string Id,
    string Title,
    byte[] Bytes,
    string MediaType,
    string AltText,
    ImageAssetKind Kind = ImageAssetKind.Illustration);
