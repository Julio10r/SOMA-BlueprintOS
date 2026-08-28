using BlueprintOS.Core.Knowledge.Contracts;
using BlueprintOS.Core.Knowledge.Models;
using BlueprintOS.Infrastructure.Services;

namespace BlueprintOS.UnitTests.Infrastructure.Services;

public class KnowledgeServiceTests
{
    [Fact]
    public async Task SearchAsync_Should_Return_Matching_Documents_Ordered_By_Score()
    {
        var provider = new FakeKnowledgeProvider(new[]
        {
            new KnowledgeDocument("1", "Onboarding", "O processo de onboarding envolve várias etapas. Onboarding é importante.", "1.md"),
            new KnowledgeDocument("2", "Deploy", "Como fazer o deploy da aplicação em produção.", "2.md"),
            new KnowledgeDocument("3", "Outro", "Conteúdo sem relação alguma.", "3.md"),
        });

        var service = new KnowledgeService(provider);

        var results = await service.SearchAsync("onboarding");

        Assert.Single(results);
        Assert.Equal("Onboarding", results[0].Document.Title);
        Assert.Equal(2, results[0].Score);
    }

    [Fact]
    public async Task SearchAsync_Should_Return_Relevant_Snippet_Containing_Query()
    {
        var provider = new FakeKnowledgeProvider(new[]
        {
            new KnowledgeDocument("1", "Deploy", "Como fazer o deploy da aplicação em produção com segurança.", "1.md"),
        });

        var service = new KnowledgeService(provider);

        var results = await service.SearchAsync("deploy");

        Assert.Single(results);
        Assert.Contains("deploy", results[0].Snippet, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SearchAsync_Should_Return_Empty_When_Query_Is_Empty()
    {
        var provider = new FakeKnowledgeProvider(Array.Empty<KnowledgeDocument>());
        var service = new KnowledgeService(provider);

        var results = await service.SearchAsync(string.Empty);

        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchAsync_Should_Respect_MaxResults()
    {
        var provider = new FakeKnowledgeProvider(new[]
        {
            new KnowledgeDocument("1", "Doc1", "termo termo termo", "1.md"),
            new KnowledgeDocument("2", "Doc2", "termo termo", "2.md"),
            new KnowledgeDocument("3", "Doc3", "termo", "3.md"),
        });

        var service = new KnowledgeService(provider);

        var results = await service.SearchAsync("termo", maxResults: 2);

        Assert.Equal(2, results.Count);
        Assert.Equal("Doc1", results[0].Document.Title);
        Assert.Equal("Doc2", results[1].Document.Title);
    }

    private sealed class FakeKnowledgeProvider : IKnowledgeProvider
    {
        private readonly IReadOnlyList<KnowledgeDocument> _documents;

        public FakeKnowledgeProvider(IReadOnlyList<KnowledgeDocument> documents)
        {
            _documents = documents;
        }

        public Task<IReadOnlyList<KnowledgeDocument>> LoadDocumentsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(_documents);
    }
}
