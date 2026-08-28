using BlueprintOS.Domain.Identity;

namespace BlueprintOS.UnitTests.Domain.Identity;

public sealed class CodigoVerificacaoOtpTests
{
    /// <summary>Work Order O1.4.3 (seção 11) — adaptação para candidatos de Bootstrap sem Usuario
    /// existente: UsuarioId permanece nulo, EmailCandidato identifica o destinatário, reaproveitando 100%
    /// do hashing/tentativas/expiração/RowVersion desta mesma classe.</summary>
    [Fact]
    public void ParaCandidatoBootstrap_Should_Leave_UsuarioId_Null_And_Set_EmailCandidato()
    {
        var agora = DateTimeOffset.UtcNow;
        var codigo = CodigoVerificacaoOtp.ParaCandidatoBootstrap("candidato@somagrupo.com.br", "hash", "salt", agora);

        Assert.Null(codigo.UsuarioId);
        Assert.Equal("candidato@somagrupo.com.br", codigo.EmailCandidato);
        Assert.True(codigo.EstaValidoEm(agora));
    }

    [Fact]
    public void Constructor_For_Normal_Login_Should_Leave_EmailCandidato_Null()
    {
        var codigo = new CodigoVerificacaoOtp(Guid.NewGuid(), "hash", "salt", DateTimeOffset.UtcNow);

        Assert.Null(codigo.EmailCandidato);
        Assert.NotNull(codigo.UsuarioId);
    }

    [Fact]
    public void EstaValidoEm_Should_Be_True_Right_After_Creation()
    {
        var agora = DateTimeOffset.UtcNow;
        var codigo = new CodigoVerificacaoOtp(Guid.NewGuid(), "hash", "salt", agora);

        Assert.True(codigo.EstaValidoEm(agora));
    }

    [Fact]
    public void EstaValidoEm_Should_Be_False_After_Expiration()
    {
        var agora = DateTimeOffset.UtcNow;
        var codigo = new CodigoVerificacaoOtp(Guid.NewGuid(), "hash", "salt", agora);

        Assert.False(codigo.EstaValidoEm(agora + CodigoVerificacaoOtp.Validade + TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public void RegistrarTentativaFalha_Should_Expire_After_Max_Attempts()
    {
        var agora = DateTimeOffset.UtcNow;
        var codigo = new CodigoVerificacaoOtp(Guid.NewGuid(), "hash", "salt", agora);

        for (var i = 0; i < CodigoVerificacaoOtp.MaxTentativas; i++)
        {
            codigo.RegistrarTentativaFalha();
        }

        Assert.Equal(StatusCodigoVerificacaoOtp.Expirado, codigo.Status);
        Assert.False(codigo.EstaValidoEm(agora));
    }

    [Fact]
    public void Consumir_Should_Prevent_Reuse_Even_Within_Validity()
    {
        var agora = DateTimeOffset.UtcNow;
        var codigo = new CodigoVerificacaoOtp(Guid.NewGuid(), "hash", "salt", agora);

        codigo.Consumir();

        Assert.False(codigo.EstaValidoEm(agora));
        Assert.Equal(StatusCodigoVerificacaoOtp.Consumido, codigo.Status);
    }

    [Fact]
    public void InvalidarPorNovoCodigo_Should_Expire_Pending_Code_Only()
    {
        var agora = DateTimeOffset.UtcNow;
        var codigo = new CodigoVerificacaoOtp(Guid.NewGuid(), "hash", "salt", agora);
        codigo.InvalidarPorNovoCodigo();

        Assert.False(codigo.EstaValidoEm(agora));
        Assert.Equal(StatusCodigoVerificacaoOtp.Expirado, codigo.Status);
    }
}
