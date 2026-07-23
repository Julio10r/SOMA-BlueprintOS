using System.Net;
using System.Text;
using BlueprintOS.Core.Publication.Models;
using BlueprintOS.Core.Publication.Models.Assets;

namespace BlueprintOS.Infrastructure.Publication.Rendering;

/// <summary>
/// Monta o HTML completo de um <see cref="PublicationDocument"/>: capa (com logo/selos quando
/// presentes), cabeçalho, índice, seções, apêndice, anexos e rodapé, com CSS embutido (sem
/// frameworks), paleta de cores por <see cref="PublicationTheme"/> e layout limpo e responsivo.
/// As seções são escritas diretamente a partir dos mesmos <see cref="ContentBlock"/> consumidos
/// pelo <see cref="PdfRenderer"/> (via <see cref="ContentBlockHtmlWriter"/>) — não há conversão
/// de Markdown para HTML; ambos os formatos derivam do mesmo modelo estruturado.
/// </summary>
internal static class HtmlDocumentTemplate
{
    public static string Render(PublicationDocument document)
    {
        var builder = new StringBuilder();
        var theme = document.Theme;

        builder.AppendLine("<!doctype html>");
        builder.AppendLine("<html lang=\"pt-BR\">");
        builder.AppendLine("<head>");
        builder.AppendLine("<meta charset=\"utf-8\" />");
        builder.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />");
        builder.AppendLine($"<title>{Encode(document.Metadata.Title)}</title>");
        builder.AppendLine($"<style>{BuildCss(theme)}</style>");
        builder.AppendLine("</head>");
        builder.AppendLine("<body>");

        AppendCover(builder, document);
        AppendHeader(builder, document);
        builder.AppendLine("<main class=\"content\">");
        AppendToc(builder, document);

        foreach (var section in document.Sections)
        {
            AppendSection(builder, section, document.Assets, headingTag: "h2");
        }

        if (document.Appendix.Count > 0)
        {
            builder.AppendLine("<section class=\"appendix-divider\"><h2>Apêndice</h2></section>");
            foreach (var section in document.Appendix)
            {
                AppendSection(builder, section, document.Assets, headingTag: "h3");
            }
        }

        AppendAttachments(builder, document.Assets.Attachments);

        builder.AppendLine("</main>");
        AppendFooter(builder, document);
        builder.AppendLine("</body>");
        builder.AppendLine("</html>");

        return builder.ToString();
    }

    private static void AppendSection(StringBuilder builder, PublicationSection section, PublicationAssets assets, string headingTag)
    {
        builder.AppendLine($"<section id=\"{MarkdownRenderer.Slugify(section.Heading)}\" class=\"section\">");
        builder.AppendLine($"<{headingTag}>{Encode(section.Heading)}</{headingTag}>");
        builder.AppendLine("<div class=\"section-body\">");
        ContentBlockHtmlWriter.Write(builder, section.Blocks, assets);
        builder.AppendLine("</div>");
        builder.AppendLine("</section>");
    }

    private static void AppendCover(StringBuilder builder, PublicationDocument document)
    {
        var metadata = document.Metadata;
        var coverLogo = document.Assets.Logos.FirstOrDefault(l => l.Placement == LogoPlacement.Cover);

        builder.AppendLine("<section class=\"cover\">");
        if (coverLogo is not null)
        {
            builder.AppendLine($"<img class=\"cover-logo\" src=\"data:{coverLogo.MediaType};base64,{Convert.ToBase64String(coverLogo.Bytes)}\" alt=\"{Encode(coverLogo.AltText)}\" />");
        }
        else
        {
            builder.AppendLine("<div class=\"cover-brand\">BLUEPRINTOS</div>");
        }

        builder.AppendLine($"<h1>{Encode(metadata.Title)}</h1>");
        builder.AppendLine($"<p class=\"cover-subtitle\">{Encode(metadata.Subtitle)}</p>");
        builder.AppendLine("<div class=\"cover-meta\">");
        builder.AppendLine($"<span>{Encode(metadata.Audience)}</span>");
        builder.AppendLine($"<span>Versão {Encode(metadata.Version)}</span>");
        builder.AppendLine($"<span>Gerado em {metadata.GeneratedAt:dd/MM/yyyy HH:mm} UTC</span>");
        builder.AppendLine($"<span>{Encode(metadata.Classification.ToString())}</span>");
        builder.AppendLine("</div>");

        if (document.Assets.Badges.Count > 0)
        {
            builder.AppendLine("<div class=\"cover-badges\">");
            foreach (var badge in document.Assets.Badges)
            {
                builder.AppendLine($"<span class=\"badge badge-{badge.Status.ToString().ToLowerInvariant()}\"><span class=\"badge-label\">{Encode(badge.Label)}</span><span class=\"badge-value\">{Encode(badge.Value)}</span></span>");
            }

            builder.AppendLine("</div>");
        }

        builder.AppendLine("</section>");
    }

