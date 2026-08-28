using BlueprintOS.Core.Documentation.Contracts;
using BlueprintOS.Core.Documentation.Models;
using BlueprintOS.Infrastructure.Documentation;

namespace BlueprintOS.UnitTests.Infrastructure.Documentation;

public class StaleDocumentationDetectorTests
{
    private sealed class FakeSyncService : IDocumentationSyncService
    {
        private readonly IReadOnlyList<StaleDocumentationInfo> _results;

        public FakeSyncService(IReadOnlyList<StaleDocumentationInfo> results)
        {
            _results = results;
        }

        public Task<StaleDocumentationInfo> CheckAsync(DocumentationSyncCheck check, CancellationToken cancellationToken = default)
            => Task.FromResult(_results.First(r => r.DocPath == check.DocPath));

        public Task<IReadOnlyList<StaleDocumentationInfo>> CheckAllAsync(
            IReadOnlyList<DocumentationSyncCheck> checks,
            CancellationToken cancellationToken = default)
            => Task.FromResult(_results);
    }

    [Fact]
    public async Task DetectStaleAsync_Should_Return_Only_Stale_Results()
    {
        var results = new[]
        {
            new StaleDocumentationInfo("doc1.md", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), true),
            new StaleDocumentationInfo("doc2.md", DateTime.UtcNow, DateTime.UtcNow.AddDays(-1), false),
        };
        var detector = new StaleDocumentationDetector(new FakeSyncService(results));

        var stale = await detector.DetectStaleAsync(new[]
        {
            new DocumentationSyncCheck("doc1.md", Array.Empty<string>()),
            new DocumentationSyncCheck("doc2.md", Array.Empty<string>()),
        });

        Assert.Single(stale);
        Assert.Equal("doc1.md", stale[0].DocPath);
    }

    [Fact]
    public async Task DetectStaleAsync_Should_Return_Empty_When_Nothing_Is_Stale()
    {
        var results = new[]
        {
            new StaleDocumentationInfo("doc1.md", DateTime.UtcNow, DateTime.UtcNow.AddDays(-1), false),
        };
        var detector = new StaleDocumentationDetector(new FakeSyncService(results));

        var stale = await detector.DetectStaleAsync(new[] { new DocumentationSyncCheck("doc1.md", Array.Empty<string>()) });

        Assert.Empty(stale);
    }
}
