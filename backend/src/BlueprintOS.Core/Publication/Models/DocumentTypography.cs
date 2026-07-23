namespace BlueprintOS.Core.Publication.Models;

/// <summary>
/// Famílias de fonte oficiais do Design System (AZZAS 2154 - GDT Design System), resolvidas pelo
/// <c>IDocumentThemeProvider</c> — nunca hardcoded pelos renderizadores.
/// </summary>
/// <param name="DisplayFontFamily">Fonte de destaque (capa, títulos grandes) — ex.: "Inter Tight".</param>
/// <param name="BodyFontFamily">Fonte de corpo/texto — ex.: "DM Sans".</param>
/// <param name="MonoFontFamily">Fonte monoespaçada para código — ex.: "DM Mono".</param>
public sealed record DocumentTypography(
    string DisplayFontFamily,
    string BodyFontFamily,
    string MonoFontFamily);
