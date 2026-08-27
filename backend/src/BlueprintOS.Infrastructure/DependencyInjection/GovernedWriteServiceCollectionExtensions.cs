using BlueprintOS.Core.AI.Governance;
using BlueprintOS.Core.AI.Governance.Contracts;
using BlueprintOS.Infrastructure.Persistence.Governance;
using Microsoft.Extensions.DependencyInjection;

namespace BlueprintOS.Infrastructure.DependencyInjection;

public static class GovernedWriteServiceCollectionExtensions
{
    public static IServiceCollection AddGovernedWriteStack(this IServiceCollection services)
    {
        services.AddScoped<IActionProposalAdapter, StructuredActionProposalAdapter>();
        services.AddScoped<IApprovalStore, EfApprovalStore>();
        services.AddScoped<IGovernanceAuditStore, EfGovernanceAuditStore>();
        services.AddScoped<IGovernedToolAdapter, SomaLinxDryRunAdapter>();
        services.AddScoped<IToolGateway, ToolGateway>();
        services.AddScoped<GovernedWriteStack>();
        return services;
    }
}
