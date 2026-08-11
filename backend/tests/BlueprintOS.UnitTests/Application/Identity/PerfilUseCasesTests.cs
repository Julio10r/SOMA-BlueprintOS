using BlueprintOS.Application.Identity;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Domain.Identity;

namespace BlueprintOS.UnitTests.Application.Identity;

/// <summary>O1.5 — casos de uso da Gestão de Perfis. Cobre: criação com permissões, unicidade de nome por
/// Unidade de Negócio, rejeição de código de permissão fora do catálogo (nunca confiar no cliente),
/// substituição integral do conjunto de permissões, ativação/inativação, escopo por Unidade de Negócio
/// (isolamento entre BUs) e a invariante que impede o auto-bloqueio administrativo.</summary>
public sealed class PerfilUseCasesTests
{
    private static readonly Guid Bu = Guid.NewGuid();
    private static readonly Guid OutraBu = Guid.NewGuid();

    private sealed record Cenario(FakePerfilRepository Perfis, FakePermissaoRepository Permissoes);

    private static Cenario Arrange()
    {
        var permissoes = new FakePermissaoRepository();
        return new Cenario(new FakePerfilRepository { Permissoes = permissoes }, permissoes);
    }

    /// <summary>Semeia um Perfil administrativo ativo para que os testes que NÃO estão exercitando a
    /// invariante de auto-bloqueio não sejam bloqueados por ela.</summary>
    private static Perfil SemearAdministrador(Cenario c, Guid bu)
    {
        var perfil = new Perfil("Administrador", "Perfil administrativo.", bu, DateTimeOffset.UtcNow);
        c.Perfis.All.Add(perfil);
        c.Perfis.Vinculos.Add(new PerfilPermissao(perfil.Id, c.Permissoes.IdDe(PermissaoCatalogo.PerfilGerenciar)));
        // Usuário vinculado: a invariante anti-auto-bloqueio só considera salvaguarda um Perfil
        // administrativo que de fato tenha alguém vinculado.
        c.Perfis.UsuariosPerfis.Add(new UsuarioPerfil(Guid.NewGuid(), perfil.Id));
        return perfil;
    }

    /// <summary>Ator com o catálogo completo — representa o Administrador Sênior, que é quem opera a
    /// Gestão de Perfis hoje. Os testes de não-escalonamento usam atores restritos explicitamente.</summary>
    private static readonly IReadOnlyList<string> AtorOnipotente = PermissaoCatalogo.Codigos.ToArray();

    private static CriarPerfilUseCase Criar(Cenario c) => new(c.Perfis, c.Permissoes, TimeProvider.System);
    private static AtualizarPerfilUseCase Atualizar(Cenario c) => new(c.Perfis, c.Permissoes, TimeProvider.System);
    private static AlterarStatusPerfilUseCase AlterarStatus(Cenario c) => new(c.Perfis, TimeProvider.System);

