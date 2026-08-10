using BlueprintOS.Domain.Identity;

namespace BlueprintOS.UnitTests.Domain.Identity;

public sealed class SessaoAutenticacaoTests
{
    [Fact]
    public void EstaAtivaEm_Should_Be_True_Right_After_Creation()
    {
        var agora = DateTimeOffset.UtcNow;
        var sessao = new SessaoAutenticacao(Guid.NewGuid(), Guid.NewGuid(), "hash", agora, TimeSpan.FromHours(12));

        Assert.True(sessao.EstaAtivaEm(agora, TimeSpan.FromMinutes(30)));
    }

    [Fact]
    public void EstaAtivaEm_Should_Be_False_After_Absolute_Expiration()
    {
        var agora = DateTimeOffset.UtcNow;
        var sessao = new SessaoAutenticacao(Guid.NewGuid(), Guid.NewGuid(), "hash", agora, TimeSpan.FromHours(12));

        Assert.False(sessao.EstaAtivaEm(agora + TimeSpan.FromHours(13), TimeSpan.FromMinutes(30)));
    }

    [Fact]
    public void EstaAtivaEm_Should_Be_False_After_Inactivity_Timeout()
    {
        var agora = DateTimeOffset.UtcNow;
        var sessao = new SessaoAutenticacao(Guid.NewGuid(), Guid.NewGuid(), "hash", agora, TimeSpan.FromHours(12));

        Assert.False(sessao.EstaAtivaEm(agora + TimeSpan.FromMinutes(31), TimeSpan.FromMinutes(30)));
    }

    [Fact]
    public void EstaAtivaEm_Should_Be_False_After_Revoked()
    {
        var agora = DateTimeOffset.UtcNow;
        var sessao = new SessaoAutenticacao(Guid.NewGuid(), Guid.NewGuid(), "hash", agora, TimeSpan.FromHours(12));

        sessao.Revogar(agora);

        Assert.False(sessao.EstaAtivaEm(agora, TimeSpan.FromMinutes(30)));
    }

    [Fact]
    public void RegistrarAtividade_Should_Extend_Inactivity_Window()
    {
        var agora = DateTimeOffset.UtcNow;
        var sessao = new SessaoAutenticacao(Guid.NewGuid(), Guid.NewGuid(), "hash", agora, TimeSpan.FromHours(12));

        sessao.RegistrarAtividade(agora + TimeSpan.FromMinutes(20));

        Assert.True(sessao.EstaAtivaEm(agora + TimeSpan.FromMinutes(45), TimeSpan.FromMinutes(30)));
    }
}
