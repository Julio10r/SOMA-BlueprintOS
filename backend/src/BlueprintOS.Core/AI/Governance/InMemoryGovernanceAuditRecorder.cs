#pragma warning disable CS1591

#pragma warning disable CS1591

using BlueprintOS.Core.AI.Governance.Contracts;
using BlueprintOS.Core.AI.Governance.Models;

namespace BlueprintOS.Core.AI.Governance;

public sealed class InMemoryGovernanceAuditRecorder : IGovernanceAuditRecorder
{
    private readonly List<GovernanceAuditEntry> _entries = new();

    public IReadOnlyList<GovernanceAuditEntry> Entries => _entries;

    public void Record(GovernanceAuditEntry entry)
    {
        _entries.Add(entry);
    }
}

