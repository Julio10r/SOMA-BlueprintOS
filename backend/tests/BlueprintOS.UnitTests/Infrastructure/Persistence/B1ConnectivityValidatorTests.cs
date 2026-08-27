using BlueprintOS.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;

namespace BlueprintOS.UnitTests.Infrastructure.Persistence;

/// <summary>Cobre a classificação de status do <see cref="B1ConnectivityValidator"/> (Continuation do caso
/// Linx PROG/OP/PED — investigação da conexão local read-only) sem tocar rede/banco: apenas os caminhos
/// que não exigem uma conexão real (configuração ausente/placeholder) são exercitados aqui. O caminho de
/// sucesso/permission-denied contra um banco real é validado manualmente e fora do CI (não há como
/// simular um SQL Server sem infraestrutura adicional), mas a garantia estrutural — nunca expor a
/// connection string, nunca promover NotConfigured/Failed a sucesso — é coberta por estes testes.</summary>
public sealed class B1ConnectivityValidatorTests
{
    [Fact]
    public async Task ValidateErpAsync_Should_Report_NotConfigured_When_ConnectionString_Is_Missing()
    {
        var configuration = new ConfigurationBuilder().Build();
        var validator = new B1ConnectivityValidator(configuration);

        var result = await validator.ValidateErpAsync();

        Assert.Equal(ConnectivityStatus.NotConfigured, result.Status);
        Assert.False(result.IsSuccess);
        Assert.Null(result.EffectiveIdentity);
    }

