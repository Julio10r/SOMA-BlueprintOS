using BlueprintOS.Core.Publication.Contracts;
using BlueprintOS.Core.Publication.Models;
using BlueprintOS.Core.Publication.Models.Assets;
using BlueprintOS.Infrastructure.Publication.Content;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BlueprintOS.Infrastructure.Publication.Rendering;

/// <summary>
/// Implementação de <see cref="IContentRenderer"/> que produz um PDF pronto para impressão
/// diretamente a partir do mesmo modelo estruturado (<see cref="ContentBlock"/>) consumido
/// pelo <see cref="HtmlRenderer"/> — nenhum dos dois deriva do outro, e não há conversão de
/// HTML para PDF em nenhum momento. Usa QuestPDF (biblioteca .NET pura, sem dependência de
/// navegador/Chromium) para desenhar capa, cabeçalho, rodapé, índice, seções, apêndice, anexos
/// e assets visuais com a mesma identidade visual do HTML: cores e fontes vêm sempre de
/// <see cref="PublicationTheme"/> (Design System oficial), nunca hardcoded aqui.
/// </summary>
public sealed class PdfRenderer : IContentRenderer
{
    static PdfRenderer()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    /// <inheritdoc />
    public PublicationFormat Format => PublicationFormat.Pdf;

    /// <inheritdoc />
    public Task<byte[]> RenderAsync(PublicationDocument document, CancellationToken cancellationToken = default)
    {
        var palette = Palette.From(document.Theme.Palette);
        var typography = document.Theme.Typography;

        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(0);
                page.PageColor(palette.Surface);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily(typography.BodyFontFamily).FontColor(palette.Ink));

