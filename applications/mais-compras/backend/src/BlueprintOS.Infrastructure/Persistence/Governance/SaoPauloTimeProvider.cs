namespace BlueprintOS.Infrastructure.Persistence.Governance;

/// <summary>
/// A <see cref="TimeProvider"/> whose <see cref="GetUtcNow"/> returns the current instant expressed with the
/// America/Sao_Paulo (-03:00) offset instead of the base class's Offset==Zero contract. The absolute instant
/// is identical to <c>TimeProvider.System.GetUtcNow()</c> — only the offset used to render it (and therefore
/// every derived ISO 8601 string, and every YYYY-MM-DD/HHmm folder computed from it) changes.
///
/// Used exclusively by <c>GovernedExecuteCliHandler</c> (the real, persisted LIVE-execution path) so every
/// NEW ActionProposal/PolicyDecision/ApprovalRequest/ApprovalGrant/RecoveryPackage/Audit record it creates
/// carries an explicit -03:00 offset and lands in a São-Paulo-dated folder. <c>governed-plan</c> (in-memory,
/// dry-run, never persisted) is intentionally left on <c>TimeProvider.System</c> — this is a narrowly-scoped
/// fix for real, persisted writes, not a global clock change.
/// </summary>
public sealed class SaoPauloTimeProvider : TimeProvider
{
    public static readonly SaoPauloTimeProvider Instance = new();

    private SaoPauloTimeProvider()
    {
    }

    public override DateTimeOffset GetUtcNow() => BrazilTimeZoneProvider.ToSaoPaulo(DateTimeOffset.UtcNow);
}
