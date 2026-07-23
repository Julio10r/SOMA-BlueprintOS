using System.Text;
using BlueprintOS.Core.Publication.Contracts;
using BlueprintOS.Core.Publication.Models;
using BlueprintOS.Infrastructure.Publication.Publishers;

namespace BlueprintOS.UnitTests.Infrastructure.Publication.Publishers;

public class ReportPublishingHelperTests
{
    private sealed class FakeRenderer : IContentRenderer
    {
        public FakeRenderer(PublicationFormat format) => Format = format;

        public PublicationFormat Format { get; }

        public Task<byte[]> RenderAsync(PublicationDocument document, CancellationToken cancellationToken = default) =>
            Task.FromResult(Encoding.UTF8.GetBytes($"{Format}:{document.Slug}"));
    }

    private static PublicationDocument CreateDocument() => new(
        Slug: "ExecutiveReport",
        Title: "Título",
        Subtitle: "Subtítulo",
        Category: "executive",
        Sections: Array.Empty<PublicationSection>(),
        ProjectVersion: "1.0.0",
        GeneratedAt: DateTimeOffset.UtcNow);

    [Fact]
    public async Task WriteAllFormatsAsync_Should_Write_One_File_Per_Renderer_With_Correct_Extension()
    {
        var distRoot = Path.Combine(Path.GetTempPath(), $"publication-engine-tests-{Guid.NewGuid():N}");
        try
        {
            var renderers = new IContentRenderer[]
            {
                new FakeRenderer(PublicationFormat.Markdown),
                new FakeRenderer(PublicationFormat.Html),
                new FakeRenderer(PublicationFormat.Pdf),
            };

            var artifacts = await ReportPublishingHelper.WriteAllFormatsAsync(
                CreateDocument(), "executive", distRoot, renderers, CancellationToken.None);

            Assert.Equal(3, artifacts.Count);
            Assert.True(File.Exists(Path.Combine(distRoot, "executive", "ExecutiveReport.md")));
            Assert.True(File.Exists(Path.Combine(distRoot, "executive", "ExecutiveReport.html")));
            Assert.True(File.Exists(Path.Combine(distRoot, "executive", "ExecutiveReport.pdf")));
            Assert.Contains(artifacts, a => a.Format == PublicationFormat.Markdown && a.RelativePath == Path.Combine("executive", "ExecutiveReport.md"));
        }
        finally
        {
            if (Directory.Exists(distRoot))
            {
                Directory.Delete(distRoot, recursive: true);
            }
        }
    }
}
