using BlueprintOS.Core.Publication.Contracts;
using BlueprintOS.Core.Publication.Models;
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
/// navegador/Chromium) para desenhar capa, cabeçalho, rodapé, índice, seções, listas e
/// tabelas com a mesma identidade visual do HTML.
/// </summary>
public sealed class PdfRenderer : IContentRenderer
{
    private static readonly Color AccentColor = Color.FromHex("#2E5C8A");
    private static readonly Color MutedColor = Color.FromHex("#5B6A7A");
    private static readonly Color BorderColor = Color.FromHex("#DFE4EA");

    static PdfRenderer()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    /// <inheritdoc />
    public PublicationFormat Format => PublicationFormat.Pdf;

    /// <inheritdoc />
    public Task<byte[]> RenderAsync(PublicationDocument document, CancellationToken cancellationToken = default)
    {
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
                    column.Item().Element(c => ComposeCover(c, document));

                    column.Item().PaddingHorizontal(40).PaddingVertical(16).Row(row =>
                    {
                        row.RelativeItem().Text("BlueprintOS").FontColor(AccentColor).Bold();
                        row.RelativeItem().AlignRight().Text(document.Title).FontColor(MutedColor);
                    });

                    column.Item().PaddingHorizontal(40).Element(c => ComposeToc(c, document));

                    foreach (var section in document.Sections)
                    {
                        column.Item().PaddingHorizontal(40).PaddingTop(20).Element(c => ComposeSection(c, section));
                    }

                    column.Item().PaddingHorizontal(40).PaddingVertical(20).Column(footer =>
                    {
                        footer.Item().LineHorizontal(1).LineColor(BorderColor);
                        footer.Item().PaddingTop(8).Row(row =>
                        {
                            row.RelativeItem().Text($"BlueprintOS · {document.Title} · v{document.ProjectVersion}").FontColor(MutedColor).FontSize(8);
                            row.RelativeItem().AlignRight().Text($"Gerado em {document.GeneratedAt:dd/MM/yyyy HH:mm} UTC").FontColor(MutedColor).FontSize(8);
                        });
                    });
                });
            });
        }).GeneratePdf();

        return Task.FromResult(bytes);
    }

    private static void ComposeCover(IContainer container, PublicationDocument document)
    {
        container
            .Background(AccentColor)
            .Padding(40)
            .MinHeight(220)
            .Column(column =>
            {
                column.Item().Text("BLUEPRINTOS").FontColor(Colors.White).FontSize(12).Bold();
                column.Item().PaddingTop(8).Text(document.Title).FontColor(Colors.White).FontSize(26).Bold();
                column.Item().PaddingTop(6).Text(document.Subtitle).FontColor(Colors.White).FontSize(12);
                column.Item().PaddingTop(16).Row(row =>
                {
                    row.RelativeItem().Text($"Versão {document.ProjectVersion}").FontColor(Colors.White).FontSize(9);
                    row.RelativeItem().AlignRight().Text($"Gerado em {document.GeneratedAt:dd/MM/yyyy HH:mm} UTC").FontColor(Colors.White).FontSize(9);
                });
            });
    }

    private static void ComposeToc(IContainer container, PublicationDocument document)
    {
        container.Border(1).BorderColor(BorderColor).Padding(16).Column(column =>
        {
            column.Item().Text("Sumário").FontColor(AccentColor).Bold().FontSize(13);
            foreach (var section in document.Sections)
            {
                column.Item().PaddingTop(4).Text($"• {section.Heading}").FontSize(10);
            }
        });
    }

    private static void ComposeSection(IContainer container, PublicationSection section)
    {
        container.Column(column =>
        {
            column.Item().Text(section.Heading).FontColor(AccentColor).Bold().FontSize(15);
            column.Item().PaddingBottom(6).LineHorizontal(1).LineColor(BorderColor);

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
                        ComposeTable(column, block);
                        break;
                    case ContentBlockKind.CodeBlock:
                        column.Item().PaddingTop(4).Background(Color.FromHex("#10151C")).Padding(8)
                            .Text(block.Text).FontColor(Colors.White).FontFamily("Courier").FontSize(8.5f);
                        break;
                }
            }
        });
    }

    private static void ComposeTable(ColumnDescriptor column, ContentBlock block)
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
                table.Cell().Background(Color.FromHex("#F6F8FA")).Border(1).BorderColor(BorderColor)
                    .Padding(4).Text(text => ComposeInline(text, headerCell, 9, bold: true));
            }

            foreach (var row in rows)
            {
                foreach (var cell in row)
                {
                    table.Cell().Border(1).BorderColor(BorderColor).Padding(4)
                        .Text(text => ComposeInline(text, cell, 9));
                }
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
}
