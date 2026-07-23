using BlueprintOS.Core.Agents.Contracts;
using BlueprintOS.Core.Agents.Models;
using BlueprintOS.Core.Workflows;
using BlueprintOS.Core.Workflows.Models;

namespace BlueprintOS.UnitTests.Core.Workflows;

public class WorkflowRunnerTests
{
    [Fact]
    public async Task RunAsync_Should_Execute_Agents_Sequentially()
    {
        var callOrder = new List<string>();
        var workflow = new Workflow(new IAgent[]
        {
            new RecordingAgent("agent-1", callOrder),
            new RecordingAgent("agent-2", callOrder),
            new RecordingAgent("agent-3", callOrder),
        });

        var runner = new WorkflowRunner();
        await runner.RunAsync(workflow, new WorkflowContext { Input = "start" });

        Assert.Equal(new[] { "agent-1", "agent-2", "agent-3" }, callOrder);
    }

    [Fact]
    public async Task RunAsync_Should_Pass_Previous_Output_As_Next_Input()
    {
        var workflow = new Workflow(new IAgent[]
        {
            new AppendingAgent("-a"),
            new AppendingAgent("-b"),
            new AppendingAgent("-c"),
        });

        var runner = new WorkflowRunner();
        var result = await runner.RunAsync(workflow, new WorkflowContext { Input = "start" });

        Assert.Equal("start-a-b-c", result.Output);
        Assert.Equal(new[] { "start-a", "start-a-b", "start-a-b-c" }, result.StepResults.Select(r => r.Output));
    }

    [Fact]
    public async Task RunAsync_Should_Return_Results_In_Execution_Order()
    {
        var workflow = new Workflow(new IAgent[]
        {
            new AppendingAgent("-first"),
            new AppendingAgent("-second"),
        });

        var runner = new WorkflowRunner();
        var result = await runner.RunAsync(workflow, new WorkflowContext { Input = "x" });

        Assert.Equal("x-first", result.StepResults[0].Output);
        Assert.Equal("x-first-second", result.StepResults[1].Output);
    }

    [Fact]
    public async Task RunAsync_With_Empty_Workflow_Should_Return_Original_Input()
    {
        var workflow = new Workflow(Array.Empty<IAgent>());

        var runner = new WorkflowRunner();
        var result = await runner.RunAsync(workflow, new WorkflowContext { Input = "unchanged" });

        Assert.Equal("unchanged", result.Output);
        Assert.Empty(result.StepResults);
    }

    private sealed class RecordingAgent : IAgent
    {
        private readonly string _name;
        private readonly List<string> _callOrder;

        public RecordingAgent(string name, List<string> callOrder)
        {
            _name = name;
            _callOrder = callOrder;
        }

        public Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken cancellationToken = default)
        {
            _callOrder.Add(_name);
            return Task.FromResult(new AgentResult(context.Input));
        }
    }

    private sealed class AppendingAgent : IAgent
    {
        private readonly string _suffix;

        public AppendingAgent(string suffix)
        {
            _suffix = suffix;
        }

        public Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken cancellationToken = default)
            => Task.FromResult(new AgentResult(context.Input + _suffix));
    }
}
