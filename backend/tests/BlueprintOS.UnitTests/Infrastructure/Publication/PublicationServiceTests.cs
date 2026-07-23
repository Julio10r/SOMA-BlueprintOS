using BlueprintOS.Core.Publication.Contracts;
using BlueprintOS.Core.Publication.Models;
using BlueprintOS.Infrastructure.Publication;

namespace BlueprintOS.UnitTests.Infrastructure.Publication;

public class PublicationServiceTests
{
    private sealed class FakeReportPublisher : IReportPublisher
    {
        private readonly IReadOnlyList<PublishedArtifact> _artifacts;

        public FakeReportPublisher(string category, IReadOnlyList<PublishedArtifact> artifacts)
        {
            Category = category;
            _artifacts = artifacts;
        }

        public string Category { get; }

        public int CallCount { get; private set; }

        public Task<IReadOnlyList<PublishedArtifact>> PublishAsync(CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(_artifacts);
        }
    }

    [Fact]
    public async Task PublishAllAsync_Should_Aggregate_Artifacts_From_All_Publishers()
    {
        var executive = new FakeReportPublisher("executive", new[]
        {
            new PublishedArtifact(PublicationFormat.Markdown, "executive/ExecutiveReport.md", "/dist/executive/ExecutiveReport.md"),
        });
        var client = new FakeReportPublisher("client", new[]
        {
            new PublishedArtifact(PublicationFormat.Html, "client/ClientGuide.html", "/dist/client/ClientGuide.html"),
        });

        var service = new PublicationService(new IReportPublisher[] { executive, client });

        var artifacts = await service.PublishAllAsync();

        Assert.Equal(2, artifacts.Count);
        Assert.Equal(1, executive.CallCount);
        Assert.Equal(1, client.CallCount);
    }

    [Fact]
    public async Task PublishAllAsync_Should_Return_Empty_When_No_Publishers_Registered()
    {
        var service = new PublicationService(Array.Empty<IReportPublisher>());

        var artifacts = await service.PublishAllAsync();

        Assert.Empty(artifacts);
    }
}
