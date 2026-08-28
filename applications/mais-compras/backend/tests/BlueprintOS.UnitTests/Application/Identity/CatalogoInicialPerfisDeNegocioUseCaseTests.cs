using BlueprintOS.Application.Identity;
using BlueprintOS.Domain.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BlueprintOS.UnitTests.Application.Identity;

/// <summary>Gate Final da Onda 1 — entregável #9. Cobre o catálogo inicial de Perfis de negócio:
/// idempotência, matriz Perfil × Permissão e a garantia de que nenhuma permissão PRODUTO é concedida a um
/// Perfil de negócio.</summary>
public sealed class CatalogoInicialPerfisDeNegocioUseCaseTests
{
    private static CatalogoInicialPerfisDeNegocioUseCase CriarSut(FakePerfilRepository perfis) =>
        new(perfis, perfis.Permissoes, TimeProvider.System, NullLogger<CatalogoInicialPerfisDeNegocioUseCase>.Instance);

    [Fact]
    public async Task GarantirCatalogoAsync_Cria_Os_Quatro_Perfis_De_Negocio()
    {
        var perfis = new FakePerfilRepository();
        var sut = CriarSut(perfis);
        var unidadeNegocioId = Guid.NewGuid();

        await sut.GarantirCatalogoAsync(unidadeNegocioId, default);

        var nomes = perfis.All.Where(p => p.UnidadeNegocioId == unidadeNegocioId).Select(p => p.Nome).ToHashSet();
        Assert.Equal(
            new HashSet<string> { Perfil.AdministradorDeBu, Perfil.Comprador, Perfil.Aprovador, Perfil.Requisitante },
            nomes);
    }

    [Fact]
    public async Task GarantirCatalogoAsync_Nunca_Cria_Administrador_Senior()
    {
        var perfis = new FakePerfilRepository();
        var sut = CriarSut(perfis);
        var unidadeNegocioId = Guid.NewGuid();

        await sut.GarantirCatalogoAsync(unidadeNegocioId, default);

        Assert.DoesNotContain(perfis.All, p => p.Nome == Perfil.AdministradorSenior);
    }

    [Fact]
    public async Task GarantirCatalogoAsync_E_Idempotente_Nao_Duplica_Perfil_Nem_Remove_Vinculo_Manual()
    {
        var perfis = new FakePerfilRepository();
        var sut = CriarSut(perfis);
        var unidadeNegocioId = Guid.NewGuid();

        await sut.GarantirCatalogoAsync(unidadeNegocioId, default);

        // Simula uma concessão manual adicional feita por um administrador via Gestão de Perfis.
        var aprovador = perfis.All.Single(p => p.Nome == Perfil.Aprovador && p.UnidadeNegocioId == unidadeNegocioId);
        var permissaoExtra = perfis.Permissoes.IdDe(PermissaoCatalogo.FornecedorEditar);
        await perfis.VincularPermissoesAsync(aprovador.Id, [permissaoExtra], default);

        await sut.GarantirCatalogoAsync(unidadeNegocioId, default);

        Assert.Equal(4, perfis.All.Count(p => p.UnidadeNegocioId == unidadeNegocioId));
        Assert.Contains(perfis.Vinculos, v => v.PerfilId == aprovador.Id && v.PermissaoId == permissaoExtra);
    }

    [Fact]
    public async Task GarantirCatalogoAsync_AdministradorDeBu_Recebe_Somente_Permissoes_De_Negocio()
    {
        var perfis = new FakePerfilRepository();
        var sut = CriarSut(perfis);
        var unidadeNegocioId = Guid.NewGuid();

        await sut.GarantirCatalogoAsync(unidadeNegocioId, default);

        var administradorDeBu = perfis.All.Single(p => p.Nome == Perfil.AdministradorDeBu && p.UnidadeNegocioId == unidadeNegocioId);
        var codigos = perfis.Vinculos
            .Where(v => v.PerfilId == administradorDeBu.Id)
            .Select(v => perfis.Permissoes.All.Single(p => p.Id == v.PermissaoId).Codigo)
            .ToHashSet();

        Assert.Equal(
            new HashSet<string>
            {
                PermissaoCatalogo.UsuarioGerenciar, PermissaoCatalogo.PerfilGerenciar, PermissaoCatalogo.FilialGerenciar,
                PermissaoCatalogo.CentroCustoGerenciar, PermissaoCatalogo.UnidadeAlocacaoGerenciar,
                PermissaoCatalogo.WorkflowGerenciar, PermissaoCatalogo.AlcadaGerenciar, PermissaoCatalogo.OrcamentoGerenciar,
            },
            codigos);

        // Nunca PRODUTO — reservadas ao Administrador Sênior.
        Assert.DoesNotContain(PermissaoCatalogo.UnidadeNegocioGerenciar, codigos);
        Assert.DoesNotContain(PermissaoCatalogo.ConfiguracaoErpGerenciar, codigos);
        Assert.DoesNotContain(PermissaoCatalogo.SistemaGerenciar, codigos);
    }

    [Fact]
    public async Task GarantirCatalogoAsync_Requisitante_Nasce_Sem_Nenhuma_Permissao()
    {
        var perfis = new FakePerfilRepository();
        var sut = CriarSut(perfis);
        var unidadeNegocioId = Guid.NewGuid();

        await sut.GarantirCatalogoAsync(unidadeNegocioId, default);

        var requisitante = perfis.All.Single(p => p.Nome == Perfil.Requisitante && p.UnidadeNegocioId == unidadeNegocioId);
        Assert.DoesNotContain(perfis.Vinculos, v => v.PerfilId == requisitante.Id);
    }

    [Fact]
    public async Task GarantirCatalogoAsync_E_Multi_BU_Consciente_Nao_Mistura_Perfis_De_BUs_Diferentes()
    {
        var perfis = new FakePerfilRepository();
        var sut = CriarSut(perfis);
        var buA = Guid.NewGuid();
        var buB = Guid.NewGuid();

        await sut.GarantirCatalogoAsync(buA, default);
        await sut.GarantirCatalogoAsync(buB, default);

        Assert.Equal(4, perfis.All.Count(p => p.UnidadeNegocioId == buA));
        Assert.Equal(4, perfis.All.Count(p => p.UnidadeNegocioId == buB));
        Assert.Equal(8, perfis.All.Count);
    }
}
