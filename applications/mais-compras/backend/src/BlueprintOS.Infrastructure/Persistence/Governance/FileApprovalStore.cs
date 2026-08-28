using BlueprintOS.Core.AI.Governance.Contracts;
using BlueprintOS.Core.AI.Governance.Models;

namespace BlueprintOS.Infrastructure.Persistence.Governance;

/// <summary>
/// File-based <see cref="IApprovalStore"/> under <c>{root}/approvals/</c>. One file per request
/// (<c>requests/{id:N}.json</c>) and one per grant (<c>grants/{id:N}.json</c>) — both keyed by Id, no natural
/// timestamp partitioning is used here because approval volume is low and lookups are always by-id.
/// </summary>
public sealed class FileApprovalStore(string rootDirectory) : IApprovalStore
{
    private readonly string _requestsRoot = Path.Combine(rootDirectory, "approvals", "requests");
    private readonly string _grantsRoot = Path.Combine(rootDirectory, "approvals", "grants");

    private string RequestPath(Guid id) => Path.Combine(_requestsRoot, $"{id:N}.json");
    private string GrantPath(Guid id) => Path.Combine(_grantsRoot, $"{id:N}.json");

    public Task SaveRequestAsync(ApprovalRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var path = RequestPath(request.Id);
        return AtomicFileWriter.WithFileLockAsync(path, () => AtomicFileWriter.WriteJsonAsync(path, request, cancellationToken).ContinueWith(_ => true, cancellationToken));
    }

    public Task<ApprovalRequest?> GetRequestAsync(Guid requestId, CancellationToken cancellationToken = default) =>
        AtomicFileWriter.ReadJsonAsync<ApprovalRequest>(RequestPath(requestId), cancellationToken);

    public Task UpdateRequestStatusAsync(Guid requestId, ApprovalRequestStatus status, CancellationToken cancellationToken = default)
    {
        var path = RequestPath(requestId);
        return AtomicFileWriter.WithFileLockAsync(path, async () =>
        {
            var existing = await AtomicFileWriter.ReadJsonAsync<ApprovalRequest>(path, cancellationToken)
                ?? throw new KeyNotFoundException($"Approval request {requestId} was not found.");
            await AtomicFileWriter.WriteJsonAsync(path, existing with { Status = status }, cancellationToken);
            return true;
        });
    }

    public Task SaveGrantAsync(ApprovalGrant grant, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(grant);
        var path = GrantPath(grant.Id);
        return AtomicFileWriter.WithFileLockAsync(path, () => AtomicFileWriter.WriteJsonAsync(path, grant, cancellationToken).ContinueWith(_ => true, cancellationToken));
    }

    public Task<ApprovalGrant?> GetGrantAsync(Guid grantId, CancellationToken cancellationToken = default) =>
        AtomicFileWriter.ReadJsonAsync<ApprovalGrant>(GrantPath(grantId), cancellationToken);

    public Task RevokeGrantAsync(Guid grantId, DateTimeOffset revokedAt, CancellationToken cancellationToken = default)
    {
        var path = GrantPath(grantId);
        return AtomicFileWriter.WithFileLockAsync(path, async () =>
        {
            var existing = await AtomicFileWriter.ReadJsonAsync<ApprovalGrant>(path, cancellationToken)
                ?? throw new KeyNotFoundException($"Approval grant {grantId} was not found.");
            await AtomicFileWriter.WriteJsonAsync(path, existing with { RevokedAt = revokedAt }, cancellationToken);
            return true;
        });
    }
}
