using BlueprintOS.Domain.Identity;

namespace BlueprintOS.UnitTests.Domain.Identity;

/// <summary>O1.5 — o catálogo é a fonte central única de códigos de permissão (policies, seed de banco,
/// endpoints e API do frontend derivam dele). Estes testes protegem exatamente as propriedades das quais
/// esse papel depende.</summary>
public sealed class PermissaoCatalogoTests
{
    [Fact]
    public void Codigos_Should_Be_Unique()
    {
        var codigos = PermissaoCatalogo.Todas.Select(x => x.Codigo).ToArray();
        Assert.Equal(codigos.Length, codigos.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    /// <summary>Os Ids são a chave primária das linhas semeadas em `Permissoes`. Duplicidade quebraria o
    /// seed; alteração quebraria os vínculos `PerfisPermissoes` já persistidos.</summary>
    [Fact]
    public void Ids_Should_Be_Unique_And_Non_Empty()
    {
        var ids = PermissaoCatalogo.Todas.Select(x => x.Id).ToArray();
        Assert.Equal(ids.Length, ids.Distinct().Count());
        Assert.DoesNotContain(Guid.Empty, ids);
    }

    [Fact]
    public void Recurso_And_Acao_Should_Be_Derived_From_Codigo()
    {
        foreach (var definicao in PermissaoCatalogo.Todas)
        {
            Assert.Equal($"{definicao.Recurso}.{definicao.Acao}", definicao.Codigo);
            Assert.False(string.IsNullOrWhiteSpace(definicao.Recurso));
            Assert.False(string.IsNullOrWhiteSpace(definicao.Acao));
            Assert.False(string.IsNullOrWhiteSpace(definicao.Descricao));
        }
    }

    /// <summary>Os códigos abaixo estão escritos em `docs/product/ComprasFuncional.md` como as permissões
    /// que governam as telas de Administração. Este teste falha se o catálogo divergir da documentação
    /// funcional aprovada.</summary>
    [Theory]
    [InlineData("Perfil.Gerenciar")]
    [InlineData("Usuario.Gerenciar")]
    [InlineData("UnidadeNegocio.Gerenciar")]
    [InlineData("Filial.Gerenciar")]
    [InlineData("CentroCusto.Gerenciar")]
    [InlineData("UnidadeAlocacao.Gerenciar")]
    [InlineData("ConfiguracaoErp.Gerenciar")]
    [InlineData("Fornecedor.Aprovar")]
    [InlineData("Workflow.Gerenciar")]
    [InlineData("Alcada.Gerenciar")]
    [InlineData("Orcamento.Gerenciar")]
    public void Should_Contain_Permissions_Documented_In_ComprasFuncional(string codigo) =>
        Assert.True(PermissaoCatalogo.Existe(codigo));

    [Fact]
    public void Existe_Should_Be_Case_Insensitive_And_Trim()
    {
        Assert.True(PermissaoCatalogo.Existe("perfil.gerenciar"));
        Assert.True(PermissaoCatalogo.Existe("  PERFIL.GERENCIAR  "));
        Assert.Equal(PermissaoCatalogo.PerfilGerenciar, PermissaoCatalogo.Normalizar("perfil.gerenciar"));
    }

    /// <summary>Código desconhecido nunca é normalizado para "algo parecido" — retorna nulo para que o
    /// chamador rejeite, em vez de conceder acidentalmente uma permissão próxima.</summary>
    [Theory]
    [InlineData("Perfil.Excluir")]
    [InlineData("Perfil")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Qualquer.Coisa")]
    public void Unknown_Codes_Should_Not_Resolve(string codigo)
    {
        Assert.False(PermissaoCatalogo.Existe(codigo));
        Assert.Null(PermissaoCatalogo.Normalizar(codigo));
        Assert.Null(PermissaoCatalogo.Obter(codigo));
    }
}

/// <summary>Perfil ganhou comportamento real na O1.5 (edição, ativação/inativação, auditoria).</summary>
public sealed class PerfilTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 11, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Should_Start_Active_With_Trimmed_Fields_And_Timestamps()
    {
        var perfil = new Perfil("  Analista  ", "  Cria pedidos.  ", Guid.NewGuid(), T0);

        Assert.Equal("Analista", perfil.Nome);
        Assert.Equal("Cria pedidos.", perfil.Descricao);
        Assert.True(perfil.Ativo);
        Assert.Equal(T0, perfil.CriadoEm);
        Assert.Equal(T0, perfil.AtualizadoEm);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_Reject_Empty_Nome(string nome) =>
        Assert.Throws<ArgumentException>(() => new Perfil(nome, "x", Guid.NewGuid(), T0));

    [Fact]
    public void Inativar_Should_Flip_Status_And_Bump_AtualizadoEm()
    {
        var perfil = new Perfil("Auditoria", "Somente leitura.", Guid.NewGuid(), T0);

        perfil.Inativar(T0.AddHours(1));

        Assert.False(perfil.Ativo);
        Assert.Equal(T0.AddHours(1), perfil.AtualizadoEm);
        Assert.Equal(T0, perfil.CriadoEm);
    }

    /// <summary>Idempotência: reinativar não deve reescrever a data da alteração real anterior.</summary>
    [Fact]
    public void Inativar_Twice_Should_Not_Change_AtualizadoEm_Again()
    {
        var perfil = new Perfil("Auditoria", "Somente leitura.", Guid.NewGuid(), T0);
        perfil.Inativar(T0.AddHours(1));

        perfil.Inativar(T0.AddHours(9));

        Assert.Equal(T0.AddHours(1), perfil.AtualizadoEm);
    }

    [Fact]
    public void Ativar_Should_Restore_Status()
    {
        var perfil = new Perfil("Auditoria", "Somente leitura.", Guid.NewGuid(), T0);
        perfil.Inativar(T0.AddHours(1));

        perfil.Ativar(T0.AddHours(2));

        Assert.True(perfil.Ativo);
        Assert.Equal(T0.AddHours(2), perfil.AtualizadoEm);
    }

    [Fact]
    public void Atualizar_Should_Change_Nome_Descricao_And_AtualizadoEm()
    {
        var perfil = new Perfil("Analista", "Antiga.", Guid.NewGuid(), T0);

        perfil.Atualizar("Analista Jr", "Nova.", T0.AddDays(1));

        Assert.Equal("Analista Jr", perfil.Nome);
        Assert.Equal("Nova.", perfil.Descricao);
        Assert.Equal(T0.AddDays(1), perfil.AtualizadoEm);
    }
}
