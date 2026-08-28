using System.Text.Json;
using BlueprintOS.Core.AI.Governance.Contracts;
using BlueprintOS.Core.AI.Governance.Models;
using Microsoft.EntityFrameworkCore;

namespace BlueprintOS.Infrastructure.Persistence.Governance;

public sealed class EfApprovalStore(BlueprintOSDbContext context) : IApprovalStore
{
    public async Task SaveRequestAsync(ApprovalRequest request, CancellationToken cancellationToken = default)
    {
        context.AIGovernanceApprovalRequests.Add(new GovernanceApprovalRequestEntity
        {
            Id = request.Id,
            ActionProposalId = request.ActionProposalId,
            ProposalHash = request.ProposalHash,
            RiskClassification = request.RiskClassification,
            Reason = request.Reason,
            RequiredApprover = request.RequiredApprover,
            CreatedAt = request.CreatedAt,
            ExpiresAt = request.ExpiresAt,
            Status = request.Status,
        });
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<ApprovalRequest?> GetRequestAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        var entity = await context.AIGovernanceApprovalRequests.AsNoTracking().SingleOrDefaultAsync(item => item.Id == requestId, cancellationToken);
        return entity is null ? null : new ApprovalRequest(entity.Id, entity.ActionProposalId, entity.ProposalHash,
            entity.RiskClassification, entity.Reason, entity.RequiredApprover, entity.CreatedAt, entity.ExpiresAt, entity.Status);
    }

    public async Task UpdateRequestStatusAsync(Guid requestId, ApprovalRequestStatus status, CancellationToken cancellationToken = default)
    {
        var entity = await context.AIGovernanceApprovalRequests.SingleOrDefaultAsync(item => item.Id == requestId, cancellationToken)
            ?? throw new KeyNotFoundException($"Approval request {requestId} was not found.");
        entity.Status = status;
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveGrantAsync(ApprovalGrant grant, CancellationToken cancellationToken = default)
    {
        context.AIGovernanceApprovalGrants.Add(new GovernanceApprovalGrantEntity
        {
            Id = grant.Id,
            ApprovalRequestId = grant.ApprovalRequestId,
            ProposalHash = grant.ProposalHash,
            ApprovedBy = grant.ApprovedBy,
            ApprovedAt = grant.ApprovedAt,
            ExpiresAt = grant.ExpiresAt,
            Scope = grant.Scope,
            Notes = grant.Notes,
            RevokedAt = grant.RevokedAt,
        });
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<ApprovalGrant?> GetGrantAsync(Guid grantId, CancellationToken cancellationToken = default)
    {
        var entity = await context.AIGovernanceApprovalGrants.AsNoTracking().SingleOrDefaultAsync(item => item.Id == grantId, cancellationToken);
        return entity is null ? null : Map(entity);
    }

    public async Task RevokeGrantAsync(Guid grantId, DateTimeOffset revokedAt, CancellationToken cancellationToken = default)
    {
        var entity = await context.AIGovernanceApprovalGrants.SingleOrDefaultAsync(item => item.Id == grantId, cancellationToken)
            ?? throw new KeyNotFoundException($"Approval grant {grantId} was not found.");
        entity.RevokedAt = revokedAt;
        await context.SaveChangesAsync(cancellationToken);
    }

    private static ApprovalGrant Map(GovernanceApprovalGrantEntity entity) => new(
        entity.Id, entity.ApprovalRequestId, entity.ProposalHash, entity.ApprovedBy,
        entity.ApprovedAt, entity.ExpiresAt, entity.Scope, entity.Notes, entity.RevokedAt);
}

public sealed class EfGovernanceAuditStore(BlueprintOSDbContext context) : IGovernanceAuditStore
{
    public async Task AppendAsync(GovernanceAuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        context.AIGovernanceAuditEvents.Add(new GovernanceAuditEventEntity
        {
            Id = auditEvent.Id,
            EventType = auditEvent.EventType,
            RequestId = auditEvent.RequestId,
            ActionProposalId = auditEvent.ActionProposalId,
            ProposalHash = auditEvent.ProposalHash,
            AgentId = auditEvent.AgentId,
            SubjectId = auditEvent.SubjectId,
            Outcome = auditEvent.Outcome,
            CategoriesJson = JsonSerializer.Serialize(auditEvent.Categories),
            CreatedAt = auditEvent.CreatedAt,
        });
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<GovernanceAuditEvent>> ListByRequestAsync(string requestId, CancellationToken cancellationToken = default)
    {
        var entities = await context.AIGovernanceAuditEvents.AsNoTracking()
            .Where(item => item.RequestId == requestId)
            .OrderBy(item => item.CreatedAt)
            .ToListAsync(cancellationToken);
        return entities.Select(item => new GovernanceAuditEvent(item.Id, item.EventType, item.RequestId, item.ActionProposalId,
            item.ProposalHash, item.AgentId, item.SubjectId, item.Outcome, item.Categories, item.CreatedAt)).ToArray();
    }
}
