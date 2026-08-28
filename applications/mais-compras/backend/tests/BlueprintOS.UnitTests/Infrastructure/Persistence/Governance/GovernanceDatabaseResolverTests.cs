#pragma warning disable CS1591

using BlueprintOS.Infrastructure.Persistence.Governance;
using Xunit;

namespace BlueprintOS.UnitTests.Infrastructure.Persistence.Governance;

/// <summary>
/// <see cref="GovernanceDatabaseResolver"/> must resolve the physical database bucket from the CANONICAL
/// <see cref="BlueprintOS.Infrastructure.Persistence.LinxConnectionProfiles"/> registry's ExpectedDatabase —
/// never by string-parsing/guessing from the connection profile's own name.
/// </summary>
public sealed class GovernanceDatabaseResolverTests
{
    [Fact]
    public void LinxDevelopment_Resolves_To_SOMA_DESENV()
    {
        Assert.Equal("SOMA_DESENV", GovernanceDatabaseResolver.ResolveForConnectionProfile("linx-development"));
    }

    [Fact]
    public void LinxProduction_Resolves_To_SOMA()
    {
        Assert.Equal("SOMA", GovernanceDatabaseResolver.ResolveForConnectionProfile("linx-production"));
    }

    [Fact]
    public void Wise_Has_No_Physical_Database_So_The_Bucket_Is_The_Profile_Name_Itself()
    {
        Assert.Equal("wise", GovernanceDatabaseResolver.ResolveForConnectionProfile("wise"));
    }

    [Fact]
    public void Null_Or_Empty_Falls_Back_To_The_Unknown_Bucket()
    {
        Assert.Equal(GovernanceDatabaseResolver.UnknownBucket, GovernanceDatabaseResolver.ResolveForConnectionProfile(null));
        Assert.Equal(GovernanceDatabaseResolver.UnknownBucket, GovernanceDatabaseResolver.ResolveForConnectionProfile("  "));
    }

    [Fact]
    public void Production_And_Development_Resolve_To_Physically_Different_Buckets()
    {
        var dev = GovernanceDatabaseResolver.ResolveForConnectionProfile("linx-development");
        var prod = GovernanceDatabaseResolver.ResolveForConnectionProfile("linx-production");
        Assert.NotEqual(dev, prod);
    }
}
