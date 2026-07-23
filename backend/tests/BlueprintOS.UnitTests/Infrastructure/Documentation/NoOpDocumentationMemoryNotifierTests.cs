using BlueprintOS.Infrastructure.Documentation;
using Microsoft.Extensions.Logging.Abstractions;

namespace BlueprintOS.UnitTests.Infrastructure.Documentation;

public class NoOpDocumentationMemoryNotifierTests
{
    [Fact]
    public async Task NotifyAsync_Should_Complete_Without_Throwing()
    {
        var notifier = new NoOpDocumentationMemoryNotifier(NullLogger<NoOpDocumentationMemoryNotifier>.Instance);

        var exception = await Record.ExceptionAsync(() => notifier.NotifyAsync("doc-1", "documento criado"));

        Assert.Null(exception);
    }
}