    private static void AppendHeader(StringBuilder builder, PublicationDocument document)
    {
        var headerText = document.Theme.HeaderText ?? document.Metadata.Title;
        builder.AppendLine("<header class=\"page-header\">");
        builder.AppendLine("<span class=\"page-header-brand\">BlueprintOS</span>");
        builder.AppendLine($"<span class=\"page-header-title\">{Encode(headerText)}</span>");
        builder.AppendLine("</header>");
    }

    private static void AppendToc(StringBuilder builder, PublicationDocument document)
    {
        builder.AppendLine("<nav class=\"toc card\">");
        builder.AppendLine("<h2>Sumário</h2>");
        builder.AppendLine("<ol>");
        foreach (var section in document.Sections)
        {
            builder.AppendLine($"<li><a href=\"#{MarkdownRenderer.Slugify(section.Heading)}\">{Encode(section.Heading)}</a></li>");
        }

        builder.AppendLine("</ol>");
        builder.AppendLine("</nav>");
    }

    private static void AppendAttachments(StringBuilder builder, IReadOnlyList<AttachmentAsset> attachments)
    {
        if (attachments.Count == 0)
        {
            return;
        }

        builder.AppendLine("<section class=\"section\"><h2>Anexos</h2><ul>");
        foreach (var attachment in attachments)
        {
            builder.AppendLine($"<li><a href=\"./attachments/{Uri.EscapeDataString(attachment.FileName)}\">{Encode(attachment.FileName)}</a> — {Encode(attachment.Description)}</li>");
        }

        builder.AppendLine("</ul></section>");
    }

    private static void AppendFooter(StringBuilder builder, PublicationDocument document)
    {
        var footerText = document.Theme.FooterText ?? $"BlueprintOS · {document.Metadata.Title} · v{document.Metadata.Version}";
        builder.AppendLine("<footer class=\"page-footer\">");
        builder.AppendLine($"<span>{Encode(footerText)}</span>");
        builder.AppendLine("<span>Gerado automaticamente pelo Publication Engine</span>");
        builder.AppendLine("</footer>");
    }

    private static string Encode(string text) => WebUtility.HtmlEncode(text);