    [Fact]
    public async Task ValidateErpAsync_Should_Report_NotConfigured_When_ConnectionString_Is_Unresolved_Placeholder()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:ErpConnection"] = "__SET_VIA_USER_SECRETS_OR_CONNECTIONSTRINGS__ERPCONNECTION__",
            })
            .Build();
        var validator = new B1ConnectivityValidator(configuration);

        var result = await validator.ValidateErpAsync();

        Assert.Equal(ConnectivityStatus.NotConfigured, result.Status);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task ValidateErpAsync_NotConfigured_Result_Should_Never_Carry_The_Connection_String()
    {
        const string secretLookingValue = "Server=192.168.9.98;Database=SOMA_DESENV;User Id=super-secret-user;Password=super-secret-password;";
        var configuration = new ConfigurationBuilder().Build();
        var validator = new B1ConnectivityValidator(configuration);

        var result = await validator.ValidateErpAsync();

        Assert.DoesNotContain(secretLookingValue, result.ToString());
        Assert.DoesNotContain("Password", result.Exception?.Message ?? string.Empty);
    }

    [Fact]
    public async Task ValidateErpAsync_Should_Fail_Fast_Without_Opening_A_Connection_When_Not_Configured()
    {
        // Regression guard: a misconfigured/empty connection string must short-circuit to NotConfigured
        // instead of attempting SqlConnection.OpenAsync (which would hang/timeout against no server).
        var configuration = new ConfigurationBuilder().Build();
        var validator = new B1ConnectivityValidator(configuration);

        var task = validator.ValidateErpAsync();
        var completed = await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(2)));

        Assert.Same(task, completed);
        Assert.Equal(ConnectivityStatus.NotConfigured, (await task).Status);
    }

    [Fact]
    public async Task ValidateErpAsync_Development_Should_Report_NotConfigured_When_Dev_And_Legacy_Secrets_Are_Both_Missing()
    {
        var configuration = new ConfigurationBuilder().Build();
        var validator = new B1ConnectivityValidator(configuration);

        var result = await validator.ValidateErpAsync(LinxEnvironment.Development);

        Assert.Equal(ConnectivityStatus.NotConfigured, result.Status);
    }

    [Fact]
    public async Task ValidateErpAsync_Production_Should_Report_NotConfigured_When_Prod_Secret_Is_Missing()
    {
        var configuration = new ConfigurationBuilder().Build();
        var validator = new B1ConnectivityValidator(configuration);

        var result = await validator.ValidateErpAsync(LinxEnvironment.Production);

        Assert.Equal(ConnectivityStatus.NotConfigured, result.Status);
    }

    [Fact]
    public async Task ValidateErpAsync_Production_Should_Not_Resolve_The_Development_Secret()
    {
        // A DEV secret being present must never leak into a Production resolution — separate keys, no inference.
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:LinxDevelopmentConnection"] = "Server=192.168.9.98;Database=SOMA_DESENV;User Id=dev;Password=dev;",
            })
            .Build();
        var validator = new B1ConnectivityValidator(configuration);

        var result = await validator.ValidateErpAsync(LinxEnvironment.Production);

        Assert.Equal(ConnectivityStatus.NotConfigured, result.Status);
    }

    [Fact]
    public async Task ValidateErpAsync_Development_Should_Fall_Back_To_Legacy_ErpConnection_When_New_Key_Is_Absent()
    {
        // The legacy key resolves and is still validated against the Development profile (mismatch guard
        // applies to the fallback the same as the canonical key) — no live connection needed to prove that.
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:ErpConnection"] = "Server=192.168.0.200;Database=SOMA;User Id=dev;Password=dev;",
            })
            .Build();
        var validator = new B1ConnectivityValidator(configuration);

        var result = await validator.ValidateErpAsync(LinxEnvironment.Development);

        // NotConfigured would mean the legacy fallback never resolved; here it resolved and was then
        // correctly rejected as a Production target under a Development profile.
        Assert.Equal(ConnectivityStatus.EnvironmentMismatch, result.Status);
    }

    [Fact]
    public async Task ValidateErpAsync_Development_Should_Block_When_Configured_Target_Is_The_Production_Server()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:LinxDevelopmentConnection"] = "Server=192.168.0.200;Database=SOMA;User Id=x;Password=x;",
            })
            .Build();
        var validator = new B1ConnectivityValidator(configuration);

        var result = await validator.ValidateErpAsync(LinxEnvironment.Development);

        Assert.Equal(ConnectivityStatus.EnvironmentMismatch, result.Status);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task ValidateErpAsync_Production_Should_Block_When_Configured_Target_Is_The_Development_Database()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:LinxProductionConnection"] = "Server=192.168.0.200;Database=SOMA_DESENV;User Id=x;Password=x;",
            })
            .Build();
        var validator = new B1ConnectivityValidator(configuration);

        var result = await validator.ValidateErpAsync(LinxEnvironment.Production);

        Assert.Equal(ConnectivityStatus.EnvironmentMismatch, result.Status);
    }

    [Fact]
    public async Task ValidateErpAsync_EnvironmentMismatch_Should_Never_Attempt_A_Network_Connection()
    {
        // Points at a non-routable/reserved address so a real connection attempt would hang/timeout;
        // the mismatch guard must short-circuit before OpenAsync is ever called.
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:LinxProductionConnection"] = "Server=10.255.255.1;Database=SOMA_DESENV;User Id=x;Password=x;",
            })
            .Build();
        var validator = new B1ConnectivityValidator(configuration);

        var task = validator.ValidateErpAsync(LinxEnvironment.Production);
        var completed = await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(2)));

        Assert.Same(task, completed);
        Assert.Equal(ConnectivityStatus.EnvironmentMismatch, (await task).Status);
    }

    [Fact]
    public async Task ValidateErpAsync_EnvironmentMismatch_Result_Should_Never_Carry_The_Connection_String()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:LinxDevelopmentConnection"] = "Server=192.168.0.200;Database=SOMA;User Id=super-secret-user;Password=super-secret-password;",
            })
            .Build();
        var validator = new B1ConnectivityValidator(configuration);

        var result = await validator.ValidateErpAsync(LinxEnvironment.Development);

        Assert.DoesNotContain("super-secret-password", result.ToString());
        Assert.DoesNotContain("Password", result.Exception?.Message ?? string.Empty);
    }

    [Fact]
    public async Task ValidateMaisComprasAsync_Should_Report_NotConfigured_When_Secret_Is_Missing()
    {
        var configuration = new ConfigurationBuilder().Build();
        var validator = new B1ConnectivityValidator(configuration);

        var result = await validator.ValidateMaisComprasAsync();

        Assert.Equal(ConnectivityStatus.NotConfigured, result.Status);
    }

    [Fact]
    public async Task ValidateMaisComprasAsync_Should_Block_When_Configured_Target_Is_SOMA_DESENV()
    {
        // Same DEV server (192.168.9.98) as linx-development is legitimate; a different database under
        // that server pointing at SOMA_DESENV instead of MAISCOMPRAS is still a mismatch.
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:MaisComprasConnection"] = "Server=192.168.9.98;Database=SOMA_DESENV;User Id=dev;Password=dev;",
            })
            .Build();
        var validator = new B1ConnectivityValidator(configuration);

        var result = await validator.ValidateMaisComprasAsync();

        Assert.Equal(ConnectivityStatus.EnvironmentMismatch, result.Status);
    }

    [Fact]
    public async Task ValidateMaisComprasAsync_Should_Block_When_Configured_Target_Is_The_Production_Server()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:MaisComprasConnection"] = "Server=192.168.0.200;Database=MAISCOMPRAS;User Id=x;Password=x;",
            })
            .Build();
        var validator = new B1ConnectivityValidator(configuration);

        var result = await validator.ValidateMaisComprasAsync();

        Assert.Equal(ConnectivityStatus.EnvironmentMismatch, result.Status);
    }

    [Fact]
    public void LinxDevelopment_And_MaisComprasDevelopment_Profiles_Should_Share_The_Same_DEV_Server_With_Distinct_Databases()
    {
        // Formalizes that the two DEV profiles may resolve to the same local identity (same server,
        // different ConnectionStrings key/database) without either profile carrying a credential itself.
        Assert.Equal(LinxConnectionProfiles.Development.ExpectedServer, LinxConnectionProfiles.MaisComprasDevelopment.ExpectedServer);
        Assert.NotEqual(LinxConnectionProfiles.Development.ExpectedDatabase, LinxConnectionProfiles.MaisComprasDevelopment.ExpectedDatabase);
        Assert.NotEqual(LinxConnectionProfiles.Development.ConnectionName, LinxConnectionProfiles.MaisComprasDevelopment.ConnectionName);
        Assert.True(LinxConnectionProfiles.MaisComprasDevelopment.VpnRequired);
    }

    [Fact]
    public void LinxConnectionStringResolver_Should_Throw_Without_Connecting_When_Not_Configured()
    {
        var configuration = new ConfigurationBuilder().Build();

        var exception = Assert.Throws<InvalidOperationException>(
            () => LinxConnectionStringResolver.Resolve(configuration, LinxConnectionProfiles.Production));

        Assert.Contains("LinxProductionConnection", exception.Message);
    }

    [Fact]
    public void LinxConnectionStringResolver_Should_Reject_A_Development_Connection_String_Passed_As_Production()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:LinxProductionConnection"] = "Server=192.168.9.98;Database=SOMA_DESENV;User Id=x;Password=x;",
            })
            .Build();

        var exception = Assert.Throws<InvalidOperationException>(
            () => LinxConnectionStringResolver.Resolve(configuration, LinxConnectionProfiles.Production));

        Assert.Contains("Environment mismatch", exception.Message);
        Assert.DoesNotContain("Password", exception.Message);
    }

    // --- Retry único de conectividade (nunca VPN "diagnosticada", só CONNECTIVITY_UNAVAILABLE) ---

    private static IConfiguration ProductionConfiguration() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:LinxProductionConnection"] = "Server=192.168.0.200;Database=SOMA;User Id=x;Password=x;",
        })
        .Build();

    [Fact]
    public async Task ValidateErpAsync_Should_Recover_And_Report_Ready_When_The_Single_Retry_Succeeds()
    {
        var probe = new FakeSqlConnectivityProbe(
            new TimeoutException("connectivity blip"),
            (Func<int, string?>)(_ => "ti.n8n"));
        var validator = new B1ConnectivityValidator(ProductionConfiguration(), probe);

        var result = await validator.ValidateErpAsync(LinxEnvironment.Production);

        Assert.Equal(ConnectivityStatus.Ready, result.Status);
        Assert.True(result.RecoveredAfterRetry);
        Assert.Equal("ti.n8n", result.EffectiveIdentity);
        Assert.Equal(2, probe.CallCount);
    }

    [Fact]
    public async Task ValidateErpAsync_Should_Report_ConnectivityUnavailable_When_Both_Attempts_Fail()
    {
        var probe = new FakeSqlConnectivityProbe(
            new TimeoutException("connectivity blip"),
            new TimeoutException("still unreachable"));
        var validator = new B1ConnectivityValidator(ProductionConfiguration(), probe);

        var result = await validator.ValidateErpAsync(LinxEnvironment.Production);

        Assert.Equal(ConnectivityStatus.ConnectivityUnavailable, result.Status);
        Assert.False(result.IsSuccess);
        Assert.Equal(2, probe.CallCount);
    }

    [Fact]
    public async Task ValidateErpAsync_Should_Never_Retry_On_Permission_Denied()
    {
        var probe = new FakeSqlConnectivityProbe(new FakePermissionDeniedException());
        var validator = new B1ConnectivityValidator(ProductionConfiguration(), probe);

        var result = await validator.ValidateErpAsync(LinxEnvironment.Production);

        Assert.Equal(ConnectivityStatus.PermissionDenied, result.Status);
        Assert.Equal(1, probe.CallCount);
    }

    [Fact]
    public async Task ValidateErpAsync_Should_Never_Retry_On_NotConfigured()
    {
        var probe = new FakeSqlConnectivityProbe(new TimeoutException("should never be called"));
        var validator = new B1ConnectivityValidator(new ConfigurationBuilder().Build(), probe);

        var result = await validator.ValidateErpAsync(LinxEnvironment.Production);

        Assert.Equal(ConnectivityStatus.NotConfigured, result.Status);
        Assert.Equal(0, probe.CallCount);
    }

    [Fact]
    public async Task ValidateErpAsync_Should_Never_Retry_On_EnvironmentMismatch()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:LinxProductionConnection"] = "Server=192.168.9.98;Database=SOMA_DESENV;User Id=x;Password=x;",
            })
            .Build();
        var probe = new FakeSqlConnectivityProbe(new TimeoutException("should never be called"));
        var validator = new B1ConnectivityValidator(configuration, probe);

        var result = await validator.ValidateErpAsync(LinxEnvironment.Production);

        Assert.Equal(ConnectivityStatus.EnvironmentMismatch, result.Status);
        Assert.Equal(0, probe.CallCount);
    }

    [Fact]
    public async Task ValidateErpAsync_Should_Attempt_At_Most_Two_Probe_Calls_Regardless_Of_Repeated_Failures()
    {
        // Guard against a future change accidentally turning this into a retry loop.
        var probe = new FakeSqlConnectivityProbe(
            new TimeoutException("first failure"),
            new TimeoutException("second failure"));
        var validator = new B1ConnectivityValidator(ProductionConfiguration(), probe);

        await validator.ValidateErpAsync(LinxEnvironment.Production);

        Assert.True(probe.CallCount <= 2, $"Expected at most 2 probe calls (1 initial + 1 retry), got {probe.CallCount}.");
    }

    private sealed class FakePermissionDeniedException() : Exception("simulated permission denied"), ISimulatedSqlFailure
    {
        public bool IsPermissionDenied => true;
    }

    /// <summary>Fake de <see cref="ISqlConnectivityProbe"/> que reproduz uma sequência fixa de
    /// resultados/exceções por chamada — nunca abre uma conexão real, então os testes de retry rodam em
    /// milissegundos e não dependem de rede/VPN/SQL Server.</summary>
    private sealed class FakeSqlConnectivityProbe : ISqlConnectivityProbe
    {
        private readonly Queue<Func<int, string?>> behaviors;
        public int CallCount { get; private set; }

        public FakeSqlConnectivityProbe(params object[] behaviors)
        {
            this.behaviors = new Queue<Func<int, string?>>(behaviors.Select(ToBehavior));
        }

        private static Func<int, string?> ToBehavior(object behavior) => behavior switch
        {
            Exception exception => _ => throw exception,
            Func<int, string?> func => func,
            string identity => _ => identity,
            null => _ => null,
            _ => throw new ArgumentException($"Unsupported behavior type: {behavior.GetType()}"),
        };

        public Task<string?> ProbeAsync(string connectionString, CancellationToken cancellationToken)
        {
            CallCount++;
            var behavior = behaviors.Count > 0 ? behaviors.Dequeue() : (_ => throw new InvalidOperationException("FakeSqlConnectivityProbe called more times than configured."));
            return Task.FromResult(behavior(CallCount));
        }
    }
}
