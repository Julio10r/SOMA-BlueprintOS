using BlueprintOS.Domain.Identity;

namespace BlueprintOS.UnitTests.Domain.Identity;

/// <summary>O1.12 — invariantes de domínio de <see cref="RegraOrcamentaria"/>: apenas o cadastro (sem
/// reserva/consumo), valor limite positivo e Centro de Custo obrigatório.</summary>
public sealed class RegraOrcamentariaTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 11, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Should_Start_Active()
    {
        var regra = new RegraOrcamentaria("Regra", Guid.NewGuid(), Guid.NewGuid(), 1000m, PeriodoOrcamentario.Mensal, T0);

        Assert.True(regra.Ativo);
        Assert.Equal(1000m, regra.ValorLimite);
        Assert.Equal(PeriodoOrcamentario.Mensal, regra.Periodo);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Should_Reject_Non_Positive_ValorLimite(decimal valorLimite) =>
        Assert.Throws<ArgumentException>(() => new RegraOrcamentaria("Regra", Guid.NewGuid(), Guid.NewGuid(), valorLimite, PeriodoOrcamentario.Mensal, T0));

    [Fact]
    public void Should_Reject_Empty_CentroCustoMetadadoId() =>
        Assert.Throws<ArgumentException>(() => new RegraOrcamentaria("Regra", Guid.NewGuid(), Guid.Empty, 100m, PeriodoOrcamentario.Mensal, T0));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_Reject_Empty_Nome(string nome) =>
        Assert.Throws<ArgumentException>(() => new RegraOrcamentaria(nome, Guid.NewGuid(), Guid.NewGuid(), 100m, PeriodoOrcamentario.Mensal, T0));

    [Fact]
    public void Inativar_And_Ativar_Should_Toggle_Status()
    {
        var regra = new RegraOrcamentaria("Regra", Guid.NewGuid(), Guid.NewGuid(), 100m, PeriodoOrcamentario.Anual, T0);

        regra.Inativar(T0.AddHours(1));
        Assert.False(regra.Ativo);

        regra.Ativar(T0.AddHours(2));
        Assert.True(regra.Ativo);
    }

    [Fact]
    public void Editar_Should_Update_Fields()
    {
        var regra = new RegraOrcamentaria("Regra", Guid.NewGuid(), Guid.NewGuid(), 100m, PeriodoOrcamentario.Mensal, T0);
        var novoCentroCusto = Guid.NewGuid();

        regra.Editar("Regra Nova", novoCentroCusto, 2000m, PeriodoOrcamentario.Trimestral, T0.AddDays(1));

        Assert.Equal("Regra Nova", regra.Nome);
        Assert.Equal(novoCentroCusto, regra.CentroCustoMetadadoId);
        Assert.Equal(2000m, regra.ValorLimite);
        Assert.Equal(PeriodoOrcamentario.Trimestral, regra.Periodo);
        Assert.Equal(T0.AddDays(1), regra.AtualizadoEm);
    }
}
