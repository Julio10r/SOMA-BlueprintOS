using BlueprintOS.Core.AI.Contracts;
using BlueprintOS.Core.AI.Models;
using BlueprintOS.Core.Agents;
using BlueprintOS.Core.Agents.Models;
using BlueprintOS.Core.Knowledge.Contracts;
using BlueprintOS.Core.Knowledge.Models;

namespace BlueprintOS.UnitTests.Core.Agents;

public class KnowledgeAgentTests
{
    [Fact]
    public async Task ExecuteAsync_Should_Augment_Prompt_With_Knowledge_Snippets()
    {
        var document = new KnowledgeDocument("1", "Onboarding", "Conteúdo completo do onboarding.", "1.md");
        var knowledgeService = new FakeKnowledgeService(new[]
        {
            new KnowledgeSearchResult(document, "trecho relevante sobre onboarding", 1),
        });
        var runtime = new CapturingAIRuntime();

        var agent = new KnowledgeAgent(runtime, knowledgeService);
        var result = await agent.ExecuteAsync(new AgentContext { Input = "Como funciona o onboarding?" });

        Assert.Contains("trecho relevante sobre onboarding", runtime.LastPrompt);
        Assert.Contains("Como funciona o onboarding?", runtime.LastPrompt);
        Assert.Equal("resposta", result.Output);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Use_Original_Input_When_No_Knowledge_Found()
    {
        var knowledgeService = new FakeKnowledgeService(Array.Empty<KnowledgeSearchResult>());
        var runtime = new CapturingAIRuntime();

        var agent = new KnowledgeAgent(runtime, knowledgeService);
        await agent.ExecuteAsync(new AgentContext { Input = "Pergunta sem contexto" });

        Assert.Equal("Pergunta sem contexto", runtime.LastPrompt);
    }

    private sealed class FakeKnowledgeService : IKnowledgeService
    {
        private readonly IReadOnlyList<KnowledgeSearchResult> _results;

        public FakeKnowledgeService(IReadOnlyList<KnowledgeSearchResult> results)
        {
            _results = results;
        }

        public Task<IReadOnlyList<KnowledgeSearchResult>> SearchAsync(
            string query,
            int maxResults = 5,
            CancellationToken cancellationToken = default)
            => Task.FromResult(_results);
    }

    private sealed class CapturingAIRuntime : IAIRuntime
    {
        public string LastPrompt { get; private set; } = string.Empty;

        public Task<AIResponse> ExecuteAsync(AIRequest request, CancellationToken cancellationToken = default)
        {
            LastPrompt = request.Messages[0].Content;

            var message = new ChatMessage(ChatRole.Assistant, "resposta");
            var usage = new TokenUsage(PromptTokens: 1, CompletionTokens: 1);
            var metrics = new AIExecutionMetrics("fake-provider", request.Model.Id, TimeSpan.Zero, usage);

            return Task.FromResult(new AIResponse(message, usage, metrics, FinishReason: "stop"));
        }
    }
}
