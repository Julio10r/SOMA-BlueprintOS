using BlueprintOS.Core.AI.Governance.Models;

namespace BlueprintOS.Infrastructure.Persistence.Governance;

/// <summary>
/// Resolves the physical DATABASE bucket (<c>SOMA</c>, <c>SOMA_DESENV</c>, ...) a piece of governance
/// bookkeeping belongs under on disk — <c>runtime/governance/&lt;database&gt;/...</c> — from a logical
/// connection profile name (<c>linx-development</c>, <c>linx-production</c>, <c>wise</c>).
///
/// This NEVER guesses the database by string-parsing the profile name (e.g. turning "linx-production" into
/// "SOMA" by convention): it looks up <see cref="LinxConnectionProfiles"/>' <c>ExpectedDatabase</c> — the
/// same canonical, already-validated field <c>LinxConnectionStringResolver.Resolve</c> checks the real
/// connection string against before any live write is allowed to proceed. For the one governed profile with
/// no physical SQL database at all (<c>wise</c> — config-only), the bucket is the profile name itself, since
/// there is no database identity to resolve.
///
/// This resolver is used ONLY for the pre-execution phases (`propose`/`approve`) where a live connection has
/// not yet been validated. Once a live write is about to run, the REAL, validated <c>Database</c> already
/// carried on the request/manifest is used directly — never re-resolved through this type.
/// </summary>
public static class GovernanceDatabaseResolver
{
    public const string UnknownBucket = "unknown";

    public static string ResolveForConnectionProfile(string? connectionProfile)
    {
        if (string.IsNullOrWhiteSpace(connectionProfile)) return UnknownBucket;

        return connectionProfile switch
        {
            WriteVerificationProfileSeeds.LinxDevelopment => LinxConnectionProfiles.Development.ExpectedDatabase,
            WriteVerificationProfileSeeds.LinxProduction => LinxConnectionProfiles.Production.ExpectedDatabase,
            WriteVerificationProfileSeeds.Wise => WriteVerificationProfileSeeds.Wise,
            _ => Sanitize(connectionProfile),
        };
    }

    private static string Sanitize(string value)
    {
        var cleaned = new string(value.Trim().Select(c => char.IsLetterOrDigit(c) || c is '-' or '_' ? c : '-').ToArray()).Trim('-');
        return string.IsNullOrEmpty(cleaned) ? UnknownBucket : cleaned;
    }
}
