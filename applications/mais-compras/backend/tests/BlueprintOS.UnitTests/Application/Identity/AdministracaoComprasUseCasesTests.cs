using BlueprintOS.Application.Identity;
using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Domain.Identity;

namespace BlueprintOS.UnitTests.Application.Identity;

/// <summary>O1.12 — Fundação de Administração (Workflow, Alçadas, Controle Orçamentário). Cobre o CRUD com
/// repositório fake em memória (mesmo padrão de <c>CentroCustoUnidadeAlocacaoUseCasesTests</c>): criação,
/// edição, ativação/inativação, isolamento por Unidade de Negócio e as validações de FK/invariantes que a
/// camada de aplicação adiciona sobre o domínio (aprovador/Centro de Custo pertencentes à mesma BU).</summary>
public sealed class AdministracaoComprasUseCasesTests
{
    private static readonly Guid Bu = Guid.NewGuid();
    private static readonly Guid OutraBu = Guid.NewGuid();

    private sealed class FakeUnidadeNegocioRepository : IUnidadeNegocioRepository
    {
        private readonly HashSet<Guid> _existentes;

        public FakeUnidadeNegocioRepository(params Guid[] existentes) => _existentes = [.. existentes];

        public Task<UnidadeNegocio?> ObterPorIdAsync(Guid id, CancellationToken ct) =>
            Task.FromResult(_existentes.Contains(id) ? new UnidadeNegocio("BU", $"bu-{id:N}") : null);
        public Task<UnidadeNegocio?> ObterPorSlugAsync(string slug, CancellationToken ct) => Task.FromResult<UnidadeNegocio?>(null);

        public Task<bool> PossuiAdministradorSeniorAtivoAsync(Guid unidadeNegocioId, CancellationToken ct) => Task.FromResult(false);

        public Task AdicionarAsync(UnidadeNegocio unidadeNegocio, CancellationToken ct)
        {
            _existentes.Add(unidadeNegocio.Id);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<UnidadeNegocio>> ListarTodasAsync(CancellationToken ct) =>
            Task.FromResult((IReadOnlyList<UnidadeNegocio>)Array.Empty<UnidadeNegocio>());

        public Task<bool> ExisteComSlugAsync(string slug, Guid? excluirId, CancellationToken ct) => Task.FromResult(false);

        public Task SalvarAlteracoesAsync(CancellationToken ct) => Task.CompletedTask;
    }

    /// <summary>Fake padrão com as duas Unidades de Negócio usadas pela suíte já cadastradas — cobre o
    /// caminho feliz. Os testes de "BU inválida" usam um fake vazio (<c>new FakeUnidadeNegocioRepository()</c>).</summary>
    private static FakeUnidadeNegocioRepository UnidadesNegocioPadrao() => new(Bu, OutraBu);

    private sealed class FakeRegraWorkflowRepository : IRegraWorkflowRepository
    {
        public List<RegraWorkflow> Registros { get; } = [];

        public Task<IReadOnlyList<RegraWorkflow>> ListarPorUnidadeNegocioAsync(Guid unidadeNegocioId, CancellationToken ct) =>
            Task.FromResult((IReadOnlyList<RegraWorkflow>)Registros.Where(x => x.UnidadeNegocioId == unidadeNegocioId).ToArray());

        public Task<RegraWorkflow?> ObterPorIdEUnidadeNegocioAsync(Guid id, Guid unidadeNegocioId, CancellationToken ct) =>
            Task.FromResult(Registros.SingleOrDefault(x => x.Id == id && x.UnidadeNegocioId == unidadeNegocioId));

        public Task AdicionarAsync(RegraWorkflow regraWorkflow, CancellationToken ct)
        {
            Registros.Add(regraWorkflow);
            return Task.CompletedTask;
        }

