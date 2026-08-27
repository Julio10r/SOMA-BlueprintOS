#pragma warning disable CS1591

using BlueprintOS.Core.AI.Governance.Models;

namespace BlueprintOS.Core.AI.Governance.Contracts;

public interface IActionProposalAdapter
{
    ActionProposalBuildResult Build(StructuredActionContext context, RoutingEvidence routing, AgentWriteAnalysis analysis, DateTimeOffset now);
}

public interface IApprovalStore
{
    Task SaveRequestAsync(ApprovalRequest request, CancellationToken cancellationToken = default);
    Task<ApprovalRequest?> GetRequestAsync(Guid requestId, CancellationToken cancellationToken = default);
    Task UpdateRequestStatusAsync(Guid requestId, ApprovalRequestStatus status, CancellationToken cancellationToken = default);
    Task SaveGrantAsync(ApprovalGrant grant, CancellationToken cancellationToken = default);
    Task<ApprovalGrant?> GetGrantAsync(Guid grantId, CancellationToken cancellationToken = default);
    Task RevokeGrantAsync(Guid grantId, DateTimeOffset revokedAt, CancellationToken cancellationToken = default);
}

public interface IGovernanceAuditStore
{
    Task AppendAsync(GovernanceAuditEvent auditEvent, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GovernanceAuditEvent>> ListByRequestAsync(string requestId, CancellationToken cancellationToken = default);
}

public interface IGovernedToolAdapter
{
    string Capability { get; }
    Task<SomaLinxDryRunPreview> DryRunAsync(ToolGatewayRequest request, CancellationToken cancellationToken = default);
}

public interface IToolGateway
{
    Task<ToolGatewayResult> InvokeAsync(ToolGatewayRequest request, CancellationToken cancellationToken = default);
}
