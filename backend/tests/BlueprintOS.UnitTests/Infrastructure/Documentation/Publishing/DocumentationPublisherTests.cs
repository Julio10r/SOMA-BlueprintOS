using BlueprintOS.Core.Documentation.Contracts;
using BlueprintOS.Core.Documentation.Models;
using BlueprintOS.Infrastructure.Documentation.Publishing;

namespace BlueprintOS.UnitTests.Infrastructure.Documentation.Publishing;

public class DocumentationPublisherTests
{
    private sealed class FakeDocumentPublisher : IDocumentPublisher
    {
        public List<(string RelativePath, string Title, string Body)> Calls { get; } = new();

        public Task<PublishedDocument> PublishAsync(
            string relativePath,
            string title,
            string body,
            CancellationToken cancellationToken = default)
        {
            Calls.Add((relativePath, title, body));
            return Task.FromResult(new PublishedDocument(relativePath, $"/fake/{relativePath}", title, DateTimeOffset.UtcNow));
        }
    }

    [Fact]
    public async Task PublishManyAsync_Should_Publish_Each_Request_In_Order()
    {
        var fake = new FakeDocumentPublisher();
        var publisher = new DocumentationPublisher(fake);
        var requests = new List<DocumentationPublishRequest>
        {
            new("a.md", "A", "corpo a"),
            new("b.md", "B", "corpo b"),
        };

        var results = await publisher.PublishManyAsync(requests);

        Assert.Equal(2, fake.Calls.Count);
        Assert.Equal("a.md", fake.Calls[0].RelativePath);
        Assert.Equal("b.md", fake.Calls[1].RelativePath);
        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task PublishManyAsync_Should_Return_Empty_When_No_Requests()
    {
        var publisher = new DocumentationPublisher(new FakeDocumentPublisher());

        var results = await publisher.PublishManyAsync(Array.Empty<DocumentationPublishRequest>());

        Assert.Empty(results);
    }
}
