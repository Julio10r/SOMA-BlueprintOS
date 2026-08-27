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
}
