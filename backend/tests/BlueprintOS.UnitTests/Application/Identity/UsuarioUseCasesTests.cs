using BlueprintOS.Application.Identity;
using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Domain.Identity;

namespace BlueprintOS.UnitTests.Application.Identity;

/// <summary>O1.6 — casos de uso da Gestão de Usuários. Cobre: criação/edição com vínculo de Perfis e
/// Centros de Custo, unicidade de e-mail, rejeição de Perfil fora da Unidade de Negócio da sessão,
/// não-escalonamento de privilégio no vínculo de Perfil, ativação/inativação e a regra do último
/// Administrador Sênior ativo (reaproveitando <see cref="AdministradorSeniorInvariantService"/>, O1.4.3.2).</summary>
public sealed class UsuarioUseCasesTests
{
    private static readonly Guid Bu = Guid.NewGuid();
    private static readonly Guid OutraBu = Guid.NewGuid();

    private sealed record Cenario(
        FakeUsuarioRepositoryCompleto Usuarios, FakePerfilRepository Perfis, FakePermissaoRepository Permissoes,
        FakeCentroCustoVinculoValidator CentrosCusto);

    /// <summary>Resolução da dívida O1.6-L2: mesma decisão do validador real
    /// (<c>CentroCustoVinculoValidator</c>) reduzida a memória — códigos em <see cref="Existentes"/> são
    /// aceitos (e ancorados à primeira Unidade de Negócio que os usar); qualquer outro código é rejeitado
    /// como inexistente no ERP; um código já ancorado a outra Unidade de Negócio é rejeitado como
    /// cross-BU.</summary>
    private sealed class FakeCentroCustoVinculoValidator : ICentroCustoVinculoValidator
    {
        public HashSet<string> Existentes { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, Guid> AncoradoEm { get; } = new(StringComparer.OrdinalIgnoreCase);

        public Task<RbacResultado<IReadOnlyList<string>>> ValidarEAncorarAsync(
            IReadOnlyList<string>? codigosErp, Guid unidadeNegocioId, CancellationToken ct)
        {
            var normalizados = (codigosErp ?? [])
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            foreach (var codigo in normalizados)
            {
                if (AncoradoEm.TryGetValue(codigo, out var bu))
                {
                    if (bu != unidadeNegocioId)
                    {
                        return Task.FromResult(RbacResultado<IReadOnlyList<string>>.Erro(
                            RbacFalha.CentroCustoInvalido, $"O Centro de Custo '{codigo}' pertence a outra Unidade de Negócio."));
                    }
                    continue;
                }

                if (!Existentes.Contains(codigo))
                {
                    return Task.FromResult(RbacResultado<IReadOnlyList<string>>.Erro(
                        RbacFalha.CentroCustoInvalido, $"O Centro de Custo '{codigo}' não existe no ERP."));
                }

                AncoradoEm[codigo] = unidadeNegocioId;
            }

            return Task.FromResult(RbacResultado<IReadOnlyList<string>>.Ok((IReadOnlyList<string>)normalizados));
        }
    }

    private static Cenario Arrange()
    {
        var permissoes = new FakePermissaoRepository();
        var perfis = new FakePerfilRepository { Permissoes = permissoes };
        var usuarios = new FakeUsuarioRepositoryCompleto { PerfilLookup = perfis.All };
        var centrosCusto = new FakeCentroCustoVinculoValidator { Existentes = { "cc-001", "cc-002", "cc-003" } };
        return new Cenario(usuarios, perfis, permissoes, centrosCusto);
    }

    private static readonly IReadOnlyList<string> AtorOnipotente = PermissaoCatalogo.Codigos.ToArray();

    private static Perfil CriarPerfil(Cenario c, Guid bu, string nome, params string[] codigosPermissao)
    {
        var perfil = new Perfil(nome, "Perfil de teste.", bu, DateTimeOffset.UtcNow);
        c.Perfis.All.Add(perfil);
        foreach (var codigo in codigosPermissao)
        {
            c.Perfis.Vinculos.Add(new PerfilPermissao(perfil.Id, c.Permissoes.IdDe(codigo)));
        }
        return perfil;
    }

    /// <summary>Vincula um usuário ativo ao Perfil "Administrador Sênior" na BU informada, satisfazendo a
    /// invariante para os testes que não a estão exercitando diretamente.</summary>
    private static Usuario SemearAdministradorSeniorAtivo(Cenario c, Guid bu)
    {
        var perfilAdmin = c.Perfis.All.SingleOrDefault(p => p.Nome == Perfil.AdministradorSenior && p.UnidadeNegocioId == bu)
            ?? CriarPerfil(c, bu, Perfil.AdministradorSenior);
        var usuario = new Usuario("admin.senior@somagrupo.com.br", "Admin Sênior", bu);
        c.Usuarios.All.Add(usuario);
        c.Usuarios.Perfis.Add(new UsuarioPerfil(usuario.Id, perfilAdmin.Id));
        return usuario;
    }

