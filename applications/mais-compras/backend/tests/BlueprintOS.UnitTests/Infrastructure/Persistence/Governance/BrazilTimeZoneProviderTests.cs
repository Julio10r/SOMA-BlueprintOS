using BlueprintOS.Infrastructure.Persistence.Governance;

namespace BlueprintOS.UnitTests.Infrastructure.Persistence.Governance;

public sealed class BrazilTimeZoneProviderTests
{
    [Fact]
    public void Zone_Resolves_Without_Throwing_And_Is_Fixed_Minus_Three_Hours()
    {
        // Brazil has not observed DST since 2019 — America/Sao_Paulo is a fixed -03:00 offset year-round,
        // so no seasonal branching is needed anywhere that consumes this.
        var instant = new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero); // southern-hemisphere summer
        var winterInstant = new DateTimeOffset(2026, 7, 15, 12, 0, 0, TimeSpan.Zero); // southern-hemisphere winter

        Assert.Equal(TimeSpan.FromHours(-3), BrazilTimeZoneProvider.ToSaoPaulo(instant).Offset);
        Assert.Equal(TimeSpan.FromHours(-3), BrazilTimeZoneProvider.ToSaoPaulo(winterInstant).Offset);
    }

    [Fact]
    public void ToSaoPaulo_Preserves_The_Same_Absolute_Instant()
    {
        var utc = new DateTimeOffset(2026, 8, 28, 21, 8, 18, TimeSpan.Zero);
        var saoPaulo = BrazilTimeZoneProvider.ToSaoPaulo(utc);

        Assert.Equal(utc.ToUnixTimeSeconds(), saoPaulo.ToUnixTimeSeconds());
        Assert.Equal(utc.UtcDateTime, saoPaulo.UtcDateTime);
    }

    [Fact]
    public void ToSaoPaulo_Moves_The_Calendar_Date_Back_Across_Midnight_Utc()
    {
        // 2026-08-29T01:00:00Z is 2026-08-28T22:00:00-03:00 — the local calendar date is the PREVIOUS day.
        var utc = new DateTimeOffset(2026, 8, 29, 1, 0, 0, TimeSpan.Zero);
        var saoPaulo = BrazilTimeZoneProvider.ToSaoPaulo(utc);

        Assert.Equal(new DateTime(2026, 8, 28, 22, 0, 0), saoPaulo.DateTime);
        Assert.Equal(TimeSpan.FromHours(-3), saoPaulo.Offset);
    }
}

public sealed class SaoPauloTimeProviderTests
{
    [Fact]
    public void GetUtcNow_Carries_An_Explicit_Minus_Three_Hour_Offset()
    {
        var now = SaoPauloTimeProvider.Instance.GetUtcNow();

        Assert.Equal(TimeSpan.FromHours(-3), now.Offset);
        // ISO 8601 round-trip ("O"/"o") must show the explicit offset, never "Z"/"+00:00".
        Assert.EndsWith("-03:00", now.ToString("yyyy-MM-ddTHH:mm:ss.fffffffzzz"));
    }

    [Fact]
    public void GetUtcNow_Represents_The_Same_Real_Instant_As_The_System_Clock()
    {
        var systemNow = TimeProvider.System.GetUtcNow();
        var saoPauloNow = SaoPauloTimeProvider.Instance.GetUtcNow();

        // Both calls happen microseconds apart in the test process — allow a small tolerance, but this proves
        // SaoPauloTimeProvider is a re-rendering of "now", not a different clock or a fixed offset in time.
        Assert.True(Math.Abs((systemNow - saoPauloNow).TotalSeconds) < 5);
    }
}
