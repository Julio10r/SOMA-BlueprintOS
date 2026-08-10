using BlueprintOS.Domain.Identity;

namespace BlueprintOS.UnitTests.Domain.Identity;

public sealed class BootstrapSessaoTests
{
    [Fact]
    public void EstaValidaEm_Should_Be_True_Right_After_Creation()
    {
        var agora = DateTimeOffset.UtcNow;
        var sessao = new BootstrapSessao("candidato@somagrupo.com.br", "hash-qualquer", agora);

        Assert.True(sessao.EstaValidaEm(agora));
    }

    [Fact]
    public void EstaValidaEm_Should_Be_False_After_Absolute_Expiration_Even_Without_Activity()
    {
        var agora = DateTimeOffset.UtcNow;
        var sessao = new BootstrapSessao("candidato@somagrupo.com.br", "hash-qualquer", agora);

        // Vida útil absoluta de 15 minutos, sem renovação por atividade (ao contrário da sessão normal) —
        // Work Order O1.4.3, seção 8.
        Assert.False(sessao.EstaValidaEm(agora.Add(BootstrapSessao.Validade).AddSeconds(1)));
    }

    [Fact]
    public void MarcarUsada_Should_Invalidate_Session_Even_Within_Validity_Window()
    {
        var agora = DateTimeOffset.UtcNow;
        var sessao = new BootstrapSessao("candidato@somagrupo.com.br", "hash-qualquer", agora);

        sessao.MarcarUsada(agora);

        Assert.False(sessao.EstaValidaEm(agora));
    }

    [Fact]
    public void Revogar_Should_Invalidate_Session_Even_Within_Validity_Window()
    {
        var agora = DateTimeOffset.UtcNow;
        var sessao = new BootstrapSessao("candidato@somagrupo.com.br", "hash-qualquer", agora);

        sessao.Revogar(agora);

        Assert.False(sessao.EstaValidaEm(agora));
    }

    [Fact]
    public void MarcarUsada_Should_Be_Idempotent_Never_Overwriting_First_Timestamp()
    {
        var agora = DateTimeOffset.UtcNow;
        var sessao = new BootstrapSessao("candidato@somagrupo.com.br", "hash-qualquer", agora);

        sessao.MarcarUsada(agora);
        sessao.MarcarUsada(agora.AddMinutes(5));

        Assert.Equal(agora, sessao.UsadaEm);
    }
}