    private static CriarUsuarioUseCase Criar(Cenario c) => new(c.Usuarios, c.Perfis, c.CentrosCusto, TimeProvider.System);
    private static AtualizarUsuarioUseCase Atualizar(Cenario c) => new(c.Usuarios, c.Perfis, c.CentrosCusto, TimeProvider.System);
    private static AlterarStatusUsuarioUseCase AlterarStatus(Cenario c) => new(c.Usuarios, TimeProvider.System);

    [Fact]
    public async Task Criar_Should_Persist_Usuario_With_Perfis_And_CentrosCusto()
    {
        var c = Arrange();
        var perfil = CriarPerfil(c, Bu, "Analista", PermissaoCatalogo.PedidoCriar);

        var resultado = await Criar(c).ExecuteAsync(
            new UsuarioInput("Bruno Lima", "bruno.lima@somagrupo.com.br", [perfil.Id], ["cc-001", "cc-002"], false),
            Bu, AtorOnipotente, CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.Equal("Bruno Lima", resultado.Valor!.Nome);
        Assert.Equal("bruno.lima@somagrupo.com.br", resultado.Valor.Email);
        Assert.True(resultado.Valor.Ativo);
        Assert.Equal([perfil.Id], resultado.Valor.Perfis.Select(p => p.Id));
        Assert.Equal(["cc-001", "cc-002"], resultado.Valor.CentrosCusto);
        Assert.False(resultado.Valor.TodosCentrosCusto);
    }

    [Fact]
    public async Task Criar_Should_Allow_TodosCentrosCusto_Without_Explicit_Links()
    {
        var c = Arrange();

        var resultado = await Criar(c).ExecuteAsync(
            new UsuarioInput("Ana Souza", "ana.souza@somagrupo.com.br", [], [], true),
            Bu, AtorOnipotente, CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.True(resultado.Valor!.TodosCentrosCusto);
        Assert.Empty(resultado.Valor.CentrosCusto);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Criar_Should_Reject_Empty_Nome(string nome)
    {
        var c = Arrange();

        var resultado = await Criar(c).ExecuteAsync(
            new UsuarioInput(nome, "x@somagrupo.com.br", [], [], false), Bu, AtorOnipotente, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.NomeObrigatorio, resultado.Falha);
        Assert.Empty(c.Usuarios.All);
    }

    [Theory]
    [InlineData("")]
    [InlineData("nao-e-email")]
    public async Task Criar_Should_Reject_Invalid_Email(string email)
    {
        var c = Arrange();

        var resultado = await Criar(c).ExecuteAsync(
            new UsuarioInput("Nome Válido", email, [], [], false), Bu, AtorOnipotente, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.True(resultado.Falha is RbacFalha.EmailObrigatorio or RbacFalha.EmailInvalido);
    }

    [Fact]
    public async Task Criar_Should_Reject_Duplicated_Email()
    {
        var c = Arrange();
        await Criar(c).ExecuteAsync(new UsuarioInput("Primeiro", "duplicado@somagrupo.com.br", [], [], false), Bu, AtorOnipotente, CancellationToken.None);

        var resultado = await Criar(c).ExecuteAsync(
            new UsuarioInput("Segundo", "DUPLICADO@somagrupo.com.br", [], [], false), Bu, AtorOnipotente, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.EmailDuplicado, resultado.Falha);
        Assert.Single(c.Usuarios.All);
    }

    /// <summary>Isolamento entre Unidades de Negócio: um Perfil de outra BU nunca é aceito, mesmo com Id
    /// válido — mesmo cuidado de PerfisRequisitados/O1.5.</summary>
    [Fact]
    public async Task Criar_Should_Reject_Perfil_From_Another_UnidadeNegocio()
    {
        var c = Arrange();
        var perfilDeOutraBu = CriarPerfil(c, OutraBu, "Analista Outra BU");

        var resultado = await Criar(c).ExecuteAsync(
            new UsuarioInput("Carla Mendes", "carla.mendes@somagrupo.com.br", [perfilDeOutraBu.Id], [], false),
            Bu, AtorOnipotente, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.PerfilInvalido, resultado.Falha);
        Assert.Empty(c.Usuarios.All);
    }

    /// <summary>Não-escalonamento de privilégio via vínculo: um ator sem Perfil.Gerenciar não pode vincular
    /// um usuário a um Perfil que concede Perfil.Gerenciar (ou qualquer outra permissão que não possui).</summary>
    [Fact]
    public async Task Criar_Should_Reject_Vinculo_Above_Ator_Permissions()
    {
        var c = Arrange();
        var perfilAdministrativo = CriarPerfil(c, Bu, "Super Perfil", PermissaoCatalogo.PerfilGerenciar, PermissaoCatalogo.UsuarioGerenciar);
        var atorLimitado = new[] { PermissaoCatalogo.UsuarioGerenciar };

        var resultado = await Criar(c).ExecuteAsync(
            new UsuarioInput("Diego Alves", "diego.alves@somagrupo.com.br", [perfilAdministrativo.Id], [], false),
            Bu, atorLimitado, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.EscalonamentoDePrivilegio, resultado.Falha);
        Assert.Empty(c.Usuarios.All);
    }

    [Fact]
    public async Task Atualizar_Should_Replace_Perfis_And_CentrosCusto()
    {
        var c = Arrange();
        var perfilA = CriarPerfil(c, Bu, "Perfil A");
        var perfilB = CriarPerfil(c, Bu, "Perfil B");
        var criado = await Criar(c).ExecuteAsync(
            new UsuarioInput("Usuário", "usuario@somagrupo.com.br", [perfilA.Id], ["cc-001"], false), Bu, AtorOnipotente, CancellationToken.None);

        var resultado = await Atualizar(c).ExecuteAsync(
            criado.Valor!.Id,
            new UsuarioInput("Usuário Editado", "usuario@somagrupo.com.br", [perfilB.Id], ["cc-002"], true),
            Bu, AtorOnipotente, CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.Equal("Usuário Editado", resultado.Valor!.Nome);
        Assert.Equal([perfilB.Id], resultado.Valor.Perfis.Select(p => p.Id));
        Assert.Equal(["cc-002"], resultado.Valor.CentrosCusto);
        Assert.True(resultado.Valor.TodosCentrosCusto);
    }

    [Fact]
    public async Task Atualizar_Should_Reject_Email_Change()
    {
        var c = Arrange();
        var criado = await Criar(c).ExecuteAsync(
            new UsuarioInput("Usuário", "original@somagrupo.com.br", [], [], false), Bu, AtorOnipotente, CancellationToken.None);

        var resultado = await Atualizar(c).ExecuteAsync(
            criado.Valor!.Id,
            new UsuarioInput("Usuário", "outro@somagrupo.com.br", [], [], false),
            Bu, AtorOnipotente, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.EmailInvalido, resultado.Falha);
    }

    [Fact]
    public async Task Atualizar_Should_Return_NotFound_For_Usuario_From_Another_UnidadeNegocio()
    {
        var c = Arrange();
        var criado = await Criar(c).ExecuteAsync(
            new UsuarioInput("Usuário", "usuario2@somagrupo.com.br", [], [], false), Bu, AtorOnipotente, CancellationToken.None);

        var resultado = await Atualizar(c).ExecuteAsync(
            criado.Valor!.Id, new UsuarioInput("X", "usuario2@somagrupo.com.br", [], [], false), OutraBu, AtorOnipotente, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.UsuarioNaoEncontrado, resultado.Falha);
    }

    [Fact]
    public async Task AlterarStatus_Should_Activate_And_Inactivate_Non_Administrador_Senior()
    {
        var c = Arrange();
        var criado = await Criar(c).ExecuteAsync(
            new UsuarioInput("Analista", "analista@somagrupo.com.br", [], [], false), Bu, AtorOnipotente, CancellationToken.None);

        var inativado = await AlterarStatus(c).ExecuteAsync(criado.Valor!.Id, false, Bu, CancellationToken.None);
        Assert.True(inativado.Sucesso);
        Assert.False(inativado.Valor!.Ativo);

        var reativado = await AlterarStatus(c).ExecuteAsync(criado.Valor.Id, true, Bu, CancellationToken.None);
        Assert.True(reativado.Sucesso);
        Assert.True(reativado.Valor!.Ativo);
    }

    /// <summary>Regra do Administrador Sênior (D1, ADR-0021): a Unidade de Negócio nunca pode ficar sem
    /// nenhum Administrador Sênior ativo. Inativar o único deve ser bloqueado com 409 (Conflict).</summary>
    [Fact]
    public async Task AlterarStatus_Should_Block_Inactivating_Last_Active_AdministradorSenior()
    {
        var c = Arrange();
        var unicoAdmin = SemearAdministradorSeniorAtivo(c, Bu);

        var resultado = await AlterarStatus(c).ExecuteAsync(unicoAdmin.Id, false, Bu, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.UltimoAdministradorSeniorAtivo, resultado.Falha);
        Assert.True(unicoAdmin.EstaAtivo());
    }

    /// <summary>Com um segundo Administrador Sênior ativo, a inativação do primeiro é permitida — a
    /// invariante exige "ao menos um", não "todos".</summary>
    [Fact]
    public async Task AlterarStatus_Should_Allow_Inactivating_AdministradorSenior_When_Another_Remains_Active()
    {
        var c = Arrange();
        var primeiro = SemearAdministradorSeniorAtivo(c, Bu);
        var perfilAdmin = c.Perfis.All.Single(p => p.Nome == Perfil.AdministradorSenior);
        var segundo = new Usuario("outro.admin@somagrupo.com.br", "Outro Admin", Bu);
        c.Usuarios.All.Add(segundo);
        c.Usuarios.Perfis.Add(new UsuarioPerfil(segundo.Id, perfilAdmin.Id));

        var resultado = await AlterarStatus(c).ExecuteAsync(primeiro.Id, false, Bu, CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.False(resultado.Valor!.Ativo);
    }

    /// <summary>Isolamento entre Unidades de Negócio na própria invariante: um Administrador Sênior ativo
    /// em OUTRA BU nunca é contado como salvaguarda da BU sob operação.</summary>
    [Fact]
    public async Task AlterarStatus_Administrador_Senior_Invariant_Is_Scoped_By_UnidadeNegocio()
    {
        var c = Arrange();
        var adminDaBu = SemearAdministradorSeniorAtivo(c, Bu);
        SemearAdministradorSeniorAtivo(c, OutraBu);

        var resultado = await AlterarStatus(c).ExecuteAsync(adminDaBu.Id, false, Bu, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.UltimoAdministradorSeniorAtivo, resultado.Falha);
    }

    // ---- O1.6-L2 — validação real do vínculo Usuário×Centro de Custo ----

    /// <summary>Um código ERP que não existe é rejeitado — nunca aceito como texto livre.</summary>
    [Fact]
    public async Task Criar_Should_Reject_CentroCusto_Not_Found_In_Erp()
    {
        var c = Arrange();

        var resultado = await Criar(c).ExecuteAsync(
            new UsuarioInput("Bruno Lima", "bruno.lima@somagrupo.com.br", [], ["cc-inexistente"], false),
            Bu, AtorOnipotente, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.CentroCustoInvalido, resultado.Falha);
    }

    /// <summary>Um código ERP já ancorado a outra Unidade de Negócio não pode ser vinculado a um usuário de
    /// uma Unidade de Negócio diferente — fecha o vetor de vínculo cross-BU da dívida O1.6-L2.</summary>
    [Fact]
    public async Task Criar_Should_Reject_CentroCusto_Anchored_To_Another_UnidadeNegocio()
    {
        var c = Arrange();
        await Criar(c).ExecuteAsync(
            new UsuarioInput("Usuário Bu", "usuario.bu@somagrupo.com.br", [], ["cc-001"], false),
            Bu, AtorOnipotente, CancellationToken.None);

        var resultado = await Criar(c).ExecuteAsync(
            new UsuarioInput("Usuário OutraBu", "usuario.outrabu@somagrupo.com.br", [], ["cc-001"], false),
            OutraBu, AtorOnipotente, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.CentroCustoInvalido, resultado.Falha);
    }

    /// <summary>Editar um usuário existente também passa pela mesma validação — não é um cuidado exclusivo
    /// da criação.</summary>
    [Fact]
    public async Task Atualizar_Should_Reject_CentroCusto_Not_Found_In_Erp()
    {
        var c = Arrange();
        var perfil = CriarPerfil(c, Bu, "Analista", PermissaoCatalogo.PedidoCriar);
        var criado = await Criar(c).ExecuteAsync(
            new UsuarioInput("Usuário", "usuario@somagrupo.com.br", [perfil.Id], ["cc-001"], false), Bu, AtorOnipotente, CancellationToken.None);

        var resultado = await Atualizar(c).ExecuteAsync(
            criado.Valor!.Id,
            new UsuarioInput("Usuário", "usuario@somagrupo.com.br", [perfil.Id], ["cc-999"], false),
            Bu, AtorOnipotente, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.CentroCustoInvalido, resultado.Falha);
    }
}
