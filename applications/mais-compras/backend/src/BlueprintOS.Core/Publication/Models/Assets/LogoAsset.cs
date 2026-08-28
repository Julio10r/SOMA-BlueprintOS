namespace BlueprintOS.Core.Publication.Models.Assets;

/// <summary>
/// Onde um <see cref="LogoAsset"/> deve ser exibido. Vários renderizadores podem ignorar
/// posições que ainda não suportam (ex.: Markdown não tem capa visual).
/// </summary>
public enum LogoPlacement
{
    /// <summary>Capa do documento.</summary>
    Cover,

    /// <summary>Cabeçalho de cada página/seção.</summary>
    Header,

    /// <summary>Rodapé de cada página/seção.</summary>
    Footer,
}

/// <summary>
/// Um logotipo (da SOMA, de um cliente, de um parceiro) usado na identidade visual do
/// documento. Quando nenhum <see cref="LogoAsset"/> de posição <see cref="LogoPlacement.Cover"/>
/// está presente, os renderizadores usam o nome textual "BLUEPRINTOS" como hoje — nenhum
/// arquivo de logo é fabricado por esta sprint.
/// </summary>
/// <param name="Id">Identificador estável do logo.</param>
/// <param name="Bytes">Conteúdo binário do logo (PNG/SVG/JPEG).</param>
/// <param name="MediaType">Tipo MIME do conteúdo.</param>
/// <param name="AltText">Texto alternativo.</param>
/// <param name="Placement">Onde este logo deve ser exibido.</param>
public sealed record LogoAsset(
    string Id,
    byte[] Bytes,
    string MediaType,
    string AltText,
    LogoPlacement Placement = LogoPlacement.Cover);