                page.Content().Column(column =>
                {
                    column.Item().Element(c => ComposeCover(c, document, palette, typography));

                    column.Item().PaddingHorizontal(40).PaddingVertical(16).Row(row =>
                    {
                        row.RelativeItem().Text(document.Theme.HeaderText ?? "BlueprintOS").FontColor(palette.Accent).FontFamily(typography.DisplayFontFamily).Bold();
                        row.RelativeItem().AlignRight().Text(document.Metadata.Title).FontColor(palette.Muted);
                    });

                    column.Item().PaddingHorizontal(40).Element(c => ComposeToc(c, document, palette, typography));

                    foreach (var section in document.Sections)
                    {
                        column.Item().PaddingHorizontal(40).PaddingTop(20).Element(c => ComposeSection(c, section, document.Assets, palette, typography));
                    }

                    if (document.Appendix.Count > 0)
                    {
                        column.Item().PaddingHorizontal(40).PaddingTop(24).Text("Apêndice").FontColor(palette.Accent).FontFamily(typography.DisplayFontFamily).Bold().FontSize(16);
                        foreach (var section in document.Appendix)
                        {
                            column.Item().PaddingHorizontal(40).PaddingTop(16).Element(c => ComposeSection(c, section, document.Assets, palette, typography));
                        }
                    }

                    if (document.Assets.Attachments.Count > 0)
                    {
                        column.Item().PaddingHorizontal(40).PaddingTop(20).Element(c => ComposeAttachments(c, document.Assets.Attachments, palette, typography));
                    }

                    column.Item().PaddingHorizontal(40).PaddingVertical(20).Column(footer =>
                    {
                        footer.Item().LineHorizontal(1).LineColor(palette.Border);
                        footer.Item().PaddingTop(8).Row(row =>
                        {
                            var footerText = document.Theme.FooterText ?? $"BlueprintOS · {document.Metadata.Title} · v{document.Metadata.Version}";
                            row.RelativeItem().Text(footerText).FontColor(palette.Muted).FontSize(8);
                            row.RelativeItem().AlignRight().Text($"Gerado em {document.Metadata.GeneratedAt:dd/MM/yyyy HH:mm} UTC").FontColor(palette.Muted).FontSize(8);
                        });
                    });
                });
            });
        }).GeneratePdf();

        return Task.FromResult(bytes);
    }

    private static void ComposeCover(IContainer container, PublicationDocument document, Palette palette, DocumentTypography typography)
    {
        var metadata = document.Metadata;
        var coverLogo = document.Assets.Logos.FirstOrDefault(l => l.Placement == LogoPlacement.Cover);

        container
            .Background(palette.Ink)
            .Padding(40)
            .MinHeight(220)
            .Column(column =>
            {
                if (coverLogo is not null)
                {
                    column.Item().MaxHeight(40).Image(coverLogo.Bytes);
                }
                else
                {
                    column.Item().Text("BLUEPRINTOS").FontColor(palette.Background).FontFamily(typography.DisplayFontFamily).FontSize(12).Bold();
                }

                column.Item().PaddingTop(8).Text(metadata.Title).FontColor(palette.Background).FontFamily(typography.DisplayFontFamily).FontSize(26).Bold();
                column.Item().PaddingTop(6).Text(metadata.Subtitle).FontColor(palette.Background).FontSize(12);
                column.Item().PaddingTop(16).Row(row =>
                {
                    row.RelativeItem().Text($"{metadata.Audience} · Versão {metadata.Version}").FontColor(palette.Background).FontSize(9);
                    row.RelativeItem().AlignRight().Text($"Gerado em {metadata.GeneratedAt:dd/MM/yyyy HH:mm} UTC").FontColor(palette.Background).FontSize(9);
                });

                if (document.Assets.Badges.Count > 0)
                {
                    column.Item().PaddingTop(12).Row(row =>
                    {
                        foreach (var badge in document.Assets.Badges)
                        {
                            row.AutoItem().PaddingRight(8).Element(c => ComposeBadge(c, badge, palette));
                        }
                    });
                }
            });
    }

    private static void ComposeBadge(IContainer container, BadgeAsset badge, Palette palette)
    {
        var color = badge.Status switch
        {
            BadgeStatus.Success => palette.Success,
            BadgeStatus.Warning => palette.Warning,
            BadgeStatus.Failure => palette.Error,
            _ => palette.Info,
        };

        container.Row(row =>
        {
            row.AutoItem().Background(palette.Ink).Padding(4).Text(badge.Label).FontColor(palette.Background).FontSize(8);
            row.AutoItem().Background(color).Padding(4).Text(badge.Value).FontColor(palette.Background).FontSize(8).Bold();
        });
    }

    private static void ComposeToc(IContainer container, PublicationDocument document, Palette palette, DocumentTypography typography)
    {
        container.Border(1).BorderColor(palette.Border).Padding(16).Column(column =>
        {
            column.Item().Text("Sumário").FontColor(palette.Accent).FontFamily(typography.DisplayFontFamily).Bold().FontSize(13);
            foreach (var section in document.Sections)
            {
                column.Item().PaddingTop(4).Text($"• {section.Heading}").FontSize(10);
            }
        });
    }

    private static void ComposeSection(IContainer container, PublicationSection section, PublicationAssets assets, Palette palette, DocumentTypography typography)
    {
        container.Column(column =>
        {
            column.Item().Text(section.Heading).FontColor(palette.Accent).FontFamily(typography.DisplayFontFamily).Bold().FontSize(15);
            column.Item().PaddingBottom(6).LineHorizontal(1).LineColor(palette.Border);

            foreach (var block in section.Blocks)
            {
                switch (block.Kind)
                {
                    case ContentBlockKind.Heading:
                        column.Item().PaddingTop(8).Text(block.Text).Bold().FontSize(block.Level <= 3 ? 12 : 10.5f);
                        break;
                    case ContentBlockKind.Paragraph:
                        column.Item().PaddingTop(4).Text(text => ComposeInline(text, block.Text ?? string.Empty, 10, typography));
                        break;
                    case ContentBlockKind.BulletList:
                        foreach (var item in block.Items ?? Array.Empty<string>())
                        {
                            column.Item().PaddingTop(2).Row(row =>
                            {
                                row.ConstantItem(12).Text("•");
                                row.RelativeItem().Text(text => ComposeInline(text, item, 10, typography));
                            });
                        }

                        break;
                    case ContentBlockKind.Table:
                        ComposeTable(column, block, palette, typography);
                        break;
                    case ContentBlockKind.CodeBlock:
                        column.Item().PaddingTop(4).Background(palette.Ink).Padding(8)
                            .Text(block.Text).FontColor(palette.Background).FontFamily(typography.MonoFontFamily).FontSize(8.5f);
                        break;
                    case ContentBlockKind.Image:
                        ComposeImage(column, block, assets, palette);
                        break;
                }
            }
        });
    }

    private static void ComposeImage(ColumnDescriptor column, ContentBlock block, PublicationAssets assets, Palette palette)
    {
        var image = block.AssetId is not null ? assets.FindEmbeddableImage(block.AssetId) : null;
        if (image is null)
        {
            return;
        }

        // QuestPDF/SkiaSharp só decodifica formatos raster (PNG/JPEG); SVG (usado pelos
        // diagramas Mermaid gerados automaticamente) é suportado pelo HTML via data URI, mas não
        // aqui — em vez de derrubar a geração do PDF, mostra a legenda como texto.
        if (string.Equals(image.Value.MediaType, "image/svg+xml", StringComparison.OrdinalIgnoreCase))
        {
            ComposeImagePlaceholder(column, block, palette);
            return;
        }

        try
        {
            column.Item().PaddingTop(6).MaxHeight(240).Image(image.Value.Bytes).FitArea();
        }
        catch (QuestPDF.Drawing.Exceptions.DocumentComposeException)
        {
            ComposeImagePlaceholder(column, block, palette);
            return;
        }

        if (!string.IsNullOrEmpty(block.Caption))
        {
            column.Item().PaddingTop(2).Text(block.Caption).FontSize(8.5f).Italic();
        }
    }

    private static void ComposeImagePlaceholder(ColumnDescriptor column, ContentBlock block, Palette palette)
    {
        column.Item().PaddingTop(6).Border(1).BorderColor(palette.Border).Background(palette.Surface).Padding(12)
            .Text(block.Caption ?? "Imagem não disponível nesta versão do documento.")
            .FontColor(palette.Muted).FontSize(9).Italic();
    }

    private static void ComposeTable(ColumnDescriptor column, ContentBlock block, Palette palette, DocumentTypography typography)
    {
        var header = block.TableHeader ?? Array.Empty<string>();
        var rows = block.TableRows ?? Array.Empty<IReadOnlyList<string>>();
        var columnCount = header.Count;
        if (columnCount == 0)
        {
            return;
        }

        column.Item().PaddingVertical(6).Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                for (var i = 0; i < columnCount; i++)
                {
                    columns.RelativeColumn();
                }
            });

            foreach (var headerCell in header)
            {
                table.Cell().Background(palette.Surface).Border(1).BorderColor(palette.Border)
                    .Padding(4).Text(text => ComposeInline(text, headerCell, 9, typography, bold: true));
            }

            foreach (var row in rows)
            {
                foreach (var cell in row)
                {
                    table.Cell().Border(1).BorderColor(palette.Border).Padding(4)
                        .Text(text => ComposeInline(text, cell, 9, typography));
                }
            }
        });

        if (!string.IsNullOrEmpty(block.Caption))
        {
            column.Item().Text(block.Caption).FontSize(8.5f).Italic();
        }
    }

    private static void ComposeAttachments(IContainer container, IReadOnlyList<AttachmentAsset> attachments, Palette palette, DocumentTypography typography)
    {
        container.Column(column =>
        {
            column.Item().Text("Anexos").FontColor(palette.Accent).FontFamily(typography.DisplayFontFamily).Bold().FontSize(15);
            column.Item().PaddingBottom(6).LineHorizontal(1).LineColor(palette.Border);
            foreach (var attachment in attachments)
            {
                column.Item().PaddingTop(4).Text(text =>
                {
                    text.Span(attachment.FileName).Bold().FontSize(10);
                    text.Span($" — {attachment.Description}").FontSize(10);
                });
            }
        });
    }

    private static void ComposeInline(TextDescriptor text, string content, float fontSize, DocumentTypography typography, bool bold = false)
    {
        foreach (var span in InlineSpanParser.Parse(content))
        {
            var textSpan = text.Span(span.Text).FontSize(fontSize);
            if (bold || span.Kind == InlineSpanKind.Bold)
            {
                textSpan.Bold();
            }

            if (span.Kind == InlineSpanKind.Code)
            {
                textSpan.FontFamily(typography.MonoFontFamily);
            }
        }
    }

    private readonly record struct Palette(
        Color Ink, Color Accent, Color Muted, Color Border, Color Surface, Color Background,
        Color Success, Color Warning, Color Error, Color Info)
    {
        public static Palette From(DocumentPalette palette) => new(
            Color.FromHex(palette.TextPrimaryHex),
            Color.FromHex(palette.AccentHex),
            Color.FromHex(palette.MutedHex),
            Color.FromHex(palette.BorderHex),
            Color.FromHex(palette.SurfaceHex),
            Color.FromHex(palette.BackgroundHex),
            Color.FromHex(palette.SuccessHex),
            Color.FromHex(palette.WarningHex),
            Color.FromHex(palette.ErrorHex),
            Color.FromHex(palette.InfoHex));
    }
}
