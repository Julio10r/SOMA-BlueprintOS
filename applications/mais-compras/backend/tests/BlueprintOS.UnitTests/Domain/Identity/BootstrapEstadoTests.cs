using BlueprintOS.Domain.Identity;

namespace BlueprintOS.UnitTests.Domain.Identity;

/// <summary>Cobre o item 1 do plano de testes da Work Order O1.4.3 (seção 18) — "Bootstrap disponível
/// inicialmente (Concluido=false por seed)" — na perspectiva de domínio: a linha criada por
/// <see cref="BootstrapEstado.CriarInicial"/> (usada pela seed migration) sempre nasce com
/// <c>Concluido = false</c> e a chave fixa conhecida.</summary>
public sealed class BootstrapEstadoTests
{
    [Fact]
    public void CriarInicial_Should_Start_With_Concluido_False()
    {
        var estado = BootstrapEstado.CriarInicial();

        Assert.False(estado.Concluido);
        Assert.Null(estado.ConcluidoEm);
        Assert.Null(estado.UsuarioAdministradorSeniorId);
    }

    [Fact]
    public void CriarInicial_Should_Always_Use_Fixed_Known_Id()
    {
        var primeira = BootstrapEstado.CriarInicial();
        var segunda = BootstrapEstado.CriarInicial();

        Assert.Equal(BootstrapEstado.IdFixo, primeira.Id);
        Assert.Equal(primeira.Id, segunda.Id);
    }
}
