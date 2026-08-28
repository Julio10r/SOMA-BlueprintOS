using System.Collections.Concurrent;
using BlueprintOS.Core.AI.Governance.Contracts;
using BlueprintOS.Core.AI.Governance.Models;

namespace BlueprintOS.Api.Governance;

/// <summary>
/// Process-lifetime, non-persisted approval/audit stores used only by the
/// `governed-plan` CLI command (<see cref="GovernedPlanCliHandler"/>). This CLI
/// invocation must work fully offline with no external connection, so it does
/// not use the application's real SqlServer-backed EF stores
/// (EfApprovalStore/EfGovernanceAuditStore in BlueprintOS.Infrastructure) —
/// those remain the ones wired into the real host via AddGovernedWriteStack()
/// + AddInfrastructure() for any long-lived process. Nothing recorded here
/// survives past a single CLI invocation, which is intentional: this command
/// currently only proves the plan → proposal → policy transport is real, not
/// a persisted approval workflow.
/// </summary>
public sealed class InMemoryApprovalStore : IApprovalStore
{
    private readonly ConcurrentDictionary<Guid, ApprovalRequest> _requests = new();
    private readonly ConcurrentDictionary<Guid, ApprovalGrant> _grants = new();

    public Task SaveRequestAsync(ApprovalRequest request, CancellationToken cancellationToken = default)
    {
        _requests[request.Id] = request;
        return Task.CompletedTask;
    }

    public Task<ApprovalRequest?> GetRequestAsync(Guid requestId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_requests.GetValueOrDefault(requestId));

    public Task UpdateRequestStatusAsync(Guid requestId, ApprovalRequestStatus status, CancellationToken cancellationToken = default)
    {
        if (_requests.TryGetValue(requestId, out var existing)) _requests[requestId] = existing with { Status = status };
        return Task.CompletedTask;
    }

    public Task SaveGrantAsync(ApprovalGrant grant, CancellationToken cancellationToken = default)
    {
        _grants[grant.Id] = grant;
        return Task.CompletedTask;
    }

    public Task<ApprovalGrant?> GetGrantAsync(Guid grantId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_grants.GetValueOrDefault(grantId));

    public Task RevokeGrantAsync(Guid grantId, DateTimeOffset revokedAt, CancellationToken cancellationToken = default)
    {
        if (_grants.TryGetValue(grantId, out var existing)) _grants[grantId] = existing with { RevokedAt = revokedAt };
        return Task.CompletedTask;
    }
}

public sealed class InMemoryPlanAuditStore : IGovernanceAuditStore
{
    private readonly ConcurrentBag<GovernanceAuditEvent> _events = new();

    public Task AppendAsync(GovernanceAuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        _events.Add(auditEvent);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<GovernanceAuditEvent>> ListByRequestAsync(string requestId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<GovernanceAuditEvent>>(_events.Where(e => e.RequestId == requestId).ToList());
}
