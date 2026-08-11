using BlueprintOS.Application.Identity;
using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Domain.Identity;
using Microsoft.Extensions.Logging.Abstractions;

namespace BlueprintOS.UnitTests.Application.Identity;

/// <summary>Cobre o subconjunto do plano de testes da Work Order O1.4.3 (seção 18) atribuído a O1.4.3.2 e não
/// coberto por <c>ConcluirBootstrapConcurrencyTests</c> (concorrência real via InMemory): itens 11 (sucesso
/// ponta a ponta com fakes), 13 (rejeição pós-conclusão), 10/9 (sessão inválida), 21 (payload inválido) e a
/// regra de reaproveitamento/rejeição de Unidade de Negócio com Administrador Sênior já ativo (seção 13,
/// passo 2).</summary>
public sealed class ConcluirBootstrapUseCaseTests
{
    private static readonly UnidadeNegocioBootstrapPayload NovaUnidade = new(null, "SOMA Matriz", "soma-matriz");
    private static readonly AdministradorSeniorBootstrapPayload AdministradorValido = new("Administradora Sênior");

    [Fact]
    public async Task Should_Succeed_End_To_End_And_Create_All_Expected_Entities()
    {
        var (useCase, ctx) = Arrange();
        var sessaoId = ctx.AdicionarSessaoValida("admin.inicial@example.invalid");

        var resultado = await useCase.ExecuteAsync(sessaoId, NovaUnidade, AdministradorValido, CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.Equal("admin.inicial@example.invalid", resultado.Email);
        Assert.NotNull(resultado.UsuarioId);
        Assert.NotNull(resultado.UnidadeNegocioId);
        Assert.Single(ctx.Usuarios.All);
        Assert.Single(ctx.UnidadesNegocio.All);
        Assert.Single(ctx.Perfis.All, p => p.Nome == Perfil.AdministradorSenior);
        Assert.Single(ctx.UsuariosPerfis.All);
        Assert.True(ctx.Estados.Estado!.Concluido);
        Assert.NotNull(ctx.Sessoes.All.Single().UsadaEm);
    }

    [Fact]
    public async Task Should_Reject_When_Already_Concluded()
    {
        var (useCase, ctx) = Arrange();
        var sessaoId = ctx.AdicionarSessaoValida("admin.inicial@example.invalid");
        MarcarConcluido(ctx.Estados.Estado!);

        var resultado = await useCase.ExecuteAsync(sessaoId, NovaUnidade, AdministradorValido, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Empty(ctx.Usuarios.All);
    }

    [Fact]
    public async Task Should_Fail_Closed_When_BootstrapEstado_Row_Is_Missing()
    {
        var (useCase, ctx) = Arrange(estadoAusente: true);
        var sessaoId = ctx.AdicionarSessaoValida("admin.inicial@example.invalid");

        var resultado = await useCase.ExecuteAsync(sessaoId, NovaUnidade, AdministradorValido, CancellationToken.None);

        Assert.False(resultado.Sucesso);
    }

    [Fact]
    public async Task Should_Reject_When_BootstrapSessao_Is_Unknown()
    {
        var (useCase, _) = Arrange();

        var resultado = await useCase.ExecuteAsync(Guid.NewGuid(), NovaUnidade, AdministradorValido, CancellationToken.None);

        Assert.False(resultado.Sucesso);
    }

    [Fact]
    public async Task Should_Reject_When_BootstrapSessao_Already_Used()
    {
        var (useCase, ctx) = Arrange();
        var sessaoId = ctx.AdicionarSessaoValida("admin.inicial@example.invalid");
        ctx.Sessoes.All.Single().MarcarUsada(DateTimeOffset.UtcNow);

        var resultado = await useCase.ExecuteAsync(sessaoId, NovaUnidade, AdministradorValido, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Empty(ctx.Usuarios.All);
    }

    [Fact]
    public async Task Should_Reject_Empty_Administrador_Nome()
    {
        var (useCase, ctx) = Arrange();
        var sessaoId = ctx.AdicionarSessaoValida("admin.inicial@example.invalid");

        var resultado = await useCase.ExecuteAsync(sessaoId, NovaUnidade, new AdministradorSeniorBootstrapPayload(""), CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Empty(ctx.Usuarios.All);
    }

    [Fact]
    public async Task Should_Never_Trust_Payload_Email_Only_The_Validated_BootstrapSessao_Email()
    {
        var (useCase, ctx) = Arrange();
        var sessaoId = ctx.AdicionarSessaoValida("validado-por-otp@example.invalid");

        var resultado = await useCase.ExecuteAsync(sessaoId, NovaUnidade, AdministradorValido, CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.Equal("validado-por-otp@example.invalid", resultado.Email);
        Assert.Equal("validado-por-otp@example.invalid", ctx.Usuarios.All.Single().Email);
    }

    [Fact]
    public async Task Should_Reject_Reuse_Of_UnidadeNegocio_That_Already_Has_Active_AdministradorSenior()
    {
        var (useCase, ctx) = Arrange();
        var existente = new UnidadeNegocio("Filial Existente", "filial-existente");
        ctx.UnidadesNegocio.All.Add(existente);
        ctx.UnidadesNegocio.ComAdministradorSeniorAtivo.Add(existente.Id);
        var sessaoId = ctx.AdicionarSessaoValida("admin.inicial@example.invalid");

        var resultado = await useCase.ExecuteAsync(
            sessaoId, new UnidadeNegocioBootstrapPayload(existente.Id, null, null), AdministradorValido, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Empty(ctx.Usuarios.All);
    }

    [Fact]
    public async Task Should_Reuse_UnidadeNegocio_Without_Active_AdministradorSenior()
    {
        var (useCase, ctx) = Arrange();
        var existente = new UnidadeNegocio("Filial Sem Admin", "filial-sem-admin");
        ctx.UnidadesNegocio.All.Add(existente);
        var sessaoId = ctx.AdicionarSessaoValida("admin.inicial@example.invalid");

        var resultado = await useCase.ExecuteAsync(
            sessaoId, new UnidadeNegocioBootstrapPayload(existente.Id, null, null), AdministradorValido, CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.Equal(existente.Id, resultado.UnidadeNegocioId);
        Assert.Single(ctx.UnidadesNegocio.All); // nenhuma nova UnidadeNegocio criada — reaproveitada.
    }

    [Fact]
    public async Task Should_Reuse_Existing_AdministradorSenior_Perfil_Instead_Of_Duplicating()
    {
        var (useCase, ctx) = Arrange();
        var sessaoId = ctx.AdicionarSessaoValida("admin.inicial@example.invalid");

        // Simula um Perfil "Administrador Sênior" já existente em uma Unidade de Negócio já existente (ex.:
        // criado por uma etapa anterior de gestão de Perfis) — a conclusão deve reaproveitar, não duplicar.
        var buExistente = new UnidadeNegocio("Filial com Perfil Pronto", "filial-com-perfil-pronto");
        ctx.UnidadesNegocio.All.Add(buExistente);
        var perfilExistente = new Perfil(Perfil.AdministradorSenior, "Perfil administrativo pré-existente.", buExistente.Id, DateTimeOffset.UtcNow);
        ctx.Perfis.All.Add(perfilExistente);

        var resultado = await useCase.ExecuteAsync(
            sessaoId, new UnidadeNegocioBootstrapPayload(buExistente.Id, null, null), AdministradorValido, CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.Single(ctx.Perfis.All); // nenhum novo Perfil criado — o existente foi reaproveitado.
        Assert.Equal(perfilExistente.Id, ctx.UsuariosPerfis.All.Single().PerfilId);
    }

    private static void MarcarConcluido(BootstrapEstado estado) =>
        typeof(BootstrapEstado).GetProperty(nameof(BootstrapEstado.Concluido))!.SetValue(estado, true);

    private static (ConcluirBootstrapUseCase UseCase, FakeContext Ctx) Arrange(bool estadoAusente = false)
    {
        var ctx = new FakeContext(estadoAusente);
        var useCase = new ConcluirBootstrapUseCase(
            ctx.Estados, ctx.Sessoes, ctx.UnidadesNegocio, ctx.Usuarios, ctx.Perfis, ctx.Perfis.Permissoes,
            ctx.UsuariosPerfis, TimeProvider.System, NullLogger<ConcluirBootstrapUseCase>.Instance);
        return (useCase, ctx);
    }

    private sealed class FakeContext(bool estadoAusente)
    {
        public FakeBootstrapEstadoRepositoryConclusao Estados { get; } = new(estadoAusente);
        public FakeBootstrapSessaoRepositoryConclusao Sessoes { get; } = new();
        public FakeUnidadeNegocioRepository UnidadesNegocio { get; } = new();
        public FakeUsuarioRepositoryConclusao Usuarios { get; } = new();
        public FakePerfilRepository Perfis { get; } = new();
        public FakeUsuarioPerfilRepository UsuariosPerfis { get; } = new();

        public Guid AdicionarSessaoValida(string emailCandidato)
        {
            var sessao = new BootstrapSessao(emailCandidato, Guid.NewGuid().ToString(), DateTimeOffset.UtcNow);
            Sessoes.All.Add(sessao);
            return sessao.Id;
        }
    }

    private sealed class FakeBootstrapEstadoRepositoryConclusao(bool estadoAusente) : IBootstrapEstadoRepository
    {
        public BootstrapEstado? Estado { get; } = estadoAusente ? null : BootstrapEstado.CriarInicial();

        public Task<BootstrapEstado?> ObterAsync(CancellationToken ct) => Task.FromResult(Estado);
        public Task AtualizarAsync(BootstrapEstado estado, CancellationToken ct) => Task.CompletedTask;
        public Task SalvarAlteracoesAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeBootstrapSessaoRepositoryConclusao : IBootstrapSessaoRepository
    {
        public List<BootstrapSessao> All { get; } = [];

        public Task<BootstrapSessao?> ObterPorIdentificadorHashAsync(string identificadorHash, CancellationToken ct) =>
            Task.FromResult(All.SingleOrDefault(x => x.IdentificadorHash == identificadorHash));

        public Task<BootstrapSessao?> ObterAtivaPorEmailCandidatoAsync(string emailCandidato, CancellationToken ct) =>
            Task.FromResult(All.Where(x => x.EmailCandidato == emailCandidato && x.UsadaEm == null && x.RevokedAt == null)
                .OrderByDescending(x => x.CreatedAt).FirstOrDefault());

        public Task<BootstrapSessao?> ObterPorIdAsync(Guid id, CancellationToken ct) =>
            Task.FromResult(All.SingleOrDefault(x => x.Id == id));

        public Task AdicionarAsync(BootstrapSessao sessao, CancellationToken ct) { All.Add(sessao); return Task.CompletedTask; }
        public Task AtualizarAsync(BootstrapSessao sessao, CancellationToken ct) => Task.CompletedTask;
        public Task SalvarAlteracoesAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeUnidadeNegocioRepository : IUnidadeNegocioRepository
    {
        public List<UnidadeNegocio> All { get; } = [];
        public HashSet<Guid> ComAdministradorSeniorAtivo { get; } = [];

        public Task<UnidadeNegocio?> ObterPorIdAsync(Guid id, CancellationToken ct) =>
            Task.FromResult(All.SingleOrDefault(x => x.Id == id));

        public Task<bool> PossuiAdministradorSeniorAtivoAsync(Guid unidadeNegocioId, CancellationToken ct) =>
            Task.FromResult(ComAdministradorSeniorAtivo.Contains(unidadeNegocioId));

        public Task AdicionarAsync(UnidadeNegocio unidadeNegocio, CancellationToken ct) { All.Add(unidadeNegocio); return Task.CompletedTask; }

        public Task<IReadOnlyList<UnidadeNegocio>> ListarTodasAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<UnidadeNegocio>>(All);

        public Task<bool> ExisteComSlugAsync(string slug, Guid? excluirId, CancellationToken ct) =>
            Task.FromResult(All.Any(x => x.Slug == slug && (excluirId == null || x.Id != excluirId)));

        public Task SalvarAlteracoesAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeUsuarioRepositoryConclusao : IUsuarioRepository
    {
        public List<Usuario> All { get; } = [];

        public Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken ct) =>
            Task.FromResult(All.SingleOrDefault(x => x.Email == email));

        public Task<Usuario?> ObterPorIdAsync(Guid id, CancellationToken ct) =>
            Task.FromResult(All.SingleOrDefault(x => x.Id == id));

        public Task AdicionarAsync(Usuario usuario, CancellationToken ct) { All.Add(usuario); return Task.CompletedTask; }

        // Membros da O1.6 (Gestão de Usuários) não exercitados pelos testes de conclusão do Bootstrap.
        public Task<IReadOnlyList<Usuario>> ListarPorUnidadeNegocioAsync(Guid unidadeNegocioId, CancellationToken ct) =>
            throw new NotSupportedException();
        public Task<Usuario?> ObterPorIdEUnidadeNegocioAsync(Guid id, Guid unidadeNegocioId, CancellationToken ct) =>
            throw new NotSupportedException();
        public Task<Usuario?> ObterPorEmailEUnidadeNegocioAsync(string email, Guid unidadeNegocioId, CancellationToken ct) =>
            throw new NotSupportedException();
        public Task<IReadOnlyDictionary<Guid, IReadOnlyList<BlueprintOS.Application.Identity.Models.UsuarioPerfilResumoDto>>> ObterPerfisPorUsuarioAsync(
            IReadOnlyCollection<Guid> usuarioIds, CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyDictionary<Guid, IReadOnlyList<string>>> ObterCentrosCustoPorUsuarioAsync(
            IReadOnlyCollection<Guid> usuarioIds, CancellationToken ct) => throw new NotSupportedException();
        public Task SubstituirPerfisAsync(Guid usuarioId, IReadOnlyCollection<Guid> perfilIds, CancellationToken ct) =>
            throw new NotSupportedException();
        public Task SubstituirCentrosCustoAsync(Guid usuarioId, IReadOnlyCollection<string> codigosErp, CancellationToken ct) =>
            throw new NotSupportedException();
        public Task<int> ContarAdministradoresSeniorAtivosAsync(Guid unidadeNegocioId, Guid? excluirUsuarioId, CancellationToken ct) =>
            throw new NotSupportedException();
        public Task SalvarAlteracoesAsync(CancellationToken ct) => throw new NotSupportedException();
    }

    private sealed class FakeUsuarioPerfilRepository : IUsuarioPerfilRepository
    {
        public List<UsuarioPerfil> All { get; } = [];

        public Task AdicionarAsync(UsuarioPerfil vinculo, CancellationToken ct) { All.Add(vinculo); return Task.CompletedTask; }
    }
}
