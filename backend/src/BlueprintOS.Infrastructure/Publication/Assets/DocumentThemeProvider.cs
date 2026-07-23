using System.Text.RegularExpressions;
using BlueprintOS.Core.Publication.Contracts;
using BlueprintOS.Core.Publication.Models;
using BlueprintOS.Infrastructure.Documentation;
using Microsoft.Extensions.Options;

namespace BlueprintOS.Infrastructure.Publication.Assets;

/// <summary>
/// Implementação de <see cref="IDocumentThemeProvider"/> que lê os tokens de cor e tipografia
/// diretamente de <c>docs/design-system/colors_and_type.css</c> e <c>fonts.css</c> — a mesma
/// fonte de verdade usada pelo Portal GDT e pelos materiais institucionais (ver
/// <c>docs/design-system/SKILL.md</c>). Nenhum valor de cor ou fonte é definido aqui além do
/// fallback de segurança usado quando o Design System não está presente no repositório.
/// </summary>
public sealed class DocumentThemeProvider : IDocumentThemeProvider
{
    private static readonly Regex TokenPattern = new(@"--([\w-]+)\s*:\s*([^;]+);", RegexOptions.Compiled);

    /// <summary>
    /// Paleta de segurança: os mesmos valores oficiais documentados em
    /// <c>colors_and_type.css</c>, embutidos apenas como rede de segurança para quando o Design
    /// System não está presente no repositório — nunca uma paleta alternativa/nova.
    /// </summary>
    private static readonly DocumentPalette FallbackPalette = new(
        TextPrimaryHex: "#1A1916",
        AccentHex: "#1A1916",
        MutedHex: "#6B6860",
        BorderHex: "#E2E0DB",
        SurfaceHex: "#FFFFFF",
        BackgroundHex: "#F7F6F3",
        SuccessHex: "#2D6A4F",
        WarningHex: "#E09B3D",
        ErrorHex: "#C0392B",
        InfoHex: "#4A90D9");

    private static readonly DocumentTypography FallbackTypography = new(
        DisplayFontFamily: "Inter Tight",
        BodyFontFamily: "DM Sans",
        MonoFontFamily: "DM Mono");

    private readonly string _designSystemRoot;

    public DocumentThemeProvider(IOptions<DocumentationOptions> options)
    {
        _designSystemRoot = options.Value.DesignSystemRootPath;
    }

    /// <inheritdoc />
    public DocumentPalette GetPalette()
    {
        var tokens = ReadTokens();
        if (tokens.Count == 0)
        {
            return FallbackPalette;
        }

        return new DocumentPalette(
            TextPrimaryHex: Resolve(tokens, "text-primary", FallbackPalette.TextPrimaryHex),
            AccentHex: Resolve(tokens, "accent", FallbackPalette.AccentHex),
            MutedHex: Resolve(tokens, "text-secondary", FallbackPalette.MutedHex),
            BorderHex: Resolve(tokens, "border", FallbackPalette.BorderHex),
            SurfaceHex: Resolve(tokens, "surface", FallbackPalette.SurfaceHex),
            BackgroundHex: Resolve(tokens, "bg", FallbackPalette.BackgroundHex),
            SuccessHex: Resolve(tokens, "aprovado", FallbackPalette.SuccessHex),
            WarningHex: Resolve(tokens, "avaliacao", FallbackPalette.WarningHex),
            ErrorHex: Resolve(tokens, "rejeitado", FallbackPalette.ErrorHex),
            InfoHex: Resolve(tokens, "novo", FallbackPalette.InfoHex));
    }

    /// <inheritdoc />
    public DocumentTypography GetTypography()
    {
        var tokens = ReadTokens();
        if (tokens.Count == 0)
        {
            return FallbackTypography;
        }

        return new DocumentTypography(
            DisplayFontFamily: ResolveFontFamily(tokens, "font-display", FallbackTypography.DisplayFontFamily),
            BodyFontFamily: ResolveFontFamily(tokens, "font", FallbackTypography.BodyFontFamily),
            MonoFontFamily: ResolveFontFamily(tokens, "mono", FallbackTypography.MonoFontFamily));
    }

    /// <inheritdoc />
    public string GetStylesheet()
    {
        var officialCss = ReadOfficialCss();
        return officialCss is not null
            ? officialCss + "\n\n" + DocumentStylesheetLayer.BuildStructuralCss()
            : DocumentStylesheetLayer.BuildFallbackTokens(FallbackPalette, FallbackTypography)
              + "\n\n" + DocumentStylesheetLayer.BuildStructuralCss();
    }

    private string? ReadOfficialCss()
    {
        var fontsPath = Path.Combine(_designSystemRoot, "fonts.css");
        var colorsPath = Path.Combine(_designSystemRoot, "colors_and_type.css");
        if (!File.Exists(colorsPath))
        {
            return null;
        }

        var fonts = File.Exists(fontsPath) ? File.ReadAllText(fontsPath) : string.Empty;
        var colors = File.ReadAllText(colorsPath);
        return fonts + "\n" + colors;
    }

    private Dictionary<string, string> ReadTokens()
    {
        var colorsPath = Path.Combine(_designSystemRoot, "colors_and_type.css");
        if (!File.Exists(colorsPath))
        {
            return new Dictionary<string, string>();
        }

        var content = File.ReadAllText(colorsPath);
        var tokens = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (Match match in TokenPattern.Matches(content))
        {
            tokens[match.Groups[1].Value.Trim()] = match.Groups[2].Value.Trim();
        }

        return tokens;
    }

    private static string Resolve(IReadOnlyDictionary<string, string> tokens, string key, string fallback) =>
        tokens.TryGetValue(key, out var value) && value.StartsWith('#') ? value : fallback;

    private static string ResolveFontFamily(IReadOnlyDictionary<string, string> tokens, string key, string fallback)
    {
        if (!tokens.TryGetValue(key, out var value))
        {
            return fallback;
        }

        // O token vem como uma lista de fallbacks entre aspas (ex.: "DM Sans", "Inter", system-ui, sans-serif).
        // A família oficial é sempre a primeira entrada da lista.
        var firstFamily = value.Split(',')[0].Trim().Trim('"');
        return string.IsNullOrWhiteSpace(firstFamily) ? fallback : firstFamily;
    }
}