    [Fact]
    public async Task Criar_Should_Persist_Perfil_With_Requested_Permissions()
    {
        var c = Arrange();

        var resultado = await Criar(c).ExecuteAsync(
            new PerfilInput("Analista", "Cria e aprova pedidos.", [PermissaoCatalogo.PedidoCriar, PermissaoCatalogo.PedidoAprovar]),
            Bu, AtorOnipotente, CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.Equal("Analista", resultado.Valor!.Nome);
        Assert.Equal(Bu, resultado.Valor.UnidadeNegocioId);
        Assert.True(resultado.Valor.Ativo);
        Assert.Equal([PermissaoCatalogo.PedidoAprovar, PermissaoCatalogo.PedidoCriar], resultado.Valor.Permissoes);
        Assert.Equal(0, resultado.Valor.UsuariosVinculados);
    }

    /// <summary>Um Perfil pode legitimamente não ter nenhuma permissão (ex.: Perfil recém-criado, ainda a
    /// configurar). Isso não é erro — mas também não concede nada.</summary>
    [Fact]
    public async Task Criar_Should_Allow_Perfil_Without_Any_Permission()
    {
        var c = Arrange();

        var resultado = await Criar(c).ExecuteAsync(new PerfilInput("Auditoria", "Sem acesso ainda.", []), Bu, AtorOnipotente, CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.Empty(resultado.Valor!.Permissoes);
    }

    [Theory]
    [InlineData("")]
    [InlineData("    ")]
    public async Task Criar_Should_Reject_Empty_Nome(string nome)
    {
        var c = Arrange();

        var resultado = await Criar(c).ExecuteAsync(new PerfilInput(nome, "x", []), Bu, AtorOnipotente, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.NomeObrigatorio, resultado.Falha);
        Assert.Empty(c.Perfis.All);
    }

    [Fact]
    public async Task Criar_Should_Reject_Duplicated_Nome_In_Same_UnidadeNegocio()
    {
        var c = Arrange();
        await Criar(c).ExecuteAsync(new PerfilInput("Analista", "Primeiro.", []), Bu, AtorOnipotente, CancellationToken.None);

        var resultado = await Criar(c).ExecuteAsync(new PerfilInput("Analista", "Segundo.", []), Bu, AtorOnipotente, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.NomeDuplicado, resultado.Falha);
        Assert.Single(c.Perfis.All);
    }

    /// <summary>O índice único é por (UnidadeNegocioId, Nome): o mesmo nome em outra Unidade de Negócio é
    /// legítimo e não deve ser bloqueado.</summary>
    [Fact]
    public async Task Criar_Should_Allow_Same_Nome_In_Different_UnidadeNegocio()
    {
        var c = Arrange();
        await Criar(c).ExecuteAsync(new PerfilInput("Analista", "BU A.", []), Bu, AtorOnipotente, CancellationToken.None);

        var resultado = await Criar(c).ExecuteAsync(new PerfilInput("Analista", "BU B.", []), OutraBu, AtorOnipotente, CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.Equal(2, c.Perfis.All.Count);
    }

    /// <summary>Segurança: um código de permissão inventado pelo cliente é rejeitado, e nenhum Perfil é
    /// criado parcialmente. Ignorar silenciosamente criaria um Perfil com menos acesso do que o operador
    /// acredita ter concedido.</summary>
    [Fact]
    public async Task Criar_Should_Reject_Unknown_Permission_Code_And_Persist_Nothing()
    {
        var c = Arrange();

        var resultado = await Criar(c).ExecuteAsync(
            new PerfilInput("Invasor", "x", [PermissaoCatalogo.PedidoCriar, "Sistema.Root"]), Bu, AtorOnipotente, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.PermissaoDesconhecida, resultado.Falha);
        Assert.Contains("Sistema.Root", resultado.Mensagem);
        Assert.Empty(c.Perfis.All);
        Assert.Empty(c.Perfis.Vinculos);
    }

    [Fact]
    public async Task Criar_Should_Normalize_Permission_Code_Case_And_Deduplicate()
    {
        var c = Arrange();

        var resultado = await Criar(c).ExecuteAsync(
            new PerfilInput("Analista", "x", ["pedido.criar", "PEDIDO.CRIAR", PermissaoCatalogo.PedidoCriar]),
            Bu, AtorOnipotente, CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.Equal([PermissaoCatalogo.PedidoCriar], resultado.Valor!.Permissoes);
    }

    [Fact]
    public async Task Atualizar_Should_Replace_Permission_Set_Removing_The_Absent_Ones()
    {
        var c = Arrange();
        SemearAdministrador(c, Bu);
        var criado = await Criar(c).ExecuteAsync(
            new PerfilInput("Analista", "x", [PermissaoCatalogo.PedidoCriar, PermissaoCatalogo.PedidoAprovar, PermissaoCatalogo.PedidoCancelar]),
            Bu, AtorOnipotente, CancellationToken.None);

        var resultado = await Atualizar(c).ExecuteAsync(
            criado.Valor!.Id, new PerfilInput("Analista Jr", "Somente cria.", [PermissaoCatalogo.PedidoCriar]),
            Bu, AtorOnipotente, CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.Equal("Analista Jr", resultado.Valor!.Nome);
        Assert.Equal([PermissaoCatalogo.PedidoCriar], resultado.Valor.Permissoes);
    }

    /// <summary>Isolamento entre Unidades de Negócio: um Id válido de OUTRA Unidade de Negócio é tratado
    /// como inexistente — não vaza a existência do recurso e não permite alterá-lo.</summary>
    [Fact]
    public async Task Atualizar_Should_Not_Reach_Perfil_Of_Another_UnidadeNegocio()
    {
        var c = Arrange();
        var alheio = new Perfil("Alheio", "De outra BU.", OutraBu, DateTimeOffset.UtcNow);
        c.Perfis.All.Add(alheio);

        var resultado = await Atualizar(c).ExecuteAsync(
            alheio.Id, new PerfilInput("Sequestrado", "x", [PermissaoCatalogo.SistemaGerenciar]), Bu, AtorOnipotente, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.PerfilNaoEncontrado, resultado.Falha);
        Assert.Equal("Alheio", alheio.Nome);
    }

    [Fact]
    public async Task Obter_Should_Not_Return_Perfil_Of_Another_UnidadeNegocio()
    {
        var c = Arrange();
        var alheio = new Perfil("Alheio", "De outra BU.", OutraBu, DateTimeOffset.UtcNow);
        c.Perfis.All.Add(alheio);

        Assert.Null(await new ObterPerfilUseCase(c.Perfis).ExecuteAsync(alheio.Id, Bu, CancellationToken.None));
        Assert.NotNull(await new ObterPerfilUseCase(c.Perfis).ExecuteAsync(alheio.Id, OutraBu, CancellationToken.None));
    }

    [Fact]
    public async Task Listar_Should_Only_Return_Perfis_Of_The_Requested_UnidadeNegocio()
    {
        var c = Arrange();
        c.Perfis.All.Add(new Perfil("Da BU", "x", Bu, DateTimeOffset.UtcNow));
        c.Perfis.All.Add(new Perfil("De outra BU", "x", OutraBu, DateTimeOffset.UtcNow));

        var lista = await new ListarPerfisUseCase(c.Perfis).ExecuteAsync(Bu, CancellationToken.None);

        Assert.Single(lista);
        Assert.Equal("Da BU", lista[0].Nome);
    }

    [Fact]
    public async Task Listar_Should_Count_Linked_Users_Per_Perfil()
    {
        var c = Arrange();
        var perfil = new Perfil("Analista", "x", Bu, DateTimeOffset.UtcNow);
        c.Perfis.All.Add(perfil);
        c.Perfis.UsuariosPerfis.Add(new UsuarioPerfil(Guid.NewGuid(), perfil.Id));
        c.Perfis.UsuariosPerfis.Add(new UsuarioPerfil(Guid.NewGuid(), perfil.Id));

        var lista = await new ListarPerfisUseCase(c.Perfis).ExecuteAsync(Bu, CancellationToken.None);

        Assert.Equal(2, lista[0].UsuariosVinculados);
    }

    [Fact]
    public async Task AlterarStatus_Should_Inactivate_And_Reactivate()
    {
        var c = Arrange();
        SemearAdministrador(c, Bu);
        var criado = await Criar(c).ExecuteAsync(new PerfilInput("Analista", "x", []), Bu, AtorOnipotente, CancellationToken.None);

        var inativado = await AlterarStatus(c).ExecuteAsync(criado.Valor!.Id, false, Bu, CancellationToken.None);
        Assert.True(inativado.Sucesso);
        Assert.False(inativado.Valor!.Ativo);

        var reativado = await AlterarStatus(c).ExecuteAsync(criado.Valor.Id, true, Bu, CancellationToken.None);
        Assert.True(reativado.Sucesso);
        Assert.True(reativado.Valor!.Ativo);
    }

    [Fact]
    public async Task AlterarStatus_Should_Return_NotFound_For_Perfil_Of_Another_UnidadeNegocio()
    {
        var c = Arrange();
        var alheio = new Perfil("Alheio", "x", OutraBu, DateTimeOffset.UtcNow);
        c.Perfis.All.Add(alheio);

        var resultado = await AlterarStatus(c).ExecuteAsync(alheio.Id, false, Bu, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.PerfilNaoEncontrado, resultado.Falha);
        Assert.True(alheio.Ativo);
    }

    // ---- Invariante de auto-bloqueio administrativo ----

    [Fact]
    public async Task Should_Refuse_To_Inactivate_The_Last_Perfil_With_PerfilGerenciar()
    {
        var c = Arrange();
        var administrador = SemearAdministrador(c, Bu);

        var resultado = await AlterarStatus(c).ExecuteAsync(administrador.Id, false, Bu, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.UltimoPerfilAdministrativo, resultado.Falha);
        Assert.True(administrador.Ativo);
    }

    [Fact]
    public async Task Should_Refuse_To_Remove_PerfilGerenciar_From_The_Last_Administrative_Perfil()
    {
        var c = Arrange();
        var administrador = SemearAdministrador(c, Bu);

        var resultado = await Atualizar(c).ExecuteAsync(
            administrador.Id, new PerfilInput("Administrador", "x", [PermissaoCatalogo.PedidoCriar]), Bu, AtorOnipotente, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.UltimoPerfilAdministrativo, resultado.Falha);
    }

    /// <summary>Com um segundo Perfil administrativo ativo, a operação é permitida — a invariante protege o
    /// acesso administrativo, não congela um Perfil específico.</summary>
    [Fact]
    public async Task Should_Allow_Inactivating_An_Administrative_Perfil_When_Another_One_Remains()
    {
        var c = Arrange();
        var primeiro = SemearAdministrador(c, Bu);
        var segundo = await Criar(c).ExecuteAsync(
            new PerfilInput("Administrador 2", "x", [PermissaoCatalogo.PerfilGerenciar]), Bu, AtorOnipotente, CancellationToken.None);
        Assert.True(segundo.Sucesso);
        // Vínculo de usuário obrigatório para que o segundo Perfil conte como salvaguarda real.
        c.Perfis.UsuariosPerfis.Add(new UsuarioPerfil(Guid.NewGuid(), segundo.Valor!.Id));

        var resultado = await AlterarStatus(c).ExecuteAsync(primeiro.Id, false, Bu, CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.False(primeiro.Ativo);
    }

    /// <summary>A invariante é por Unidade de Negócio: um Perfil administrativo em OUTRA BU não conta como
    /// salvaguarda para esta.</summary>
    [Fact]
    public async Task Invariant_Should_Not_Be_Satisfied_By_An_Administrative_Perfil_Of_Another_UnidadeNegocio()
    {
        var c = Arrange();
        SemearAdministrador(c, OutraBu);
        var daBu = SemearAdministrador(c, Bu);

        var resultado = await AlterarStatus(c).ExecuteAsync(daBu.Id, false, Bu, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.UltimoPerfilAdministrativo, resultado.Falha);
    }


    // ---- Regra de não-escalonamento de privilégio (achado HIGH da Security Validation independente) ----

    /// <summary>Cenário exato do achado: um "administrador de Perfis" delegado, cuja única permissão é
    /// `Perfil.Gerenciar`, edita o próprio Perfil tentando anexar todo o catálogo. Como as permissões
    /// efetivas são reresolvidas a cada requisição, se isso passasse ele já teria acesso total na chamada
    /// seguinte, sem novo login.</summary>
    [Fact]
    public async Task Atualizar_Should_Refuse_To_Grant_Permissions_The_Actor_Does_Not_Have()
    {
        var c = Arrange();
        var delegado = SemearAdministrador(c, Bu);
        IReadOnlyList<string> atorRestrito = [PermissaoCatalogo.PerfilGerenciar];

        var resultado = await Atualizar(c).ExecuteAsync(
            delegado.Id,
            new PerfilInput("Administrador", "x", [PermissaoCatalogo.PerfilGerenciar, PermissaoCatalogo.SistemaGerenciar]),
            Bu, atorRestrito, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.EscalonamentoDePrivilegio, resultado.Falha);
        Assert.Contains(PermissaoCatalogo.SistemaGerenciar, resultado.Mensagem);
    }

    [Fact]
    public async Task Criar_Should_Refuse_To_Grant_Permissions_The_Actor_Does_Not_Have()
    {
        var c = Arrange();
        IReadOnlyList<string> atorRestrito = [PermissaoCatalogo.PerfilGerenciar, PermissaoCatalogo.PedidoCriar];

        var resultado = await Criar(c).ExecuteAsync(
            new PerfilInput("Superperfil", "x", [PermissaoCatalogo.PedidoCriar, PermissaoCatalogo.UsuarioGerenciar]),
            Bu, atorRestrito, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.EscalonamentoDePrivilegio, resultado.Falha);
        Assert.Empty(c.Perfis.All);
    }

    /// <summary>O ator pode conceder exatamente o que possui — delegação legítima, não escalonamento.</summary>
    [Fact]
    public async Task Criar_Should_Allow_Granting_A_Subset_Of_The_Actor_Permissions()
    {
        var c = Arrange();
        IReadOnlyList<string> ator = [PermissaoCatalogo.PerfilGerenciar, PermissaoCatalogo.PedidoCriar, PermissaoCatalogo.PedidoAprovar];

        var resultado = await Criar(c).ExecuteAsync(
            new PerfilInput("Analista Jr", "x", [PermissaoCatalogo.PedidoCriar]), Bu, ator, CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.Equal([PermissaoCatalogo.PedidoCriar], resultado.Valor!.Permissoes);
    }

    /// <summary>A comparação é case-insensitive, para não virar um bypass acidental nem um falso positivo.</summary>
    [Fact]
    public async Task NonEscalation_Check_Should_Be_Case_Insensitive()
    {
        var c = Arrange();
        IReadOnlyList<string> ator = ["perfil.gerenciar", "PEDIDO.CRIAR"];

        var resultado = await Criar(c).ExecuteAsync(
            new PerfilInput("Analista Jr", "x", [PermissaoCatalogo.PedidoCriar]), Bu, ator, CancellationToken.None);

        Assert.True(resultado.Sucesso);
    }

    /// <summary>Ator sem nenhuma permissão não concede nada — mesmo que a policy o tivesse deixado entrar
    /// por outro caminho, a camada de aplicação falha fechado.</summary>
    [Fact]
    public async Task Criar_With_Empty_Actor_Permissions_Should_Only_Allow_An_Empty_Permission_Set()
    {
        var c = Arrange();

        var comPermissao = await Criar(c).ExecuteAsync(
            new PerfilInput("Tentativa", "x", [PermissaoCatalogo.PedidoCriar]), Bu, [], CancellationToken.None);
        Assert.False(comPermissao.Sucesso);
        Assert.Equal(RbacFalha.EscalonamentoDePrivilegio, comPermissao.Falha);

        var semPermissao = await Criar(c).ExecuteAsync(
            new PerfilInput("Vazio", "x", []), Bu, [], CancellationToken.None);
        Assert.True(semPermissao.Sucesso);
    }

    // ---- Invariante anti-auto-bloqueio: exige usuário vinculado (achado MEDIUM) ----

    /// <summary>Cenário exato do achado: criar um Perfil administrativo SEM usuários e usá-lo como "álibi"
    /// para remover a permissão do Perfil realmente em uso. A invariante deve continuar bloqueando, porque
    /// um Perfil sem ninguém vinculado não preserva acesso administrativo a nenhuma pessoa.</summary>
    [Fact]
    public async Task An_Administrative_Perfil_With_Zero_Users_Should_Not_Satisfy_The_Invariant()
    {
        var c = Arrange();
        var emUso = SemearAdministrador(c, Bu);
        var alibi = await Criar(c).ExecuteAsync(
            new PerfilInput("Temp", "Sem usuários.", [PermissaoCatalogo.PerfilGerenciar]), Bu, AtorOnipotente, CancellationToken.None);
        Assert.True(alibi.Sucesso);
        Assert.Equal(0, alibi.Valor!.UsuariosVinculados);

        var resultado = await AlterarStatus(c).ExecuteAsync(emUso.Id, false, Bu, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.UltimoPerfilAdministrativo, resultado.Falha);
        Assert.True(emUso.Ativo);
    }

    [Fact]
    public async Task Invariant_Should_Pass_When_The_Second_Administrative_Perfil_Has_A_Linked_User()
    {
        var c = Arrange();
        var primeiro = SemearAdministrador(c, Bu);
        var segundo = SemearAdministradorComNome(c, Bu, "Administrador 2");

        var resultado = await AlterarStatus(c).ExecuteAsync(primeiro.Id, false, Bu, CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.False(primeiro.Ativo);
        Assert.True(segundo.Ativo);
    }

    private static Perfil SemearAdministradorComNome(Cenario c, Guid bu, string nome)
    {
        var perfil = new Perfil(nome, "Perfil administrativo.", bu, DateTimeOffset.UtcNow);
        c.Perfis.All.Add(perfil);
        c.Perfis.Vinculos.Add(new PerfilPermissao(perfil.Id, c.Permissoes.IdDe(PermissaoCatalogo.PerfilGerenciar)));
        c.Perfis.UsuariosPerfis.Add(new UsuarioPerfil(Guid.NewGuid(), perfil.Id));
        return perfil;
    }

    // ---- Catálogo ----


    [Fact]
    public async Task ListarCatalogoPermissoes_Should_Return_The_Persisted_Catalog_With_Presentation_Metadata()
    {
        var c = Arrange();

        var catalogo = await new ListarCatalogoPermissoesUseCase(c.Permissoes).ExecuteAsync(CancellationToken.None);

        Assert.Equal(PermissaoCatalogo.Todas.Count, catalogo.Count);
        var gerenciarPerfil = catalogo.Single(x => x.Codigo == PermissaoCatalogo.PerfilGerenciar);
        Assert.Equal("Perfil", gerenciarPerfil.Recurso);
        Assert.Equal("Gerenciar", gerenciarPerfil.Acao);
        Assert.False(string.IsNullOrWhiteSpace(gerenciarPerfil.Descricao));
    }

    /// <summary>Se o banco tiver um código que a aplicação não conhece, ele não é oferecido à interface —
    /// a UI nunca apresenta uma permissão que nenhuma policy sabe avaliar.</summary>
    [Fact]
    public async Task ListarCatalogoPermissoes_Should_Omit_Persisted_Codes_Absent_From_The_Code_Catalog()
    {
        var c = Arrange();
        c.Permissoes.All.Add(new Permissao("Legado.Desconhecido", "Resíduo de outra versão."));

        var catalogo = await new ListarCatalogoPermissoesUseCase(c.Permissoes).ExecuteAsync(CancellationToken.None);

        Assert.DoesNotContain(catalogo, x => x.Codigo == "Legado.Desconhecido");
        Assert.Equal(PermissaoCatalogo.Todas.Count, catalogo.Count);
    }

    /// <summary>E o inverso: um código do catálogo em código, ausente do banco (seed não aplicado), também
    /// não é oferecido — a interface reflete o que de fato pode ser concedido.</summary>
    [Fact]
    public async Task ListarCatalogoPermissoes_Should_Omit_Code_Catalog_Entries_Absent_From_Database()
    {
        var c = Arrange();
        c.Permissoes.All.RemoveAll(x => x.Codigo == PermissaoCatalogo.SistemaGerenciar);

        var catalogo = await new ListarCatalogoPermissoesUseCase(c.Permissoes).ExecuteAsync(CancellationToken.None);

        Assert.DoesNotContain(catalogo, x => x.Codigo == PermissaoCatalogo.SistemaGerenciar);
    }
}
