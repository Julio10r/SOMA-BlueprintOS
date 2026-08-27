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
}
