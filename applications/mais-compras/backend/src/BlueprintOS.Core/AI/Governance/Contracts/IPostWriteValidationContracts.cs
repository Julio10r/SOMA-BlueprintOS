#pragma warning disable CS1591

using BlueprintOS.Core.AI.Governance.Models;

namespace BlueprintOS.Core.AI.Governance.Contracts;

/// <summary>Resolves the post-write validation rule for an (operation, resource) pair. Returning null means
/// "we do not know how to verify this write" and must block the write, never permit an unverified one.</summary>
public interface IPostWriteValidationRuleCatalog
{
    PostWriteValidationRule? Resolve(ActionOperation operation, string resource);

    IReadOnlyList<PostWriteValidationRule> ListRules();
}

/// <summary>Persistent record of write-validation knowledge gaps, so an unverifiable write leaves a trace a
/// human can act on instead of silently disappearing.</summary>
public interface IWriteValidationKnowledgeGapStore
{
    Task RecordAsync(WriteValidationKnowledgeGap gap, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WriteValidationKnowledgeGap>> ListAsync(CancellationToken cancellationToken = default);
}

/// <summary>Persistent record of rollback-capability gaps (a profile requires rollback support that the
/// capability performing the write does not offer), so the blocked attempt leaves a trace instead of silently
/// disappearing.</summary>
public interface IRollbackCapabilityGapStore
{
    Task RecordAsync(RollbackCapabilityGap gap, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RollbackCapabilityGap>> ListAsync(CancellationToken cancellationToken = default);
}
