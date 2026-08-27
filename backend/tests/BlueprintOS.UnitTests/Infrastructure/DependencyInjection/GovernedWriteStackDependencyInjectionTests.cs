using BlueprintOS.Application.Governance;
using BlueprintOS.Core.AI.Governance;
using BlueprintOS.Core.AI.Governance.Contracts;
using BlueprintOS.Infrastructure.DependencyInjection;
using BlueprintOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BlueprintOS.UnitTests.Infrastructure.DependencyInjection;

/// <summary>
/// Proves that GovernedWriteServiceCollectionExtensions.AddGovernedWriteStack()
/// — the composition-root call added at
/// BlueprintOS.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs:157
/// — actually resolves the full governed-write object graph through the real
/// DI container, not just via hand-constructed test fixtures.
/// </summary>
public sealed class GovernedWriteStackDependencyInjectionTests
{
    [Fact]
    public void AddGovernedWriteStack_Resolves_Full_Governance_Graph()
    {
        var services = new ServiceCollection();
        services.AddDbContext<BlueprintOSDbContext>(options =>
            options.UseInMemoryDatabase($"governed-write-di-{Guid.NewGuid():N}"));
        services.AddSingleton<IAIGovernancePolicyEngine, AIGovernancePolicyEngine>();
        services.AddSingleton<IApprovalPolicy, ApprovalPolicy>();
        services.AddSingleton(TimeProvider.System);
        services.AddGovernedWriteStack();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var scoped = scope.ServiceProvider;

        Assert.NotNull(scoped.GetRequiredService<GovernedWriteStack>());
        Assert.NotNull(scoped.GetRequiredService<IToolGateway>());
        Assert.NotNull(scoped.GetRequiredService<GovernedPlanBridge>());
        Assert.NotNull(scoped.GetRequiredService<IActionProposalAdapter>());
        Assert.NotNull(scoped.GetRequiredService<IApprovalStore>());
        Assert.NotNull(scoped.GetRequiredService<IGovernanceAuditStore>());

        var adapters = scoped.GetServices<IGovernedToolAdapter>().ToList();
        Assert.Equal(4, adapters.Count);
        Assert.Contains(adapters, a => a.Capability == SomaLinxReadOnlyAdapter.Capability);
        Assert.Contains(adapters, a => a.Capability == WiseGovernedAdapter.Capability);
        Assert.Contains(adapters, a => a.Capability == StructuredActionProposalAdapter.Capability);
        Assert.Contains(adapters, a => a.Capability == LinxKnowledgeStoreReadOnlyAdapter.Capability);
    }
}