        public Task SalvarAlteracoesAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeAlcadaAprovacaoRepository : IAlcadaAprovacaoRepository
    {
        public List<AlcadaAprovacao> Registros { get; } = [];

        public Task<IReadOnlyList<AlcadaAprovacao>> ListarPorUnidadeNegocioAsync(Guid unidadeNegocioId, CancellationToken ct) =>
            Task.FromResult((IReadOnlyList<AlcadaAprovacao>)Registros.Where(x => x.UnidadeNegocioId == unidadeNegocioId).ToArray());

        public Task<AlcadaAprovacao?> ObterPorIdEUnidadeNegocioAsync(Guid id, Guid unidadeNegocioId, CancellationToken ct) =>
            Task.FromResult(Registros.SingleOrDefault(x => x.Id == id && x.UnidadeNegocioId == unidadeNegocioId));

        public Task AdicionarAsync(AlcadaAprovacao alcadaAprovacao, CancellationToken ct)
        {
            Registros.Add(alcadaAprovacao);
            return Task.CompletedTask;
        }

        public Task SalvarAlteracoesAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeRegraOrcamentariaRepository : IRegraOrcamentariaRepository
    {
        public List<RegraOrcamentaria> Registros { get; } = [];

        public Task<IReadOnlyList<RegraOrcamentaria>> ListarPorUnidadeNegocioAsync(Guid unidadeNegocioId, CancellationToken ct) =>
            Task.FromResult((IReadOnlyList<RegraOrcamentaria>)Registros.Where(x => x.UnidadeNegocioId == unidadeNegocioId).ToArray());

        public Task<RegraOrcamentaria?> ObterPorIdEUnidadeNegocioAsync(Guid id, Guid unidadeNegocioId, CancellationToken ct) =>
            Task.FromResult(Registros.SingleOrDefault(x => x.Id == id && x.UnidadeNegocioId == unidadeNegocioId));

        public Task AdicionarAsync(RegraOrcamentaria regraOrcamentaria, CancellationToken ct)
        {
            Registros.Add(regraOrcamentaria);
            return Task.CompletedTask;
        }

        public Task SalvarAlteracoesAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeCentroCustoMetadadoRepository : ICentroCustoMetadadoRepository
    {
        public List<CentroCustoMetadado> Registros { get; } = [];

        public Task<IReadOnlyDictionary<string, CentroCustoMetadado>> ListarPorUnidadeNegocioAsync(Guid unidadeNegocioId, CancellationToken ct) =>
            Task.FromResult((IReadOnlyDictionary<string, CentroCustoMetadado>)Registros
                .Where(x => x.UnidadeNegocioId == unidadeNegocioId)
                .ToDictionary(x => x.CodigoErp, StringComparer.OrdinalIgnoreCase));

        public Task<CentroCustoMetadado?> ObterPorCodigoErpAsync(string codigoErp, Guid unidadeNegocioId, CancellationToken ct) =>
            Task.FromResult(Registros.SingleOrDefault(x => x.CodigoErp == codigoErp && x.UnidadeNegocioId == unidadeNegocioId));

        public Task<CentroCustoMetadado?> ObterPorIdEUnidadeNegocioAsync(Guid id, Guid unidadeNegocioId, CancellationToken ct) =>
            Task.FromResult(Registros.SingleOrDefault(x => x.Id == id && x.UnidadeNegocioId == unidadeNegocioId));

        public Task AdicionarAsync(CentroCustoMetadado metadado, CancellationToken ct)
        {
            Registros.Add(metadado);
            return Task.CompletedTask;
        }

        public Task SalvarAlteracoesAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeUsuarioRepository : IUsuarioRepository
    {
        public List<Usuario> Registros { get; } = [];

        public Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken ct) =>
            Task.FromResult(Registros.SingleOrDefault(x => x.Email == email));

        public Task<Usuario?> ObterPorIdAsync(Guid id, CancellationToken ct) =>
            Task.FromResult(Registros.SingleOrDefault(x => x.Id == id));

        public Task AdicionarAsync(Usuario usuario, CancellationToken ct)
        {
            Registros.Add(usuario);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Usuario>> ListarPorUnidadeNegocioAsync(Guid unidadeNegocioId, CancellationToken ct) =>
            Task.FromResult((IReadOnlyList<Usuario>)Registros.Where(x => x.UnidadeNegocioId == unidadeNegocioId).ToArray());

        public Task<Usuario?> ObterPorIdEUnidadeNegocioAsync(Guid id, Guid unidadeNegocioId, CancellationToken ct) =>
            Task.FromResult(Registros.SingleOrDefault(x => x.Id == id && x.UnidadeNegocioId == unidadeNegocioId));

        public Task<Usuario?> ObterPorEmailEUnidadeNegocioAsync(string email, Guid unidadeNegocioId, CancellationToken ct) =>
            Task.FromResult(Registros.SingleOrDefault(x => x.Email == email && x.UnidadeNegocioId == unidadeNegocioId));

        public Task<IReadOnlyDictionary<Guid, IReadOnlyList<UsuarioPerfilResumoDto>>> ObterPerfisPorUsuarioAsync(IReadOnlyCollection<Guid> usuarioIds, CancellationToken ct) =>
            Task.FromResult((IReadOnlyDictionary<Guid, IReadOnlyList<UsuarioPerfilResumoDto>>)new Dictionary<Guid, IReadOnlyList<UsuarioPerfilResumoDto>>());

        public Task<IReadOnlyDictionary<Guid, IReadOnlyList<string>>> ObterCentrosCustoPorUsuarioAsync(IReadOnlyCollection<Guid> usuarioIds, CancellationToken ct) =>
            Task.FromResult((IReadOnlyDictionary<Guid, IReadOnlyList<string>>)new Dictionary<Guid, IReadOnlyList<string>>());

        public Task SubstituirPerfisAsync(Guid usuarioId, IReadOnlyCollection<Guid> perfilIds, CancellationToken ct) => Task.CompletedTask;

        public Task SubstituirCentrosCustoAsync(Guid usuarioId, IReadOnlyCollection<string> codigosErp, CancellationToken ct) => Task.CompletedTask;

        public Task<int> ContarAdministradoresSeniorAtivosAsync(Guid unidadeNegocioId, Guid? excluirUsuarioId, CancellationToken ct) =>
            Task.FromResult(0);

        public Task SalvarAlteracoesAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakePerfilRepository : IPerfilRepository
    {
        public List<Perfil> Registros { get; } = [];

        public Task<Perfil?> ObterPorNomeEUnidadeNegocioAsync(string nome, Guid unidadeNegocioId, CancellationToken ct) =>
            Task.FromResult(Registros.SingleOrDefault(x => x.Nome == nome && x.UnidadeNegocioId == unidadeNegocioId));

        public Task AdicionarAsync(Perfil perfil, CancellationToken ct)
        {
            Registros.Add(perfil);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Perfil>> ListarPorUnidadeNegocioAsync(Guid unidadeNegocioId, CancellationToken ct) =>
            Task.FromResult((IReadOnlyList<Perfil>)Registros.Where(x => x.UnidadeNegocioId == unidadeNegocioId).ToArray());

        public Task<Perfil?> ObterPorIdEUnidadeNegocioAsync(Guid id, Guid unidadeNegocioId, CancellationToken ct) =>
            Task.FromResult(Registros.SingleOrDefault(x => x.Id == id && x.UnidadeNegocioId == unidadeNegocioId));

        public Task<IReadOnlyList<Perfil>> ObterPorIdsEUnidadeNegocioAsync(IReadOnlyCollection<Guid> ids, Guid unidadeNegocioId, CancellationToken ct) =>
            Task.FromResult((IReadOnlyList<Perfil>)Registros.Where(x => ids.Contains(x.Id) && x.UnidadeNegocioId == unidadeNegocioId).ToArray());

        public Task<IReadOnlyDictionary<Guid, IReadOnlyList<string>>> ObterPermissoesPorPerfilAsync(IReadOnlyCollection<Guid> perfilIds, CancellationToken ct) =>
            Task.FromResult((IReadOnlyDictionary<Guid, IReadOnlyList<string>>)new Dictionary<Guid, IReadOnlyList<string>>());

        public Task<IReadOnlyDictionary<Guid, int>> ContarUsuariosPorPerfilAsync(IReadOnlyCollection<Guid> perfilIds, CancellationToken ct) =>
            Task.FromResult((IReadOnlyDictionary<Guid, int>)new Dictionary<Guid, int>());

        public Task SubstituirPermissoesAsync(Guid perfilId, IReadOnlyCollection<Guid> permissaoIds, CancellationToken ct) => Task.CompletedTask;

        public Task VincularPermissoesAsync(Guid perfilId, IReadOnlyCollection<Guid> permissaoIds, CancellationToken ct) => Task.CompletedTask;

        public Task SalvarAlteracoesAsync(CancellationToken ct) => Task.CompletedTask;
    }

    // ---------------- RegraWorkflow ----------------

    [Fact]
    public async Task RegraWorkflow_Criar_Should_Succeed_And_Be_Scoped_To_UnidadeNegocio()
    {
        var repo = new FakeRegraWorkflowRepository();
        var criar = new CriarRegraWorkflowUseCase(repo, UnidadesNegocioPadrao(), TimeProvider.System);

        var resultado = await criar.ExecuteAsync(new RegraWorkflowInput("Regra 1", "Solicitação", 1), Bu, CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.Equal(Bu, resultado.Valor!.UnidadeNegocioId);
        Assert.Single(repo.Registros);
    }

    [Fact]
    public async Task RegraWorkflow_Criar_Should_Reject_Unknown_UnidadeNegocio()
    {
        var repo = new FakeRegraWorkflowRepository();
        var criar = new CriarRegraWorkflowUseCase(repo, new FakeUnidadeNegocioRepository(), TimeProvider.System);

        var resultado = await criar.ExecuteAsync(new RegraWorkflowInput("Regra 1", "Solicitação", 1), Bu, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.UnidadeNegocioNaoEncontrada, resultado.Falha);
        Assert.Empty(repo.Registros);
    }

    [Theory]
    [InlineData("", "Solicitação")]
    [InlineData("Regra", "")]
    public async Task RegraWorkflow_Criar_Should_Reject_Missing_Required_Fields(string nome, string tipoProcesso)
    {
        var criar = new CriarRegraWorkflowUseCase(new FakeRegraWorkflowRepository(), UnidadesNegocioPadrao(), TimeProvider.System);

        var resultado = await criar.ExecuteAsync(new RegraWorkflowInput(nome, tipoProcesso, 1), Bu, CancellationToken.None);

        Assert.False(resultado.Sucesso);
    }

    [Fact]
    public async Task RegraWorkflow_Atualizar_Should_Reject_When_Regra_Belongs_To_Another_UnidadeNegocio()
    {
        var repo = new FakeRegraWorkflowRepository();
        var criar = new CriarRegraWorkflowUseCase(repo, UnidadesNegocioPadrao(), TimeProvider.System);
        var criado = await criar.ExecuteAsync(new RegraWorkflowInput("Regra", "Solicitação", 1), OutraBu, CancellationToken.None);

        var atualizar = new AtualizarRegraWorkflowUseCase(repo, TimeProvider.System);
        var resultado = await atualizar.ExecuteAsync(criado.Valor!.Id, new RegraWorkflowInput("Regra 2", "Cotação", 2), Bu, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.RegraWorkflowNaoEncontrada, resultado.Falha);
    }

    [Fact]
    public async Task RegraWorkflow_AlterarStatus_Should_Toggle_Ativo()
    {
        var repo = new FakeRegraWorkflowRepository();
        var criado = await new CriarRegraWorkflowUseCase(repo, UnidadesNegocioPadrao(), TimeProvider.System)
            .ExecuteAsync(new RegraWorkflowInput("Regra", "Solicitação", 1), Bu, CancellationToken.None);

        var alterarStatus = new AlterarStatusRegraWorkflowUseCase(repo, TimeProvider.System);
        var inativado = await alterarStatus.ExecuteAsync(criado.Valor!.Id, false, Bu, CancellationToken.None);
        var reativado = await alterarStatus.ExecuteAsync(criado.Valor!.Id, true, Bu, CancellationToken.None);

        Assert.False(inativado.Valor!.Ativo);
        Assert.True(reativado.Valor!.Ativo);
    }

    // ---------------- AlcadaAprovacao ----------------

    private static (FakeAlcadaAprovacaoRepository Alcadas, FakeCentroCustoMetadadoRepository CentrosCusto, FakeUsuarioRepository Usuarios, FakePerfilRepository Perfis) ArrangeAlcada() =>
        (new(), new(), new(), new());

    [Fact]
    public async Task AlcadaAprovacao_Criar_Should_Succeed_With_Usuario_Approver_In_The_Same_UnidadeNegocio()
    {
        var (alcadas, centrosCusto, usuarios, perfis) = ArrangeAlcada();
        var usuario = new Usuario("ana@example.invalid", "Ana", Bu);
        usuarios.Registros.Add(usuario);
        var criar = new CriarAlcadaAprovacaoUseCase(alcadas, UnidadesNegocioPadrao(), centrosCusto, usuarios, perfis, TimeProvider.System);

        var resultado = await criar.ExecuteAsync(
            new AlcadaAprovacaoInput("Alçada 1", CriterioAlcada.Valor, 0m, 10000m, null, 1, usuario.Id, null), Bu, CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.Equal(usuario.Id, resultado.Valor!.AprovadorUsuarioId);
    }

    [Fact]
    public async Task AlcadaAprovacao_Criar_Should_Reject_Unknown_UnidadeNegocio()
    {
        var (alcadas, centrosCusto, usuarios, perfis) = ArrangeAlcada();
        var usuario = new Usuario("ana@example.invalid", "Ana", Bu);
        usuarios.Registros.Add(usuario);
        var criar = new CriarAlcadaAprovacaoUseCase(alcadas, new FakeUnidadeNegocioRepository(), centrosCusto, usuarios, perfis, TimeProvider.System);

        var resultado = await criar.ExecuteAsync(
            new AlcadaAprovacaoInput("Alçada 1", CriterioAlcada.Valor, 0m, 10000m, null, 1, usuario.Id, null), Bu, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.UnidadeNegocioNaoEncontrada, resultado.Falha);
        Assert.Empty(alcadas.Registros);
    }

    [Fact]
    public async Task AlcadaAprovacao_Criar_Should_Reject_Usuario_Approver_From_Another_UnidadeNegocio()
    {
        var (alcadas, centrosCusto, usuarios, perfis) = ArrangeAlcada();
        var usuarioDeOutraBu = new Usuario("bob@example.invalid", "Bob", OutraBu);
        usuarios.Registros.Add(usuarioDeOutraBu);
        var criar = new CriarAlcadaAprovacaoUseCase(alcadas, UnidadesNegocioPadrao(), centrosCusto, usuarios, perfis, TimeProvider.System);

        var resultado = await criar.ExecuteAsync(
            new AlcadaAprovacaoInput("Alçada 1", CriterioAlcada.Valor, null, null, null, 1, usuarioDeOutraBu.Id, null), Bu, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.AprovadorInvalido, resultado.Falha);
        Assert.Empty(alcadas.Registros);
    }

    [Fact]
    public async Task AlcadaAprovacao_Criar_Should_Reject_Perfil_Approver_From_Another_UnidadeNegocio()
    {
        var (alcadas, centrosCusto, usuarios, perfis) = ArrangeAlcada();
        var perfilDeOutraBu = new Perfil("Aprovador", "desc", OutraBu, DateTimeOffset.UtcNow);
        perfis.Registros.Add(perfilDeOutraBu);
        var criar = new CriarAlcadaAprovacaoUseCase(alcadas, UnidadesNegocioPadrao(), centrosCusto, usuarios, perfis, TimeProvider.System);

        var resultado = await criar.ExecuteAsync(
            new AlcadaAprovacaoInput("Alçada 1", CriterioAlcada.Valor, null, null, null, 1, null, perfilDeOutraBu.Id), Bu, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.AprovadorInvalido, resultado.Falha);
    }

    [Fact]
    public async Task AlcadaAprovacao_Criar_Should_Reject_Both_Approvers_Informed()
    {
        var (alcadas, centrosCusto, usuarios, perfis) = ArrangeAlcada();
        var usuario = new Usuario("ana@example.invalid", "Ana", Bu);
        usuarios.Registros.Add(usuario);
        var perfil = new Perfil("Aprovador", "desc", Bu, DateTimeOffset.UtcNow);
        perfis.Registros.Add(perfil);
        var criar = new CriarAlcadaAprovacaoUseCase(alcadas, UnidadesNegocioPadrao(), centrosCusto, usuarios, perfis, TimeProvider.System);

        var resultado = await criar.ExecuteAsync(
            new AlcadaAprovacaoInput("Alçada 1", CriterioAlcada.Valor, null, null, null, 1, usuario.Id, perfil.Id), Bu, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.AprovadorInvalido, resultado.Falha);
    }

    [Fact]
    public async Task AlcadaAprovacao_Criar_Should_Reject_No_Approver_Informed()
    {
        var (alcadas, centrosCusto, usuarios, perfis) = ArrangeAlcada();
        var criar = new CriarAlcadaAprovacaoUseCase(alcadas, UnidadesNegocioPadrao(), centrosCusto, usuarios, perfis, TimeProvider.System);

        var resultado = await criar.ExecuteAsync(
            new AlcadaAprovacaoInput("Alçada 1", CriterioAlcada.Valor, null, null, null, 1, null, null), Bu, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.AprovadorInvalido, resultado.Falha);
    }

    [Fact]
    public async Task AlcadaAprovacao_Criar_Should_Reject_Nivel_Less_Than_One()
    {
        var (alcadas, centrosCusto, usuarios, perfis) = ArrangeAlcada();
        var usuario = new Usuario("ana@example.invalid", "Ana", Bu);
        usuarios.Registros.Add(usuario);
        var criar = new CriarAlcadaAprovacaoUseCase(alcadas, UnidadesNegocioPadrao(), centrosCusto, usuarios, perfis, TimeProvider.System);

        var resultado = await criar.ExecuteAsync(
            new AlcadaAprovacaoInput("Alçada 1", CriterioAlcada.Valor, null, null, null, 0, usuario.Id, null), Bu, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.NivelInvalido, resultado.Falha);
    }

    [Fact]
    public async Task AlcadaAprovacao_Criar_Should_Reject_Inverted_Value_Range()
    {
        var (alcadas, centrosCusto, usuarios, perfis) = ArrangeAlcada();
        var usuario = new Usuario("ana@example.invalid", "Ana", Bu);
        usuarios.Registros.Add(usuario);
        var criar = new CriarAlcadaAprovacaoUseCase(alcadas, UnidadesNegocioPadrao(), centrosCusto, usuarios, perfis, TimeProvider.System);

        var resultado = await criar.ExecuteAsync(
            new AlcadaAprovacaoInput("Alçada 1", CriterioAlcada.Valor, 1000m, 100m, null, 1, usuario.Id, null), Bu, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.FaixaDeValorInvalida, resultado.Falha);
    }

    [Fact]
    public async Task AlcadaAprovacao_Criar_Should_Reject_CentroCusto_From_Another_UnidadeNegocio()
    {
        var (alcadas, centrosCusto, usuarios, perfis) = ArrangeAlcada();
        var usuario = new Usuario("ana@example.invalid", "Ana", Bu);
        usuarios.Registros.Add(usuario);
        var centroCustoDeOutraBu = new CentroCustoMetadado("CC-001", OutraBu, DateTimeOffset.UtcNow);
        centrosCusto.Registros.Add(centroCustoDeOutraBu);
        var criar = new CriarAlcadaAprovacaoUseCase(alcadas, UnidadesNegocioPadrao(), centrosCusto, usuarios, perfis, TimeProvider.System);

        var resultado = await criar.ExecuteAsync(
            new AlcadaAprovacaoInput("Alçada 1", CriterioAlcada.CentroCusto, null, null, centroCustoDeOutraBu.Id, 1, usuario.Id, null), Bu, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.CentroCustoInvalidoNaUnidadeDeNegocio, resultado.Falha);
    }

    [Fact]
    public async Task AlcadaAprovacao_Criar_Should_Reject_Missing_CentroCusto_When_Criterio_Is_CentroCusto()
    {
        var (alcadas, centrosCusto, usuarios, perfis) = ArrangeAlcada();
        var usuario = new Usuario("ana@example.invalid", "Ana", Bu);
        usuarios.Registros.Add(usuario);
        var criar = new CriarAlcadaAprovacaoUseCase(alcadas, UnidadesNegocioPadrao(), centrosCusto, usuarios, perfis, TimeProvider.System);

        var resultado = await criar.ExecuteAsync(
            new AlcadaAprovacaoInput("Alçada 1", CriterioAlcada.CentroCusto, null, null, null, 1, usuario.Id, null), Bu, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.CentroCustoObrigatorio, resultado.Falha);
    }

    [Fact]
    public async Task AlcadaAprovacao_AlterarStatus_Should_Toggle_Ativo()
    {
        var (alcadas, centrosCusto, usuarios, perfis) = ArrangeAlcada();
        var usuario = new Usuario("ana@example.invalid", "Ana", Bu);
        usuarios.Registros.Add(usuario);
        var criado = await new CriarAlcadaAprovacaoUseCase(alcadas, UnidadesNegocioPadrao(), centrosCusto, usuarios, perfis, TimeProvider.System)
            .ExecuteAsync(new AlcadaAprovacaoInput("Alçada 1", CriterioAlcada.Valor, null, null, null, 1, usuario.Id, null), Bu, CancellationToken.None);

        var alterarStatus = new AlterarStatusAlcadaAprovacaoUseCase(alcadas, TimeProvider.System);
        var inativado = await alterarStatus.ExecuteAsync(criado.Valor!.Id, false, Bu, CancellationToken.None);

        Assert.True(inativado.Sucesso);
        Assert.False(inativado.Valor!.Ativo);
    }

    [Fact]
    public async Task AlcadaAprovacao_AlterarStatus_Should_Reject_Cross_UnidadeNegocio()
    {
        var (alcadas, centrosCusto, usuarios, perfis) = ArrangeAlcada();
        var usuario = new Usuario("ana@example.invalid", "Ana", OutraBu);
        usuarios.Registros.Add(usuario);
        var criado = await new CriarAlcadaAprovacaoUseCase(alcadas, UnidadesNegocioPadrao(), centrosCusto, usuarios, perfis, TimeProvider.System)
            .ExecuteAsync(new AlcadaAprovacaoInput("Alçada 1", CriterioAlcada.Valor, null, null, null, 1, usuario.Id, null), OutraBu, CancellationToken.None);

        var alterarStatus = new AlterarStatusAlcadaAprovacaoUseCase(alcadas, TimeProvider.System);
        var resultado = await alterarStatus.ExecuteAsync(criado.Valor!.Id, false, Bu, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.AlcadaAprovacaoNaoEncontrada, resultado.Falha);
    }

    // ---------------- RegraOrcamentaria ----------------

    [Fact]
    public async Task RegraOrcamentaria_Criar_Should_Succeed_With_CentroCusto_In_The_Same_UnidadeNegocio()
    {
        var regras = new FakeRegraOrcamentariaRepository();
        var centrosCusto = new FakeCentroCustoMetadadoRepository();
        var centroCusto = new CentroCustoMetadado("CC-001", Bu, DateTimeOffset.UtcNow);
        centrosCusto.Registros.Add(centroCusto);
        var criar = new CriarRegraOrcamentariaUseCase(regras, UnidadesNegocioPadrao(), centrosCusto, TimeProvider.System);

        var resultado = await criar.ExecuteAsync(
            new RegraOrcamentariaInput("Regra 1", centroCusto.Id, 5000m, PeriodoOrcamentario.Mensal), Bu, CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.Equal(centroCusto.Id, resultado.Valor!.CentroCustoMetadadoId);
    }

    [Fact]
    public async Task RegraOrcamentaria_Criar_Should_Reject_Unknown_UnidadeNegocio()
    {
        var regras = new FakeRegraOrcamentariaRepository();
        var centrosCusto = new FakeCentroCustoMetadadoRepository();
        var centroCusto = new CentroCustoMetadado("CC-001", Bu, DateTimeOffset.UtcNow);
        centrosCusto.Registros.Add(centroCusto);
        var criar = new CriarRegraOrcamentariaUseCase(regras, new FakeUnidadeNegocioRepository(), centrosCusto, TimeProvider.System);

        var resultado = await criar.ExecuteAsync(
            new RegraOrcamentariaInput("Regra 1", centroCusto.Id, 5000m, PeriodoOrcamentario.Mensal), Bu, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.UnidadeNegocioNaoEncontrada, resultado.Falha);
        Assert.Empty(regras.Registros);
    }

    [Fact]
    public async Task RegraOrcamentaria_Criar_Should_Reject_CentroCusto_From_Another_UnidadeNegocio()
    {
        var regras = new FakeRegraOrcamentariaRepository();
        var centrosCusto = new FakeCentroCustoMetadadoRepository();
        var centroCustoDeOutraBu = new CentroCustoMetadado("CC-001", OutraBu, DateTimeOffset.UtcNow);
        centrosCusto.Registros.Add(centroCustoDeOutraBu);
        var criar = new CriarRegraOrcamentariaUseCase(regras, UnidadesNegocioPadrao(), centrosCusto, TimeProvider.System);

        var resultado = await criar.ExecuteAsync(
            new RegraOrcamentariaInput("Regra 1", centroCustoDeOutraBu.Id, 5000m, PeriodoOrcamentario.Mensal), Bu, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.CentroCustoInvalidoNaUnidadeDeNegocio, resultado.Falha);
        Assert.Empty(regras.Registros);
    }

    [Fact]
    public async Task RegraOrcamentaria_Criar_Should_Reject_Unknown_CentroCusto()
    {
        var regras = new FakeRegraOrcamentariaRepository();
        var centrosCusto = new FakeCentroCustoMetadadoRepository();
        var criar = new CriarRegraOrcamentariaUseCase(regras, UnidadesNegocioPadrao(), centrosCusto, TimeProvider.System);

        var resultado = await criar.ExecuteAsync(
            new RegraOrcamentariaInput("Regra 1", Guid.NewGuid(), 5000m, PeriodoOrcamentario.Mensal), Bu, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.CentroCustoInvalidoNaUnidadeDeNegocio, resultado.Falha);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public async Task RegraOrcamentaria_Criar_Should_Reject_Non_Positive_ValorLimite(decimal valorLimite)
    {
        var regras = new FakeRegraOrcamentariaRepository();
        var centrosCusto = new FakeCentroCustoMetadadoRepository();
        var centroCusto = new CentroCustoMetadado("CC-001", Bu, DateTimeOffset.UtcNow);
        centrosCusto.Registros.Add(centroCusto);
        var criar = new CriarRegraOrcamentariaUseCase(regras, UnidadesNegocioPadrao(), centrosCusto, TimeProvider.System);

        var resultado = await criar.ExecuteAsync(
            new RegraOrcamentariaInput("Regra 1", centroCusto.Id, valorLimite, PeriodoOrcamentario.Mensal), Bu, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.ValorLimiteInvalido, resultado.Falha);
    }

    [Fact]
    public async Task RegraOrcamentaria_AlterarStatus_Should_Toggle_Ativo()
    {
        var regras = new FakeRegraOrcamentariaRepository();
        var centrosCusto = new FakeCentroCustoMetadadoRepository();
        var centroCusto = new CentroCustoMetadado("CC-001", Bu, DateTimeOffset.UtcNow);
        centrosCusto.Registros.Add(centroCusto);
        var criado = await new CriarRegraOrcamentariaUseCase(regras, UnidadesNegocioPadrao(), centrosCusto, TimeProvider.System)
            .ExecuteAsync(new RegraOrcamentariaInput("Regra 1", centroCusto.Id, 5000m, PeriodoOrcamentario.Anual), Bu, CancellationToken.None);

        var alterarStatus = new AlterarStatusRegraOrcamentariaUseCase(regras, TimeProvider.System);
        var inativado = await alterarStatus.ExecuteAsync(criado.Valor!.Id, false, Bu, CancellationToken.None);

        Assert.True(inativado.Sucesso);
        Assert.False(inativado.Valor!.Ativo);
    }

    [Fact]
    public async Task RegraOrcamentaria_Atualizar_Should_Reject_When_Regra_Belongs_To_Another_UnidadeNegocio()
    {
        var regras = new FakeRegraOrcamentariaRepository();
        var centrosCusto = new FakeCentroCustoMetadadoRepository();
        var centroCusto = new CentroCustoMetadado("CC-001", OutraBu, DateTimeOffset.UtcNow);
        centrosCusto.Registros.Add(centroCusto);
        var criado = await new CriarRegraOrcamentariaUseCase(regras, UnidadesNegocioPadrao(), centrosCusto, TimeProvider.System)
            .ExecuteAsync(new RegraOrcamentariaInput("Regra 1", centroCusto.Id, 5000m, PeriodoOrcamentario.Mensal), OutraBu, CancellationToken.None);

        var atualizar = new AtualizarRegraOrcamentariaUseCase(regras, centrosCusto, TimeProvider.System);
        var resultado = await atualizar.ExecuteAsync(
            criado.Valor!.Id, new RegraOrcamentariaInput("Regra 2", centroCusto.Id, 6000m, PeriodoOrcamentario.Mensal), Bu, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.RegraOrcamentariaNaoEncontrada, resultado.Falha);
    }

    [Fact]
    public async Task Listar_Should_Not_Leak_Across_UnidadesDeNegocio()
    {
        var regras = new FakeRegraOrcamentariaRepository();
        var centrosCusto = new FakeCentroCustoMetadadoRepository();
        var centroCustoDeOutraBu = new CentroCustoMetadado("CC-001", OutraBu, DateTimeOffset.UtcNow);
        centrosCusto.Registros.Add(centroCustoDeOutraBu);
        await new CriarRegraOrcamentariaUseCase(regras, UnidadesNegocioPadrao(), centrosCusto, TimeProvider.System)
            .ExecuteAsync(new RegraOrcamentariaInput("Regra 1", centroCustoDeOutraBu.Id, 5000m, PeriodoOrcamentario.Mensal), OutraBu, CancellationToken.None);

        var listar = new ListarRegrasOrcamentariasUseCase(regras);
        var resultado = await listar.ExecuteAsync(Bu, CancellationToken.None);

        Assert.Empty(resultado);
    }
}
