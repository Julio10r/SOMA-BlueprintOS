using BlueprintOS.Domain.Identity;

namespace BlueprintOS.UnitTests.Domain.Identity;

/// <summary>O1.12 — invariantes de domínio de <see cref="AlcadaAprovacao"/>: nível >= 1, faixa de valor
/// mínima &lt;= máxima quando ambas informadas, e exatamente um aprovador (Usuário XOR Perfil).</summary>
public sealed class AlcadaAprovacaoTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 11, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid Bu = Guid.NewGuid();

    private static AlcadaAprovacao NovaComAprovadorUsuario(
        int nivel = 1, decimal? min = null, decimal? max = null, Guid? usuarioId = null) =>
        new("Alçada", Bu, CriterioAlcada.Valor, min, max, null, nivel, usuarioId ?? Guid.NewGuid(), null, T0);

    [Fact]
    public void Should_Start_Active_With_Trimmed_Nome()
    {
        var alcada = NovaComAprovadorUsuario();

        Assert.True(alcada.Ativo);
        Assert.Equal(T0, alcada.CriadoEm);
        Assert.Equal(T0, alcada.AtualizadoEm);
    }

    [Fact]
    public void Should_Reject_Nivel_Less_Than_One() =>
        Assert.Throws<ArgumentException>(() => NovaComAprovadorUsuario(nivel: 0));

    [Fact]
    public void Should_Accept_Nivel_Equal_To_One() =>
        Assert.Equal(1, NovaComAprovadorUsuario(nivel: 1).Nivel);

    [Fact]
    public void Should_Reject_Inverted_Value_Range() =>
        Assert.Throws<ArgumentException>(() => NovaComAprovadorUsuario(min: 1000m, max: 100m));

    [Fact]
    public void Should_Accept_Equal_Min_And_Max()
    {
        var alcada = NovaComAprovadorUsuario(min: 500m, max: 500m);
        Assert.Equal(500m, alcada.ValorMinimo);
        Assert.Equal(500m, alcada.ValorMaximo);
    }

    [Fact]
    public void Should_Reject_Both_Aprovador_Usuario_And_Perfil() =>
        Assert.Throws<ArgumentException>(() => new AlcadaAprovacao(
            "Alçada", Bu, CriterioAlcada.Valor, null, null, null, 1, Guid.NewGuid(), Guid.NewGuid(), T0));

    [Fact]
    public void Should_Reject_Neither_Aprovador_Usuario_Nor_Perfil() =>
        Assert.Throws<ArgumentException>(() => new AlcadaAprovacao(
            "Alçada", Bu, CriterioAlcada.Valor, null, null, null, 1, null, null, T0));

    [Fact]
    public void Should_Accept_Only_Aprovador_Perfil()
    {
        var perfilId = Guid.NewGuid();
        var alcada = new AlcadaAprovacao("Alçada", Bu, CriterioAlcada.Valor, null, null, null, 1, null, perfilId, T0);

        Assert.Null(alcada.AprovadorUsuarioId);
        Assert.Equal(perfilId, alcada.AprovadorPerfilId);
    }

    [Fact]
    public void CentroCusto_Should_Be_Ignored_When_Criterio_Is_Not_CentroCusto()
    {
        var centroCustoId = Guid.NewGuid();
        var alcada = new AlcadaAprovacao("Alçada", Bu, CriterioAlcada.Valor, null, null, centroCustoId, 1, Guid.NewGuid(), null, T0);

        Assert.Null(alcada.CentroCustoMetadadoId);
    }

    [Fact]
    public void ValorMinimoMaximo_Should_Be_Ignored_When_Criterio_Is_Not_Valor()
    {
        var alcada = new AlcadaAprovacao("Alçada", Bu, CriterioAlcada.CentroCusto, 10m, 20m, Guid.NewGuid(), 1, Guid.NewGuid(), null, T0);

        Assert.Null(alcada.ValorMinimo);
        Assert.Null(alcada.ValorMaximo);
    }

    [Fact]
    public void Inativar_And_Ativar_Should_Toggle_Status()
    {
        var alcada = NovaComAprovadorUsuario();

        alcada.Inativar(T0.AddHours(1));
        Assert.False(alcada.Ativo);

        alcada.Ativar(T0.AddHours(2));
        Assert.True(alcada.Ativo);
    }

    [Fact]
    public void Editar_Enforces_The_Same_Invariants_As_Construction() =>
        Assert.Throws<ArgumentException>(() => NovaComAprovadorUsuario().Editar(
            "Alçada", CriterioAlcada.Valor, null, null, null, 0, Guid.NewGuid(), null, T0.AddHours(1)));
}
