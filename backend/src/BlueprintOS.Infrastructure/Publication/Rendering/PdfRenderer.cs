using BlueprintOS.Core.Publication.Contracts;
using BlueprintOS.Core.Publication.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BlueprintOS.Infrastructure.Publication.Rendering;

/// <summary>
/// Implementação de <see cref="IContentRenderer"/> que produz um PDF pronto para impressão a
/// partir do mesmo <see cref="PublicationDocument"/> usado pelos demais formatos, preservando
/// capa, cabeçalho, rodapé, índice, seções, listas e tabelas. Usa QuestPDF (biblioteca .NET
/// pura, sem dependência de navegador/Chromium) em vez de converter o HTML diretamente.
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

            var blocks = MarkdownBlockParser.Parse(section.MarkdownBody);
            IReadOnlyList<string>? headerCells = null;
            var tableRows = new List<IReadOnlyList<string>>();

            void FlushTable()
            {
                if (headerCells is null || tableRows.Count == 0)
                {
                    headerCells = null;
                    tableRows.Clear();
                    return;
                }

                var columnCount = headerCells.Count;
                column.Item().PaddingVertical(6).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        for (var i = 0; i < columnCount; i++)
                        {
                            columns.RelativeColumn();
                        }
                    });

                    foreach (var header in headerCells)
                    {
                        table.Cell().Background(Color.FromHex("#F6F8FA")).Border(1).BorderColor(BorderColor)
                            .Padding(4).Text(header).Bold().FontSize(9);
                    }

                    foreach (var row in tableRows)
                    {
                        foreach (var cell in row)
                        {
                            table.Cell().Border(1).BorderColor(BorderColor).Padding(4).Text(cell).FontSize(9);
                        }
                    }
                });

                headerCells = null;
                tableRows.Clear();
            }

            foreach (var block in blocks)
            {
                if (block.Kind != MarkdownBlockKind.TableRow)
                {
                    FlushTable();
                }

                switch (block.Kind)
                {
                    case MarkdownBlockKind.Heading:
                        column.Item().PaddingTop(8).Text(block.Text).Bold().FontSize(block.Level <= 2 ? 12 : 10.5f);
                        break;
                    case MarkdownBlockKind.Paragraph:
                        column.Item().PaddingTop(4).Text(StripEmphasis(block.Text)).FontSize(10);
                        break;
                    case MarkdownBlockKind.BulletItem:
                        column.Item().PaddingTop(2).Row(row =>
                        {
                            row.ConstantItem(12).Text("•");
                            row.RelativeItem().Text(StripEmphasis(block.Text)).FontSize(10);
                        });
                        break;
                    case MarkdownBlockKind.CodeBlock:
                        column.Item().PaddingTop(4).Background(Color.FromHex("#10151C")).Padding(8)
                            .Text(block.Text).FontColor(Colors.White).FontFamily("Courier").FontSize(8.5f);
                        break;
                    case MarkdownBlockKind.TableRow:
                        if (block.IsTableHeader)
                        {
                            headerCells = block.Cells;
                        }
                        else
                        {
                            tableRows.Add(block.Cells ?? Array.Empty<string>());
                        }

                        break;
                }
            }

            FlushTable();
        });
    }

    private static string StripEmphasis(string text) => text.Replace("**", string.Empty).Replace("__", string.Empty);
}
