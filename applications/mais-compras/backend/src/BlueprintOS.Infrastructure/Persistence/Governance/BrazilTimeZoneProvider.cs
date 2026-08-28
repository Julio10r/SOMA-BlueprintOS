namespace BlueprintOS.Infrastructure.Persistence.Governance;

/// <summary>
/// Canonical operational timezone for the Agents runtime: America/Sao_Paulo. Brazil has not observed DST
/// since 2019, so this is a fixed -03:00 offset year-round — no seasonal edge cases to reason about.
///
/// Used ONLY to decide (a) which YYYY-MM-DD/HHmm folder a NEW record is written under, and (b) the offset
/// carried by NEW persisted timestamps (via <see cref="SaoPauloTimeProvider"/>). It is never used to
/// reinterpret or move any historical artifact: every File*Store's lookup-by-id scans all date partitions
/// rather than recomputing an expected path from "now", so folders named under the old (UTC) convention
/// remain fully discoverable after this change — see each store's <c>FindPathByIdAsync</c>/<c>ScanAllAsync</c>.
/// </summary>
public static class BrazilTimeZoneProvider
{
    /// <summary>
    /// Resolved once, with a fallback chain: IANA id (Linux/macOS, and Windows with ICU), then the Windows
    /// legacy id, then a manually-constructed fixed -03:00 zone as a last resort so this can never throw at
    /// runtime regardless of the host's tzdata availability.
    /// </summary>
    public static readonly TimeZoneInfo Zone = Resolve();

    /// <summary>Converts any instant to its America/Sao_Paulo local representation — same absolute instant,
    /// offset re-expressed as -03:00 (or whatever <see cref="Zone"/> resolved to, in the fallback case).</summary>
    public static DateTimeOffset ToSaoPaulo(DateTimeOffset instant) => TimeZoneInfo.ConvertTime(instant, Zone);

    private static TimeZoneInfo Resolve()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo"); }
        catch (TimeZoneNotFoundException) { }
        catch (InvalidTimeZoneException) { }

        try { return TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time"); }
        catch (TimeZoneNotFoundException) { }
        catch (InvalidTimeZoneException) { }

        return TimeZoneInfo.CreateCustomTimeZone("America/Sao_Paulo (fixed fallback)", TimeSpan.FromHours(-3), "Horario de Brasilia (fallback)", "Horario de Brasilia (fallback)");
    }
}