    private static string BuildCss(PublicationTheme theme) => $$"""
        :root {
            color-scheme: light dark;
            --ink: #1c2530;
            --muted: #{{theme.MutedColorHex}};
            --accent: #{{theme.AccentColorHex}};
            --primary: #{{theme.PrimaryColorHex}};
            --border: #{{theme.BorderColorHex}};
            --surface: #ffffff;
            --surface-alt: #f6f8fa;
            font-synthesis: none;
        }
        * { box-sizing: border-box; }
        body {
            margin: 0;
            font-family: "Segoe UI", Helvetica, Arial, sans-serif;
            color: var(--ink);
            background: var(--surface-alt);
            line-height: 1.6;
        }
        .cover {
            min-height: 60vh;
            display: flex;
            flex-direction: column;
            justify-content: center;
            align-items: flex-start;
            gap: 0.75rem;
            padding: 4rem clamp(1.5rem, 6vw, 6rem);
            background: linear-gradient(135deg, var(--accent), var(--primary));
            color: #fff;
        }
        .cover-brand { font-weight: 600; letter-spacing: 0.08em; text-transform: uppercase; opacity: 0.85; }
        .cover-logo { max-height: 3rem; max-width: 12rem; object-fit: contain; }
        .cover h1 { font-size: clamp(2rem, 4vw, 3rem); margin: 0; }
        .cover-subtitle { font-size: 1.15rem; opacity: 0.9; max-width: 42rem; }
        .cover-meta { display: flex; flex-wrap: wrap; gap: 1.5rem; margin-top: 1rem; font-size: 0.95rem; opacity: 0.85; }
        .cover-badges { display: flex; flex-wrap: wrap; gap: 0.5rem; margin-top: 1rem; }
        .badge {
            display: inline-flex; overflow: hidden; border-radius: 0.3rem; font-size: 0.8rem; font-weight: 600;
        }
        .badge-label, .badge-value { padding: 0.2rem 0.5rem; }
        .badge-label { background: rgba(255,255,255,0.15); }
        .badge-value { color: #fff; }
        .badge-success .badge-value { background: #1f8a4c; }
        .badge-warning .badge-value { background: #b8860b; }
        .badge-failure .badge-value { background: #b8341f; }
        .badge-neutral .badge-value { background: #4a5568; }
        .page-header {
            position: sticky; top: 0; z-index: 10;
            display: flex; justify-content: space-between; align-items: center;
            padding: 0.9rem clamp(1.5rem, 6vw, 6rem);
            background: var(--surface);
            border-bottom: 1px solid var(--border);
        }
        .page-header-brand { font-weight: 700; color: var(--accent); }
        .page-header-title { color: var(--muted); font-size: 0.95rem; }
        .content { max-width: 60rem; margin: 0 auto; padding: 2rem clamp(1rem, 6vw, 6rem) 4rem; }
        .card {
            background: var(--surface);
            border: 1px solid var(--border);
            border-radius: 0.75rem;
            padding: 1.5rem;
            margin-bottom: 2rem;
        }
        .toc ol { padding-left: 1.25rem; }
        .toc a { color: var(--accent); text-decoration: none; }
        .toc a:hover { text-decoration: underline; }
        .section { margin-bottom: 2.5rem; }
        .section h2, .section h3 {
            border-bottom: 2px solid var(--border);
            padding-bottom: 0.5rem;
            color: var(--accent);
        }
        .appendix-divider { margin: 3rem 0 1.5rem; }
        .appendix-divider h2 { color: var(--primary); border-bottom: 2px solid var(--border); padding-bottom: 0.5rem; }
        .section-body table { width: 100%; border-collapse: collapse; margin: 1rem 0; }
        .section-body th, .section-body td {
            border: 1px solid var(--border);
            padding: 0.5rem 0.75rem;
            text-align: left;
        }
        .section-body th { background: var(--surface-alt); }
        .section-body code { background: var(--surface-alt); padding: 0.1rem 0.35rem; border-radius: 0.25rem; }
        .section-body pre { background: #10151c; color: #e8edf3; padding: 1rem; border-radius: 0.5rem; overflow-x: auto; }
        .section-body figure { margin: 1rem 0; }
        .section-body figure img { max-width: 100%; border-radius: 0.5rem; border: 1px solid var(--border); }
        .section-body figcaption { font-size: 0.85rem; color: var(--muted); margin-top: 0.35rem; }
        .page-footer {
            display: flex; justify-content: space-between; flex-wrap: wrap; gap: 0.5rem;
            padding: 1.25rem clamp(1.5rem, 6vw, 6rem);
            color: var(--muted); font-size: 0.85rem;
            border-top: 1px solid var(--border);
        }
        @media (prefers-color-scheme: dark) {
            :root {
                --ink: #e8edf3; --border: #2a3644;
                --surface: #141a22; --surface-alt: #0f1319;
            }
            .section-body pre { background: #05070a; }
        }
        @media (max-width: 40rem) {
            .cover { padding: 3rem 1.25rem; }
            .page-header { flex-direction: column; align-items: flex-start; gap: 0.25rem; }
        }
        """;
}
