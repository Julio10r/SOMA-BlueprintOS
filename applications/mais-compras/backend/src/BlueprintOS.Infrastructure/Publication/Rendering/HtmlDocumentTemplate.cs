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
        builder.AppendLine($"<style>{theme.Stylesheet}</style>");
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
}
