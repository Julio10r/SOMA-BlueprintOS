using BlueprintOS.Core.Publication.Models;
using BlueprintOS.Infrastructure.Publication.Content;

namespace BlueprintOS.UnitTests.Infrastructure.Publication.Content;

public class MarkdownContentParserTests
{
    [Fact]
    public void Parse_Should_Recognize_Heading()
    {
        var blocks = MarkdownContentParser.Parse("## Título da Seção");

        var block = Assert.Single(blocks);
        Assert.Equal(ContentBlockKind.Heading, block.Kind);
        Assert.Equal("Título da Seção", block.Text);
        Assert.Equal(2, block.Level);
    }

    [Fact]
    public void Parse_Should_Group_Consecutive_Bullet_Lines_Into_A_Single_BulletList_Block()
    {
        var blocks = MarkdownContentParser.Parse("- Item um\n- Item dois");

        var block = Assert.Single(blocks);
        Assert.Equal(ContentBlockKind.BulletList, block.Kind);
        Assert.Equal(new[] { "Item um", "Item dois" }, block.Items);
    }

    [Fact]
    public void Parse_Should_Group_Table_Header_And_Rows_Into_A_Single_Table_Block()
    {
        var markdown = "| Indicador | Valor |\n|---|---|\n| Warnings | 0 |\n| Erros | 0 |";

        var blocks = MarkdownContentParser.Parse(markdown);

        var block = Assert.Single(blocks);
        Assert.Equal(ContentBlockKind.Table, block.Kind);
        Assert.Equal(new[] { "Indicador", "Valor" }, block.TableHeader);
        Assert.Equal(2, block.TableRows!.Count);
        Assert.Equal(new[] { "Warnings", "0" }, block.TableRows[0]);
        Assert.Equal(new[] { "Erros", "0" }, block.TableRows[1]);
    }

    [Fact]
    public void Parse_Should_Merge_Consecutive_Lines_Into_A_Single_Paragraph()
    {
        var blocks = MarkdownContentParser.Parse("Primeira linha.\nSegunda linha.\n\nNovo parágrafo.");

        Assert.Equal(2, blocks.Count);
        Assert.Equal("Primeira linha. Segunda linha.", blocks[0].Text);
        Assert.Equal("Novo parágrafo.", blocks[1].Text);
    }

    [Fact]
    public void Parse_Should_Recognize_Code_Blocks()
    {
        var blocks = MarkdownContentParser.Parse("```\nvar x = 1;\n```");

        var block = Assert.Single(blocks);
        Assert.Equal(ContentBlockKind.CodeBlock, block.Kind);
        Assert.Equal("var x = 1;", block.Text);
    }

    [Fact]
    public void Parse_Should_Preserve_Inline_Emphasis_Markers_For_Renderers_To_Interpret()
    {
        var blocks = MarkdownContentParser.Parse("Texto com **negrito** e `código`.");

        var block = Assert.Single(blocks);
        Assert.Equal("Texto com **negrito** e `código`.", block.Text);
    }
}
