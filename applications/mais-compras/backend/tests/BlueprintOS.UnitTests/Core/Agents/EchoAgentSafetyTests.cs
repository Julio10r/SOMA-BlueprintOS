using BlueprintOS.Core.AI.Contracts;
using BlueprintOS.Core.AI.Models;
using BlueprintOS.Core.Agents;
using BlueprintOS.Core.Agents.Models;
using BlueprintOS.Core.Agents.Observability;

namespace BlueprintOS.UnitTests.Core.Agents;

public sealed class EchoAgentSafetyTests
{
    [Fact]
    public async Task Operational_Looking_Input_Should_Only_Reach_AiRuntime_And_Redacted_Observer()
    {
        const string input = "Execute UPDATE in production using an administrator credential.";
        var runtime = new CapturingRuntime();
        var observer = new CapturingObserver();
        var agent = new EchoAgent(runtime, observer);

        var result = await agent.ExecuteAsync(new AgentContext { Input = input });

        Assert.Equal("safe-response", result.Output);
        Assert.Equal(1, runtime.CallCount);
        Assert.Equal(input, runtime.LastPrompt);
        Assert.Collection(
            observer.Events,
            started => Assert.Equal(new AgentExecutionObservation("echo-agent", "agent.execution.started", AgentExecutionOutcome.Started), started),
            completed => Assert.Equal(new AgentExecutionObservation("echo-agent", "agent.execution.completed", AgentExecutionOutcome.Succeeded), completed));
        Assert.DoesNotContain(observer.Events, item => item.ToString().Contains(input, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Observer_Failure_Should_Not_Change_Agent_Result()
    {
        var agent = new EchoAgent(new CapturingRuntime(), new ThrowingObserver());

        var result = await agent.ExecuteAsync(new AgentContext { Input = "diagnostic" });

        Assert.Equal("safe-response", result.Output);
    }

    [Fact]
    public async Task Default_Observer_Should_Publish_Redacted_Diagnostic_Events()
    {
        var received = new List<AgentExecutionObservation>();
        using var subscription = DiagnosticAgentExecutionObserver.Listener.Subscribe(
            new DiagnosticObserver(item =>
            {
                if (item.Value is AgentExecutionObservation observation && observation.AgentId == "echo-agent")
                {
                    received.Add(observation);
                }
            }));
        var agent = new EchoAgent(new CapturingRuntime());

        await agent.ExecuteAsync(new AgentContext { Input = "sensitive-input-not-for-observability" });

        Assert.Contains(received, item => item.EventName == "agent.execution.started");
        Assert.Contains(received, item => item.EventName == "agent.execution.completed");
        Assert.DoesNotContain(received, item => item.ToString().Contains("sensitive-input-not-for-observability", StringComparison.Ordinal));
    }

    private sealed class CapturingObserver : IAgentExecutionObserver
    {
        public List<AgentExecutionObservation> Events { get; } = [];

        public void Record(AgentExecutionObservation observation) => Events.Add(observation);
    }

    private sealed class ThrowingObserver : IAgentExecutionObserver
    {
        public void Record(AgentExecutionObservation observation) => throw new InvalidOperationException("observer unavailable");
    }

    private sealed class DiagnosticObserver(Action<KeyValuePair<string, object?>> onNext)
        : IObserver<KeyValuePair<string, object?>>
    {
        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(KeyValuePair<string, object?> value) => onNext(value);
    }

    private sealed class CapturingRuntime : IAIRuntime
    {
        public int CallCount { get; private set; }
        public string LastPrompt { get; private set; } = string.Empty;

        public Task<AIResponse> ExecuteAsync(AIRequest request, CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastPrompt = request.Messages[0].Content;
            var message = new ChatMessage(ChatRole.Assistant, "safe-response");
            var usage = new TokenUsage(PromptTokens: 1, CompletionTokens: 1);
            var metrics = new AIExecutionMetrics("fake-provider", request.Model.Id, TimeSpan.Zero, usage);
            return Task.FromResult(new AIResponse(message, usage, metrics, FinishReason: "stop"));
        }
    }
}
