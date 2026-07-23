using System.Net;
using System.Text;
using BlueprintOS.Core.Publication.Models;
using Markdig;

namespace BlueprintOS.Infrastructure.Publication.Rendering;

/// <summary>
/// Monta o HTML completo de um <see cref="PublicationDocument"/>: capa, cabeçalho, índice,
/// seções (convertidas de Markdown para HTML via Markdig) e rodapé, com CSS embutido (sem
/// frameworks), layout limpo e responsivo.
/// </summary>
internal static class HtmlDocumentTemplate
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    public static string Render(PublicationDocument document)
    {
        var builder = new StringBuilder();

        builder.AppendLine("<!doctype html>");
        builder.AppendLine("<html lang=\"pt-BR\">");
        builder.AppendLine("<head>");
        builder.AppendLine("<meta charset=\"utf-8\" />");
        builder.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />");
        builder.AppendLine($"<title>{Encode(document.Title)}</title>");
        builder.AppendLine($"<style>{Css}</style>");
        builder.AppendLine("</head>");
        builder.AppendLine("<body>");

        AppendCover(builder, document);
        AppendHeader(builder, document);
        builder.AppendLine("<main class=\"content\">");
        AppendToc(builder, document);
        foreach (var section in document.Sections)
        {
            builder.AppendLine($"<section id=\"{MarkdownRenderer.Slugify(section.Heading)}\" class=\"section\">");
            builder.AppendLine($"<h2>{Encode(section.Heading)}</h2>");
            builder.AppendLine("<div class=\"section-body\">");
            builder.AppendLine(Markdown.ToHtml(section.MarkdownBody, Pipeline));
            builder.AppendLine("</div>");
            builder.AppendLine("</section>");
        }

        builder.AppendLine("</main>");
        AppendFooter(builder, document);
        builder.AppendLine("</body>");
        builder.AppendLine("</html>");

        return builder.ToString();
    }

    private static void AppendCover(StringBuilder builder, PublicationDocument document)
    {
        builder.AppendLine("<section class=\"cover\">");
        builder.AppendLine("<div class=\"cover-brand\">BlueprintOS</div>");
        builder.AppendLine($"<h1>{Encode(document.Title)}</h1>");
        builder.AppendLine($"<p class=\"cover-subtitle\">{Encode(document.Subtitle)}</p>");
        builder.AppendLine("<div class=\"cover-meta\">");
        builder.AppendLine($"<span>Versão {Encode(document.ProjectVersion)}</span>");
        builder.AppendLine($"<span>Gerado em {document.GeneratedAt:dd/MM/yyyy HH:mm} UTC</span>");
        builder.AppendLine("</div>");
        builder.AppendLine("</section>");
    }

    private static void AppendHeader(StringBuilder builder, PublicationDocument document)
    {
        builder.AppendLine("<header class=\"page-header\">");
        builder.AppendLine("<span class=\"page-header-brand\">BlueprintOS</span>");
        builder.AppendLine($"<span class=\"page-header-title\">{Encode(document.Title)}</span>");
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

    private static void AppendFooter(StringBuilder builder, PublicationDocument document)
    {
        builder.AppendLine("<footer class=\"page-footer\">");
        builder.AppendLine($"<span>BlueprintOS · {Encode(document.Title)} · v{Encode(document.ProjectVersion)}</span>");
        builder.AppendLine("<span>Gerado automaticamente pelo Publication Engine</span>");
        builder.AppendLine("</footer>");
    }

    private static string Encode(string text) => WebUtility.HtmlEncode(text);

    private const string Css = """
        :root {
            color-scheme: light dark;
            --ink: #1c2530;
            --muted: #5b6a7a;
            --accent: #2e5c8a;
            --border: #dfe4ea;
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
            background: linear-gradient(135deg, var(--accent), #16324f);
            color: #fff;
        }
        .cover-brand { font-weight: 600; letter-spacing: 0.08em; text-transform: uppercase; opacity: 0.85; }
        .cover h1 { font-size: clamp(2rem, 4vw, 3rem); margin: 0; }
        .cover-subtitle { font-size: 1.15rem; opacity: 0.9; max-width: 42rem; }
        .cover-meta { display: flex; gap: 1.5rem; margin-top: 1rem; font-size: 0.95rem; opacity: 0.85; }
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
        .section h2 {
            border-bottom: 2px solid var(--border);
            padding-bottom: 0.5rem;
            color: var(--accent);
        }
        .section-body table { width: 100%; border-collapse: collapse; margin: 1rem 0; }
        .section-body th, .section-body td {
            border: 1px solid var(--border);
            padding: 0.5rem 0.75rem;
            text-align: left;
        }
        .section-body th { background: var(--surface-alt); }
        .section-body code { background: var(--surface-alt); padding: 0.1rem 0.35rem; border-radius: 0.25rem; }
        .section-body pre { background: #10151c; color: #e8edf3; padding: 1rem; border-radius: 0.5rem; overflow-x: auto; }
        .page-footer {
            display: flex; justify-content: space-between; flex-wrap: wrap; gap: 0.5rem;
            padding: 1.25rem clamp(1.5rem, 6vw, 6rem);
            color: var(--muted); font-size: 0.85rem;
            border-top: 1px solid var(--border);
        }
        @media (prefers-color-scheme: dark) {
            :root {
                --ink: #e8edf3; --muted: #9fb0c3; --border: #2a3644;
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
