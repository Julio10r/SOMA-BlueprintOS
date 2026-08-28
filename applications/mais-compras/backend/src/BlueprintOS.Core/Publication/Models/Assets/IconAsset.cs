namespace BlueprintOS.Core.Publication.Models.Assets;

/// <summary>
/// Um ícone vetorial (SVG) referenciável por nome, usado para pequenos indicadores visuais
/// dentro de seções (ex.: um ícone de "sucesso" ao lado de um indicador de build). O HTML
/// embute o SVG diretamente; o PDF, sem suporte nativo a SVG no QuestPDF, usa <see cref="FallbackGlyph"/>
/// como aproximação textual até que exista um pipeline de rasterização de SVG.
/// </summary>
/// <param name="Id">Identificador estável do ícone.</param>
/// <param name="Name">Nome descritivo do ícone (ex.: "check-circle").</param>
/// <param name="SvgMarkup">Marcação SVG completa do ícone.</param>
/// <param name="FallbackGlyph">Caractere/emoji usado como aproximação em formatos sem suporte a SVG.</param>
public sealed record IconAsset(string Id, string Name, string SvgMarkup, string FallbackGlyph = "•");
