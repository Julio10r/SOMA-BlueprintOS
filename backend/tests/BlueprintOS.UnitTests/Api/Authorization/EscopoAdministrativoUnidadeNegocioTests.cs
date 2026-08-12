using BlueprintOS.Api.Authorization;
using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Domain.Identity;
using Xunit;

namespace BlueprintOS.UnitTests.Api.Authorization;

/// <summary>Gate Final da Onda 1 — cobre a decisão formal do Product Owner sobre escopo administrativo
/// (Produto × Negócio), independente de RBAC (permissão). Os seis cenários abaixo correspondem
/// exatamente aos exigidos pelo Gate.</summary>
public sealed class EscopoAdministrativoUnidadeNegocioTests
{
    private sealed class FakeCurrentIdentity(RequestIdentity identidade) : ICurrentIdentity
    {
        public RequestIdentity GetRequired() => identidade;
    }

    private static RequestIdentity Identidade(Guid unidadeNegocioId, EscopoAdministrativo escopo, params string[] permissoes) =>
        new(Guid.NewGuid(), "Buyer", unidadeNegocioId, permissoes, escopo);

    // Cenário 1 — Administrador BU A → recurso BU A, possui a permissão → PERMITIDO.
    [Fact]
    public void Autoriza_AdministradorDeBu_Na_Propria_BU_E_Permitido()
    {
        var buA = Guid.NewGuid();
        var identidade = Identidade(buA, EscopoAdministrativo.Negocio, PermissaoCatalogo.AlcadaGerenciar);

        Assert.True(EscopoAdministrativoUnidadeNegocio.Autoriza(identidade, buA));
    }

    // Cenário 2 — Administrador BU A → recurso BU B, mesma permissão → NEGADO, mesmo conhecendo o Id.
    [Fact]
    public void Autoriza_AdministradorDeBu_Em_Outra_BU_E_Negado_Mesmo_Com_Id_Valido()
    {
        var buA = Guid.NewGuid();
        var buB = Guid.NewGuid();
        var identidade = Identidade(buA, EscopoAdministrativo.Negocio, PermissaoCatalogo.AlcadaGerenciar);

        Assert.False(EscopoAdministrativoUnidadeNegocio.Autoriza(identidade, buB));
    }

    // Cenário 3 — Administrador Sênior → recurso BU A → PERMITIDO.
    [Fact]
    public void Autoriza_AdministradorSenior_Na_Propria_BU_E_Permitido()
    {
        var buA = Guid.NewGuid();
        var identidade = Identidade(buA, EscopoAdministrativo.Produto, PermissaoCatalogo.AlcadaGerenciar);

        Assert.True(EscopoAdministrativoUnidadeNegocio.Autoriza(identidade, buA));
    }

    // Cenário 4 — Administrador Sênior → recurso BU B (outra BU) → PERMITIDO (cross-BU legítimo).
    [Fact]
    public void Autoriza_AdministradorSenior_Em_Outra_BU_E_Permitido()
    {
        var buA = Guid.NewGuid();
        var buB = Guid.NewGuid();
        var identidade = Identidade(buA, EscopoAdministrativo.Produto, PermissaoCatalogo.AlcadaGerenciar);

        Assert.True(EscopoAdministrativoUnidadeNegocio.Autoriza(identidade, buB));
    }

    // Cenário 6 — manipular unidadeNegocioId no path/body/query nunca causa bypass: TryResolverUnidadeNegocio
    // (usado pelos controllers "BU da sessão" com override opcional) aplica a mesma regra de escopo.
    [Fact]
    public void TryResolverUnidadeNegocio_Sem_Override_Usa_A_Propria_BU_Da_Sessao()
    {
        var buA = Guid.NewGuid();
        var identidade = new FakeCurrentIdentity(Identidade(buA, EscopoAdministrativo.Negocio));

        var ok = EscopoAdministrativoUnidadeNegocio.TryResolverUnidadeNegocio(identidade, null, out var resolvido, out var falha);

        Assert.True(ok);
        Assert.Equal(buA, resolvido);
        Assert.Null(falha);
    }

    [Fact]
    public void TryResolverUnidadeNegocio_AdministradorDeBu_Tentando_Override_Para_Outra_BU_E_Negado()
    {
        var buA = Guid.NewGuid();
        var buB = Guid.NewGuid();
        var identidade = new FakeCurrentIdentity(Identidade(buA, EscopoAdministrativo.Negocio));

        var ok = EscopoAdministrativoUnidadeNegocio.TryResolverUnidadeNegocio(identidade, buB, out var resolvido, out var falha);

        Assert.False(ok);
        Assert.Equal(Guid.Empty, resolvido);
        Assert.NotNull(falha);
    }

    [Fact]
    public void TryResolverUnidadeNegocio_AdministradorSenior_Pode_Fazer_Override_Para_Outra_BU()
    {
        var buA = Guid.NewGuid();
        var buB = Guid.NewGuid();
        var identidade = new FakeCurrentIdentity(Identidade(buA, EscopoAdministrativo.Produto));

        var ok = EscopoAdministrativoUnidadeNegocio.TryResolverUnidadeNegocio(identidade, buB, out var resolvido, out var falha);

        Assert.True(ok);
        Assert.Equal(buB, resolvido);
        Assert.Null(falha);
    }

    // Cenário 5 (usuário sem UnidadeNegocioId resolvido — esquema de Development) → fail-closed, nunca
    // tratado como "sem restrição".
    [Fact]
    public void TryResolverUnidadeNegocio_Sem_UnidadeNegocioId_Resolvida_E_Fail_Closed()
    {
        var identidade = new FakeCurrentIdentity(new RequestIdentity(Guid.NewGuid(), "Buyer"));

        var ok = EscopoAdministrativoUnidadeNegocio.TryResolverUnidadeNegocio(identidade, null, out var resolvido, out var falha);

        Assert.False(ok);
        Assert.Equal(Guid.Empty, resolvido);
        Assert.NotNull(falha);
    }
}
