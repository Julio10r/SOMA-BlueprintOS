using BlueprintOS.Application.Governance;
using BlueprintOS.Core.AI.Governance;
using BlueprintOS.Core.AI.Governance.Contracts;
using BlueprintOS.Infrastructure.Persistence.Governance;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BlueprintOS.Infrastructure.DependencyInjection;

public static class GovernedWriteServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Governed Write Stack. Governance bookkeeping (approvals, audit, recovery index,
    /// rollback audit, write-execution audit, knowledge gaps, write-verification profiles) is persisted by
    /// the File* stores under <c>runtime/governance/</c> — a sibling of the existing <c>runtime/backups/</c>
    /// Recovery Package root — so the Agents' governance runtime has ZERO dependency on
    /// <c>BlueprintOSDbContext</c>/<c>MaisComprasConnection</c>, the +Compras business database.
    ///
    /// The root directory is resolved from configuration key <c>Governance:RuntimeRoot</c> when present
    /// (this is how tests point the stack at a unique temp directory per run); otherwise it defaults to
    /// <c>{CurrentDirectory}/runtime/governance</c>, mirroring the same "relative to the running process's
    /// working directory" convention already used for Recovery Packages.
    /// </summary>
    public static IServiceCollection AddGovernedWriteStack(this IServiceCollection services, IConfiguration? configuration = null)
    {
        var configuredRoot = configuration?["Governance:RuntimeRoot"];
        var governanceRoot = string.IsNullOrWhiteSpace(configuredRoot)
            ? Path.Combine(Directory.GetCurrentDirectory(), "runtime", "governance")
            : configuredRoot;

        services.AddScoped<IActionProposalAdapter, StructuredActionProposalAdapter>();
        services.AddSingleton<IApprovalStore>(_ => new FileApprovalStore(governanceRoot));
        services.AddSingleton<IGovernanceAuditStore>(_ => new FileGovernanceAuditStore(governanceRoot));
        services.AddSingleton<IRecoveryIndexStore>(_ => new FileRecoveryIndexStore(governanceRoot));
        services.AddSingleton<IRollbackAuditStore>(_ => new FileRollbackAuditStore(governanceRoot));
        services.AddSingleton<IWriteExecutionAuditStore>(_ => new FileWriteExecutionAuditStore(governanceRoot));
        services.AddSingleton<IWriteValidationKnowledgeGapStore>(_ => new FileWriteValidationKnowledgeGapStore(governanceRoot));
        services.AddSingleton<IWriteVerificationProfileStore>(_ => new FileWriteVerificationProfileStore(governanceRoot));
        services.AddSingleton<IRollbackCapabilityGapStore>(_ => new FileRollbackCapabilityGapStore(governanceRoot));
        services.AddScoped<IGovernedToolAdapter, SomaLinxDryRunAdapter>();
        services.AddScoped<IGovernedToolAdapter, SomaLinxReadOnlyAdapter>();
        services.AddScoped<IGovernedToolAdapter, WiseGovernedAdapter>();
        services.AddScoped<IGovernedToolAdapter, LinxKnowledgeStoreReadOnlyAdapter>();
        services.AddScoped<IToolGateway, ToolGateway>();
        services.AddScoped<GovernedWriteStack>();
        services.AddScoped<GovernedPlanBridge>();
        return services;
    }
}
