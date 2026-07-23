using BlueprintOS.Infrastructure.Publication.Rendering;

namespace BlueprintOS.UnitTests.Infrastructure.Publication.Rendering;

public class MarkdownBlockParserTests
{
    [Fact]
    public void Parse_Should_Recognize_Heading()
    {
        var blocks = MarkdownBlockParser.Parse("## Título da Seção");

        var block = Assert.Single(blocks);
        Assert.Equal(MarkdownBlockKind.Heading, block.Kind);
        Assert.Equal("Título da Seção", block.Text);
        Assert.Equal(2, block.Level);
    }

    [Fact]
    public void Parse_Should_Recognize_Bullet_Items()
    {
        var blocks = MarkdownBlockParser.Parse("- Item um\n- Item dois");

        Assert.Equal(2, blocks.Count);
        Assert.All(blocks, b => Assert.Equal(MarkdownBlockKind.BulletItem, b.Kind));
        Assert.Equal("Item um", blocks[0].Text);
        Assert.Equal("Item dois", blocks[1].Text);
    }

    [Fact]
    public void Parse_Should_Recognize_Table_With_Header_And_Rows()
    {
        var markdown = "| Indicador | Valor |\n|---|---|\n| Warnings | 0 |\n| Erros | 0 |";

        var blocks = MarkdownBlockParser.Parse(markdown);

        Assert.Equal(3, blocks.Count);
        Assert.True(blocks[0].IsTableHeader);
        Assert.Equal(new[] { "Indicador", "Valor" }, blocks[0].Cells);
        Assert.False(blocks[1].IsTableHeader);
        Assert.Equal(new[] { "Warnings", "0" }, blocks[1].Cells);
        Assert.Equal(new[] { "Erros", "0" }, blocks[2].Cells);
    }

    [Fact]
    public void Parse_Should_Merge_Consecutive_Lines_Into_A_Single_Paragraph()
    {
        var blocks = MarkdownBlockParser.Parse("Primeira linha.\nSegunda linha.\n\nNovo parágrafo.");

        Assert.Equal(2, blocks.Count);
        Assert.Equal("Primeira linha. Segunda linha.", blocks[0].Text);
        Assert.Equal("Novo parágrafo.", blocks[1].Text);
    }

    [Fact]
    public void Parse_Should_Recognize_Code_Blocks()
    {
        var blocks = MarkdownBlockParser.Parse("```\nvar x = 1;\n```");

        var block = Assert.Single(blocks);
        Assert.Equal(MarkdownBlockKind.CodeBlock, block.Kind);
        Assert.Equal("var x = 1;", block.Text);
    }
}
