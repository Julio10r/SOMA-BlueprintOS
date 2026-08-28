using BlueprintOS.Core.Publication.Models;
using BlueprintOS.Infrastructure.Publication.Content;

namespace BlueprintOS.UnitTests.Infrastructure.Publication.Content;

public class InlineSpanParserTests
{
    [Fact]
    public void Parse_Should_Return_Single_Plain_Span_When_No_Emphasis()
    {
        var spans = InlineSpanParser.Parse("texto simples");

        var span = Assert.Single(spans);
        Assert.Equal(InlineSpanKind.Plain, span.Kind);
        Assert.Equal("texto simples", span.Text);
    }

    [Fact]
    public void Parse_Should_Recognize_Bold_And_Code_Spans_Interleaved_With_Plain_Text()
    {
        var spans = InlineSpanParser.Parse("Build **succeeded** com `0 warnings`.");

        Assert.Equal(5, spans.Count);
        Assert.Equal(new InlineSpan(InlineSpanKind.Plain, "Build "), spans[0]);
        Assert.Equal(new InlineSpan(InlineSpanKind.Bold, "succeeded"), spans[1]);
        Assert.Equal(new InlineSpan(InlineSpanKind.Plain, " com "), spans[2]);
        Assert.Equal(new InlineSpan(InlineSpanKind.Code, "0 warnings"), spans[3]);
        Assert.Equal(new InlineSpan(InlineSpanKind.Plain, "."), spans[4]);
    }
}
