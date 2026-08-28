using BlueprintOS.Domain.Identity;

namespace BlueprintOS.UnitTests.Domain.Identity;

/// <summary>O1.12 — invariantes de domínio de <see cref="RegraWorkflow"/>.</summary>
public sealed class RegraWorkflowTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 11, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Should_Start_Active_With_Trimmed_Fields()
    {
        var regra = new RegraWorkflow("  Aprovação Simples  ", Guid.NewGuid(), "  Solicitação  ", 1, T0);

        Assert.Equal("Aprovação Simples", regra.Nome);
        Assert.Equal("Solicitação", regra.TipoProcesso);
        Assert.Equal(1, regra.Ordem);
        Assert.True(regra.Ativo);
        Assert.Equal(T0, regra.CriadoEm);
        Assert.Equal(T0, regra.AtualizadoEm);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_Reject_Empty_Nome(string nome) =>
        Assert.Throws<ArgumentException>(() => new RegraWorkflow(nome, Guid.NewGuid(), "Solicitação", 1, T0));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_Reject_Empty_TipoProcesso(string tipoProcesso) =>
        Assert.Throws<ArgumentException>(() => new RegraWorkflow("Regra", Guid.NewGuid(), tipoProcesso, 1, T0));

    [Fact]
    public void Should_Reject_Negative_Ordem() =>
        Assert.Throws<ArgumentException>(() => new RegraWorkflow("Regra", Guid.NewGuid(), "Solicitação", -1, T0));

    [Fact]
    public void Inativar_Should_Flip_Status_And_Bump_AtualizadoEm()
    {
        var regra = new RegraWorkflow("Regra", Guid.NewGuid(), "Solicitação", 1, T0);

        regra.Inativar(T0.AddHours(1));

        Assert.False(regra.Ativo);
        Assert.Equal(T0.AddHours(1), regra.AtualizadoEm);
    }

    [Fact]
    public void Ativar_Should_Restore_Status()
    {
        var regra = new RegraWorkflow("Regra", Guid.NewGuid(), "Solicitação", 1, T0);
        regra.Inativar(T0.AddHours(1));

        regra.Ativar(T0.AddHours(2));

        Assert.True(regra.Ativo);
        Assert.Equal(T0.AddHours(2), regra.AtualizadoEm);
    }

    [Fact]
    public void Editar_Should_Change_Fields_And_AtualizadoEm()
    {
        var regra = new RegraWorkflow("Regra", Guid.NewGuid(), "Solicitação", 1, T0);

        regra.Editar("Regra Nova", "Cotação", 2, T0.AddDays(1));

        Assert.Equal("Regra Nova", regra.Nome);
        Assert.Equal("Cotação", regra.TipoProcesso);
        Assert.Equal(2, regra.Ordem);
        Assert.Equal(T0.AddDays(1), regra.AtualizadoEm);
    }
}
