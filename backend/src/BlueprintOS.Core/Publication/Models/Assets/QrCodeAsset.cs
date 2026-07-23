namespace BlueprintOS.Core.Publication.Models.Assets;

/// <summary>
/// Um QR Code a ser embutido no documento, apontando para conteúdo real (ex.: a URL do
/// repositório), nunca para um destino fabricado. A imagem (<see cref="ImageBytes"/>) é gerada
/// em tempo real pelo Publication Engine a partir de <see cref="Content"/> (ver
/// <c>QrCodeImageGenerator</c> em Infrastructure).
/// </summary>
/// <param name="Id">Identificador estável, referenciado por um <c>ContentBlock</c> de imagem via <c>AssetId</c>.</param>
/// <param name="Content">Conteúdo codificado no QR Code (tipicamente uma URL).</param>
/// <param name="Label">Rótulo exibido junto ao QR Code.</param>
/// <param name="ImageBytes">Imagem PNG já gerada a partir de <see cref="Content"/>.</param>
public sealed record QrCodeAsset(string Id, string Content, string Label, byte[] ImageBytes);
