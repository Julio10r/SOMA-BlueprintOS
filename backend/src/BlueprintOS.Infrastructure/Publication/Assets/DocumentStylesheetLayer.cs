using BlueprintOS.Core.Publication.Models;

namespace BlueprintOS.Infrastructure.Publication.Assets;

/// <summary>
/// Camada de CSS estrutural (layout de documento: capa, cabeçalho, rodapé, índice, seções,
/// selos, tabelas, blocos de código) usada pelo <see cref="DocumentThemeProvider"/> para montar
/// a folha de estilo final. Referencia exclusivamente as variáveis CSS do Design System oficial
/// (<c>--text-primary</c>, <c>--accent</c>, <c>--border</c>, <c>--surface</c>, <c>--bg</c>,
/// <c>--font</c>, <c>--font-display</c>, <c>--mono</c>, <c>--radius</c>, <c>--shadow-1</c> etc.)
/// — nunca define uma cor ou fonte diretamente, evitando duplicar o Design System.
/// </summary>
internal static class DocumentStylesheetLayer
{
    /// <summary>
    /// Define as mesmas variáveis usadas pela camada estrutural, com os valores oficiais de
    /// fallback — usado apenas quando <c>docs/design-system/colors_and_type.css</c> não está
    /// presente, para que a folha de estilo final continue funcionando sem o Design System.
    /// </summary>
    public static string BuildFallbackTokens(DocumentPalette palette, DocumentTypography typography) => $$"""
        :root {
            --text-primary: {{palette.TextPrimaryHex}};
            --accent: {{palette.AccentHex}};
            --text-secondary: {{palette.MutedHex}};
            --border: {{palette.BorderHex}};
            --surface: {{palette.SurfaceHex}};
            --bg: {{palette.BackgroundHex}};
            --aprovado: {{palette.SuccessHex}};
            --avaliacao: {{palette.WarningHex}};
            --rejeitado: {{palette.ErrorHex}};
            --novo: {{palette.InfoHex}};
            --font-display: "{{typography.DisplayFontFamily}}", Arial, sans-serif;
            --font: "{{typography.BodyFontFamily}}", system-ui, sans-serif;
            --mono: "{{typography.MonoFontFamily}}", ui-monospace, monospace;
            --radius: 12px;
            --radius-sm: 8px;
            --shadow-1: 0 2px 8px rgba(26,25,22,0.07);
        }
        """;

    /// <summary>
    /// Layout de documento (não faz parte do Design System, que não define templates de
    /// documento — ver <c>docs/design-system/INDEX.md</c>): usa somente <c>var(...)</c> sobre os
    /// tokens acima/oficiais, nunca cor ou fonte literal.
    /// </summary>
    public static string BuildStructuralCss() => """
        * { box-sizing: border-box; }
        body {
            margin: 0;
            font-family: var(--font);
            color: var(--text-primary);
            background: var(--bg);
            line-height: 1.5;
        }
        .cover {
            min-height: 55vh;
            display: flex;
            flex-direction: column;
            justify-content: center;
            align-items: flex-start;
            gap: 0.75rem;
            padding: 4rem clamp(1.5rem, 6vw, 6rem);
            background: var(--text-primary);
            color: var(--bg);
        }
        .cover-brand { font-family: var(--font-display); font-weight: 700; letter-spacing: 0.04em; text-transform: uppercase; opacity: 0.85; }
        .cover-logo { max-height: 3rem; max-width: 12rem; object-fit: contain; }
        .cover h1 { font-family: var(--font-display); font-weight: 700; letter-spacing: -0.02em; font-size: clamp(2rem, 4vw, 3rem); margin: 0; }
        .cover-subtitle { font-size: 1.15rem; opacity: 0.9; max-width: 42rem; }
        .cover-meta { display: flex; flex-wrap: wrap; gap: 1.5rem; margin-top: 1rem; font-size: 0.95rem; opacity: 0.85; }
        .cover-badges { display: flex; flex-wrap: wrap; gap: 0.5rem; margin-top: 1rem; }
        .badge {
            display: inline-flex; overflow: hidden; border-radius: var(--radius-sm); font-size: 0.8rem; font-weight: 600;
        }
        .badge-label, .badge-value { padding: 0.2rem 0.5rem; }
        .badge-label { background: rgba(247,246,243,0.15); }
        .badge-value { color: var(--bg); }
        .badge-success .badge-value { background: var(--aprovado); }
        .badge-warning .badge-value { background: var(--avaliacao); }
        .badge-failure .badge-value { background: var(--rejeitado); }
        .badge-neutral .badge-value { background: var(--novo); }
        .page-header {
            position: sticky; top: 0; z-index: 10;
            display: flex; justify-content: space-between; align-items: center;
            padding: 0.9rem clamp(1.5rem, 6vw, 6rem);
            background: var(--surface);
            border-bottom: 1px solid var(--border);
        }
        .page-header-brand { font-family: var(--font-display); font-weight: 700; color: var(--accent); }
        .page-header-title { color: var(--text-secondary); font-size: 0.95rem; }
        .content { max-width: 60rem; margin: 0 auto; padding: 2rem clamp(1rem, 6vw, 6rem) 4rem; }
        .card {
            background: var(--surface);
            border: 1px solid var(--border);
            border-radius: var(--radius);
            box-shadow: var(--shadow-1);
            padding: 1.5rem;
            margin-bottom: 2rem;
        }
        .toc ol { padding-left: 1.25rem; }
        .toc a { color: var(--accent); text-decoration: none; }
        .toc a:hover { text-decoration: underline; }
        .section { margin-bottom: 2.5rem; }
        .section h2, .section h3 {
            font-family: var(--font-display);
            border-bottom: 2px solid var(--border);
            padding-bottom: 0.5rem;
            color: var(--accent);
        }
        .appendix-divider { margin: 3rem 0 1.5rem; }
        .appendix-divider h2 { font-family: var(--font-display); color: var(--accent); border-bottom: 2px solid var(--border); padding-bottom: 0.5rem; }
        .section-body table { width: 100%; border-collapse: collapse; margin: 1rem 0; }
        .section-body th, .section-body td {
            border: 1px solid var(--border);
            padding: 0.5rem 0.75rem;
            text-align: left;
        }
        .section-body th { background: var(--surface); }
        .section-body code { font-family: var(--mono); background: var(--surface); padding: 0.1rem 0.35rem; border-radius: 4px; }
        .section-body pre { font-family: var(--mono); background: var(--text-primary); color: var(--bg); padding: 1rem; border-radius: var(--radius-sm); overflow-x: auto; }
        .section-body figure { margin: 1rem 0; }
        .section-body figure img { max-width: 100%; border-radius: var(--radius-sm); border: 1px solid var(--border); }
        .section-body figcaption { font-size: 0.85rem; color: var(--text-secondary); margin-top: 0.35rem; }
        .page-footer {
            display: flex; justify-content: space-between; flex-wrap: wrap; gap: 0.5rem;
            padding: 1.25rem clamp(1.5rem, 6vw, 6rem);
            color: var(--text-secondary); font-size: 0.85rem;
            border-top: 1px solid var(--border);
        }
        @media (max-width: 40rem) {
            .cover { padding: 3rem 1.25rem; }
            .page-header { flex-direction: column; align-items: flex-start; gap: 0.25rem; }
        }
        """;
}
