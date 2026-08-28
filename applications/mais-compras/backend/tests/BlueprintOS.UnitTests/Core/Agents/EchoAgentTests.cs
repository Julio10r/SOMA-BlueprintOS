using BlueprintOS.Core.AI.Contracts;
using BlueprintOS.Core.AI.Models;
using BlueprintOS.Core.Agents;
using BlueprintOS.Core.Agents.Models;

namespace BlueprintOS.UnitTests.Core.Agents;

public class EchoAgentTests
{
    [Fact]
    public async Task Create_And_ExecuteAsync_Should_Return_Runtime_Output()
    {
        var factory = new AgentFactory(new FakeAIRuntime());

        var agent = factory.Create<EchoAgent>();
        var result = await agent.ExecuteAsync(new AgentContext { Input = "Diga Olá" });

        Assert.Equal("Olá!", result.Output);
    }

    private sealed class FakeAIRuntime : IAIRuntime
    {
        public Task<AIResponse> ExecuteAsync(AIRequest request, CancellationToken cancellationToken = default)
        {
            var message = new ChatMessage(ChatRole.Assistant, "Olá!");
            var usage = new TokenUsage(PromptTokens: 1, CompletionTokens: 1);
            var metrics = new AIExecutionMetrics("fake-provider", request.Model.Id, TimeSpan.Zero, usage);

            return Task.FromResult(new AIResponse(message, usage, metrics, FinishReason: "stop"));
        }
    }
}
