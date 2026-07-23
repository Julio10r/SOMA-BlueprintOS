namespace BlueprintOS.Core.Publication.Models;

/// <summary>
/// Paleta de cores (hexadecimal, sem o caractere <c>#</c>) usada pelos documentos publicados,
/// resolvida a partir do Design System oficial (AZZAS 2154 - GDT Design System) pelo
/// <c>IDocumentThemeProvider</c> — nunca hardcoded pelos renderizadores ou Publishers.
/// </summary>
/// <param name="TextPrimaryHex">Cor de texto principal ("quase-preto" institucional).</param>
/// <param name="AccentHex">Cor de destaque (links, títulos, capa).</param>
/// <param name="MutedHex">Cor de texto secundário/legendas.</param>
/// <param name="BorderHex">Cor de bordas e divisores.</param>
/// <param name="SurfaceHex">Cor de fundo de superfícies (cards, cabeçalhos de tabela).</param>
/// <param name="BackgroundHex">Cor de fundo da página.</param>
/// <param name="SuccessHex">Cor semântica de sucesso/aprovado.</param>
/// <param name="WarningHex">Cor semântica de alerta/avaliação.</param>
/// <param name="ErrorHex">Cor semântica de erro/rejeitado.</param>
/// <param name="InfoHex">Cor semântica neutra/informativa.</param>
public sealed record DocumentPalette(
    string TextPrimaryHex,
    string AccentHex,
    string MutedHex,
    string BorderHex,
    string SurfaceHex,
    string BackgroundHex,
    string SuccessHex,
    string WarningHex,
    string ErrorHex,
    string InfoHex);
