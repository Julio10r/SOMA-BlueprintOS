using BlueprintOS.Core.AI.Contracts;
using BlueprintOS.Core.AI.Models;

namespace BlueprintOS.UnitTests.Core.AI;

public class AIRuntimeTests
{
    [Fact]
    public async Task ExecuteAsync_Should_Delegate_To_Registered_Provider()
    {
        var provider = new FakeAIProvider();
        IAIRuntime runtime = new FakeAIRuntime(provider);

        var request = new AIRequest(
            Model: new AIModel("fake-model", "fake-provider"),
            Messages: new[] { new ChatMessage(ChatRole.User, "Qual a capital do Brasil?") });

        var response = await runtime.ExecuteAsync(request);

        Assert.Equal(ChatRole.Assistant, response.Message.Role);
        Assert.Equal("Brasília", response.Message.Content);
        Assert.Equal(1, response.Usage.TotalTokens - response.Usage.CompletionTokens);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Respect_Cancellation()
    {
        IAIRuntime runtime = new FakeAIRuntime(new FakeAIProvider());
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var request = new AIRequest(
            Model: new AIModel("fake-model", "fake-provider"),
            Messages: new[] { new ChatMessage(ChatRole.User, "teste") });

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => runtime.ExecuteAsync(request, cts.Token));
    }

    private sealed class FakeAIProvider : IAIProvider
    {
        public string Name => "fake-provider";

        public Task<AIResponse> CompleteAsync(AIRequest request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var message = new ChatMessage(ChatRole.Assistant, "Brasília");
            var usage = new TokenUsage(PromptTokens: 1, CompletionTokens: 2);
            var metrics = new AIExecutionMetrics(Name, request.Model.Id, TimeSpan.Zero, usage);

            return Task.FromResult(new AIResponse(message, usage, metrics, FinishReason: "stop"));
        }
    }

    private sealed class FakeAIRuntime(IAIProvider provider) : IAIRuntime
    {
        public Task<AIResponse> ExecuteAsync(AIRequest request, CancellationToken cancellationToken = default) =>
            provider.CompleteAsync(request, cancellationToken);
    }
}
