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
/// e assets visuais com a mesma identidade visual do HTML, incluindo a paleta por
/// <see cref="PublicationTheme"/>.
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
        var palette = Palette.From(document.Theme);

        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(0);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Helvetica"));

                page.Content().Column(column =>
                {
                    column.Item().Element(c => ComposeCover(c, document, palette));

                    column.Item().PaddingHorizontal(40).PaddingVertical(16).Row(row =>
                    {
                        row.RelativeItem().Text(document.Theme.HeaderText ?? "BlueprintOS").FontColor(palette.Accent).Bold();
                        row.RelativeItem().AlignRight().Text(document.Metadata.Title).FontColor(palette.Muted);
                    });

                    column.Item().PaddingHorizontal(40).Element(c => ComposeToc(c, document, palette));

                    foreach (var section in document.Sections)
                    {
                        column.Item().PaddingHorizontal(40).PaddingTop(20).Element(c => ComposeSection(c, section, document.Assets, palette));
                    }

                    if (document.Appendix.Count > 0)
                    {
                        column.Item().PaddingHorizontal(40).PaddingTop(24).Text("Apêndice").FontColor(palette.Primary).Bold().FontSize(16);
                        foreach (var section in document.Appendix)
                        {
                            column.Item().PaddingHorizontal(40).PaddingTop(16).Element(c => ComposeSection(c, section, document.Assets, palette));
                        }
                    }

                    if (document.Assets.Attachments.Count > 0)
                    {
                        column.Item().PaddingHorizontal(40).PaddingTop(20).Element(c => ComposeAttachments(c, document.Assets.Attachments, palette));
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

    private static void ComposeCover(IContainer container, PublicationDocument document, Palette palette)
    {
        var metadata = document.Metadata;
        var coverLogo = document.Assets.Logos.FirstOrDefault(l => l.Placement == LogoPlacement.Cover);

        container
            .Background(palette.Accent)
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
                    column.Item().Text("BLUEPRINTOS").FontColor(Colors.White).FontSize(12).Bold();
                }

                column.Item().PaddingTop(8).Text(metadata.Title).FontColor(Colors.White).FontSize(26).Bold();
                column.Item().PaddingTop(6).Text(metadata.Subtitle).FontColor(Colors.White).FontSize(12);
                column.Item().PaddingTop(16).Row(row =>
                {
                    row.RelativeItem().Text($"{metadata.Audience} · Versão {metadata.Version}").FontColor(Colors.White).FontSize(9);
                    row.RelativeItem().AlignRight().Text($"Gerado em {metadata.GeneratedAt:dd/MM/yyyy HH:mm} UTC").FontColor(Colors.White).FontSize(9);
                });

                if (document.Assets.Badges.Count > 0)
                {
                    column.Item().PaddingTop(12).Row(row =>
                    {
                        foreach (var badge in document.Assets.Badges)
                        {
                            row.AutoItem().PaddingRight(8).Element(c => ComposeBadge(c, badge));
                        }
                    });
                }
            });
    }

    private static void ComposeBadge(IContainer container, BadgeAsset badge)
    {
        var color = badge.Status switch
        {
            BadgeStatus.Success => Color.FromHex("#1F8A4C"),
            BadgeStatus.Warning => Color.FromHex("#B8860B"),
            BadgeStatus.Failure => Color.FromHex("#B8341F"),
            _ => Color.FromHex("#4A5568"),
        };

        container.Row(row =>
        {
            row.AutoItem().Background(Color.FromHex("#00000033")).Padding(4).Text(badge.Label).FontColor(Colors.White).FontSize(8);
            row.AutoItem().Background(color).Padding(4).Text(badge.Value).FontColor(Colors.White).FontSize(8).Bold();
        });
    }

    private static void ComposeToc(IContainer container, PublicationDocument document, Palette palette)
    {
        container.Border(1).BorderColor(palette.Border).Padding(16).Column(column =>
        {
            column.Item().Text("Sumário").FontColor(palette.Accent).Bold().FontSize(13);
            foreach (var section in document.Sections)
            {
                column.Item().PaddingTop(4).Text($"• {section.Heading}").FontSize(10);
            }
        });
    }

    private static void ComposeSection(IContainer container, PublicationSection section, PublicationAssets assets, Palette palette)
    {
        container.Column(column =>
        {
            column.Item().Text(section.Heading).FontColor(palette.Accent).Bold().FontSize(15);
            column.Item().PaddingBottom(6).LineHorizontal(1).LineColor(palette.Border);

            foreach (var block in section.Blocks)
            {
                switch (block.Kind)
                {
                    case ContentBlockKind.Heading:
                        column.Item().PaddingTop(8).Text(block.Text).Bold().FontSize(block.Level <= 3 ? 12 : 10.5f);
                        break;
                    case ContentBlockKind.Paragraph:
                        column.Item().PaddingTop(4).Text(text => ComposeInline(text, block.Text ?? string.Empty, 10));
                        break;
                    case ContentBlockKind.BulletList:
                        foreach (var item in block.Items ?? Array.Empty<string>())
                        {
                            column.Item().PaddingTop(2).Row(row =>
                            {
                                row.ConstantItem(12).Text("•");
                                row.RelativeItem().Text(text => ComposeInline(text, item, 10));
                            });
                        }

                        break;
                    case ContentBlockKind.Table:
                        ComposeTable(column, block, palette);
                        break;
                    case ContentBlockKind.CodeBlock:
                        column.Item().PaddingTop(4).Background(Color.FromHex("#10151C")).Padding(8)
                            .Text(block.Text).FontColor(Colors.White).FontFamily("Courier").FontSize(8.5f);
                        break;
                    case ContentBlockKind.Image:
                        ComposeImage(column, block, assets);
                        break;
                }
            }
        });
    }

    private static void ComposeImage(ColumnDescriptor column, ContentBlock block, PublicationAssets assets)
    {
        var image = block.AssetId is not null ? assets.FindEmbeddableImage(block.AssetId) : null;
        if (image is null)
        {
            return;
        }

        column.Item().PaddingTop(6).MaxHeight(240).Image(image.Value.Bytes).FitArea();
        if (!string.IsNullOrEmpty(block.Caption))
        {
            column.Item().PaddingTop(2).Text(block.Caption).FontSize(8.5f).Italic();
        }
    }

    private static void ComposeTable(ColumnDescriptor column, ContentBlock block, Palette palette)
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
                table.Cell().Background(Color.FromHex("#F6F8FA")).Border(1).BorderColor(palette.Border)
                    .Padding(4).Text(text => ComposeInline(text, headerCell, 9, bold: true));
            }

            foreach (var row in rows)
            {
                foreach (var cell in row)
                {
                    table.Cell().Border(1).BorderColor(palette.Border).Padding(4)
                        .Text(text => ComposeInline(text, cell, 9));
                }
            }
        });

        if (!string.IsNullOrEmpty(block.Caption))
        {
            column.Item().Text(block.Caption).FontSize(8.5f).Italic();
        }
    }

    private static void ComposeAttachments(IContainer container, IReadOnlyList<AttachmentAsset> attachments, Palette palette)
    {
        container.Column(column =>
        {
            column.Item().Text("Anexos").FontColor(palette.Accent).Bold().FontSize(15);
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

    private static void ComposeInline(TextDescriptor text, string content, float fontSize, bool bold = false)
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
                textSpan.FontFamily("Courier");
            }
        }
    }

    private readonly record struct Palette(Color Primary, Color Accent, Color Muted, Color Border)
    {
        public static Palette From(PublicationTheme theme) => new(
            Color.FromHex($"#{theme.PrimaryColorHex}"),
            Color.FromHex($"#{theme.AccentColorHex}"),
            Color.FromHex($"#{theme.MutedColorHex}"),
            Color.FromHex($"#{theme.BorderColorHex}"));
    }
}
