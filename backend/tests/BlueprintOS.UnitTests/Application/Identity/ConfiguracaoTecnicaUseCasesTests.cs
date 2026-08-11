using BlueprintOS.Application.Identity;
using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Domain.Identity;
using Microsoft.Extensions.Logging.Abstractions;

namespace BlueprintOS.UnitTests.Application.Identity;

/// <summary>O1.11 — Identity Providers e Configuração de ERP por Unidade de Negócio. Cobre: 404 quando a
/// Unidade de Negócio referenciada não existe, e a garantia central de segurança — o segredo em claro
/// enviado na requisição NUNCA aparece na projeção de leitura devolvida pela API (apenas
/// <c>ParametrosConfigurados: bool</c>).</summary>
public sealed class ConfiguracaoTecnicaUseCasesTests
{
    private static readonly Guid Bu = Guid.NewGuid();

    private sealed class FakeSegredoProtector : ISegredoProtector
    {
        public List<string> ValoresProtegidos { get; } = [];

        public string Proteger(string valorEmClaro)
        {
            ValoresProtegidos.Add(valorEmClaro);
            return $"CIFRADO::{valorEmClaro}";
        }

        public string Desproteger(string valorProtegido) => valorProtegido.Replace("CIFRADO::", string.Empty);
    }

    private sealed class FakeUnidadeNegocioRepositoryMinimo : IUnidadeNegocioRepository
    {
        public List<UnidadeNegocio> All { get; } = [];

        public Task<UnidadeNegocio?> ObterPorIdAsync(Guid id, CancellationToken ct) => Task.FromResult(All.SingleOrDefault(x => x.Id == id));
        public Task<bool> PossuiAdministradorSeniorAtivoAsync(Guid unidadeNegocioId, CancellationToken ct) => Task.FromResult(false);
        public Task AdicionarAsync(UnidadeNegocio unidadeNegocio, CancellationToken ct) { All.Add(unidadeNegocio); return Task.CompletedTask; }
        public Task<IReadOnlyList<UnidadeNegocio>> ListarTodasAsync(CancellationToken ct) => Task.FromResult((IReadOnlyList<UnidadeNegocio>)All);
        public Task<bool> ExisteComSlugAsync(string slug, Guid? excluirId, CancellationToken ct) => Task.FromResult(false);
        public Task SalvarAlteracoesAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeIdentityProviderRepository : IIdentityProviderRepository
    {
        public List<IdentityProvider> All { get; } = [];

        public Task<IReadOnlyList<IdentityProvider>> ListarPorUnidadeNegocioAsync(Guid unidadeNegocioId, CancellationToken ct) =>
            Task.FromResult((IReadOnlyList<IdentityProvider>)All.Where(x => x.UnidadeNegocioId == unidadeNegocioId).ToArray());

        public Task<IdentityProvider?> ObterPorIdEUnidadeNegocioAsync(Guid id, Guid unidadeNegocioId, CancellationToken ct) =>
            Task.FromResult(All.SingleOrDefault(x => x.Id == id && x.UnidadeNegocioId == unidadeNegocioId));

        public Task AdicionarAsync(IdentityProvider identityProvider, CancellationToken ct) { All.Add(identityProvider); return Task.CompletedTask; }
        public Task SalvarAlteracoesAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeConfiguracaoErpRepository : IConfiguracaoErpRepository
    {
        public List<ConfiguracaoErp> All { get; } = [];

        public Task<ConfiguracaoErp?> ObterPorUnidadeNegocioAsync(Guid unidadeNegocioId, CancellationToken ct) =>
            Task.FromResult(All.SingleOrDefault(x => x.UnidadeNegocioId == unidadeNegocioId));

        public Task AdicionarAsync(ConfiguracaoErp configuracaoErp, CancellationToken ct) { All.Add(configuracaoErp); return Task.CompletedTask; }
        public Task SalvarAlteracoesAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private static (FakeUnidadeNegocioRepositoryMinimo unidades, Guid buId) ArrangeUnidade()
    {
        var repo = new FakeUnidadeNegocioRepositoryMinimo();
        var unidade = new UnidadeNegocio("SOMA", "soma");
        repo.All.Add(unidade);
        return (repo, unidade.Id);
    }

    [Fact]
    public async Task CriarIdentityProvider_Should_Return_NotFound_When_UnidadeNegocio_Does_Not_Exist()
    {
        var unidades = new FakeUnidadeNegocioRepositoryMinimo();
        var useCase = new CriarIdentityProviderUseCase(
            unidades, new FakeIdentityProviderRepository(), new FakeSegredoProtector(), TimeProvider.System,
            NullLogger<CriarIdentityProviderUseCase>.Instance);

        var resultado = await useCase.ExecuteAsync(Guid.NewGuid(), new IdentityProviderInput("MicrosoftEntraId", null, "segredo"), CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.UnidadeNegocioNaoEncontrada, resultado.Falha);
    }

    [Fact]
    public async Task CriarIdentityProvider_Should_Never_Expose_Raw_Secret()
    {
        var (unidades, buId) = ArrangeUnidade();
        var protector = new FakeSegredoProtector();
        var useCase = new CriarIdentityProviderUseCase(
            unidades, new FakeIdentityProviderRepository(), protector, TimeProvider.System,
            NullLogger<CriarIdentityProviderUseCase>.Instance);

        const string segredo = "client-secret-super-sensivel";
        var resultado = await useCase.ExecuteAsync(buId, new IdentityProviderInput("MicrosoftEntraId", ["soma.com.br"], segredo), CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.True(resultado.Valor!.ParametrosConfigurados);
        Assert.Contains(segredo, protector.ValoresProtegidos);

        // A projeção de leitura (o que a API devolve) não possui em nenhum lugar o segredo em claro.
        var serializado = System.Text.Json.JsonSerializer.Serialize(resultado.Valor);
        Assert.DoesNotContain(segredo, serializado);
    }

    [Fact]
    public async Task SalvarConfiguracaoErp_Should_Never_Expose_Raw_Secret_And_Preserve_On_Edit_Without_Reenvio()
    {
        var (unidades, buId) = ArrangeUnidade();
        var protector = new FakeSegredoProtector();
        var repo = new FakeConfiguracaoErpRepository();
        var useCase = new SalvarConfiguracaoErpUseCase(
            unidades, repo, protector, TimeProvider.System, NullLogger<SalvarConfiguracaoErpUseCase>.Instance);

        const string segredo = "connection-string-sensivel";
        var criado = await useCase.ExecuteAsync(buId, new ConfiguracaoErpInput("Linx", segredo), CancellationToken.None);
        Assert.True(criado.Sucesso);
        Assert.True(criado.Valor!.ParametrosConfigurados);

        var serializado = System.Text.Json.JsonSerializer.Serialize(criado.Valor);
        Assert.DoesNotContain(segredo, serializado);

        var protegidoAposCriacao = repo.All[0].ParametrosConexaoProtegidos;

        // Edição sem reenviar o segredo (SistemaErp apenas) preserva o valor cifrado já salvo.
        var editado = await useCase.ExecuteAsync(buId, new ConfiguracaoErpInput("Linx", null), CancellationToken.None);
        Assert.True(editado.Sucesso);
        Assert.True(editado.Valor!.ParametrosConfigurados);
        Assert.Equal(protegidoAposCriacao, repo.All[0].ParametrosConexaoProtegidos);
    }

    [Fact]
    public async Task SalvarConfiguracaoErp_Should_Return_NotFound_When_UnidadeNegocio_Does_Not_Exist()
    {
        var unidades = new FakeUnidadeNegocioRepositoryMinimo();
        var useCase = new SalvarConfiguracaoErpUseCase(
            unidades, new FakeConfiguracaoErpRepository(), new FakeSegredoProtector(), TimeProvider.System,
            NullLogger<SalvarConfiguracaoErpUseCase>.Instance);

        var resultado = await useCase.ExecuteAsync(Guid.NewGuid(), new ConfiguracaoErpInput("Linx", "x"), CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.UnidadeNegocioNaoEncontrada, resultado.Falha);
    }

    // ---- O1.11, item #24 — Configuração de Notificações por Unidade de Negócio ----

    private sealed class FakeConfiguracaoNotificacaoRepository : IConfiguracaoNotificacaoRepository
    {
        public List<ConfiguracaoNotificacao> All { get; } = [];

        public Task<ConfiguracaoNotificacao?> ObterPorUnidadeNegocioAsync(Guid unidadeNegocioId, CancellationToken ct) =>
            Task.FromResult(All.SingleOrDefault(x => x.UnidadeNegocioId == unidadeNegocioId));

        public Task AdicionarAsync(ConfiguracaoNotificacao configuracaoNotificacao, CancellationToken ct) { All.Add(configuracaoNotificacao); return Task.CompletedTask; }
        public Task SalvarAlteracoesAsync(CancellationToken ct) => Task.CompletedTask;
    }

    [Fact]
    public async Task ObterConfiguracaoNotificacao_Should_Return_NotFound_When_UnidadeNegocio_Does_Not_Exist()
    {
        var unidades = new FakeUnidadeNegocioRepositoryMinimo();
        var useCase = new ObterConfiguracaoNotificacaoUseCase(unidades, new FakeConfiguracaoNotificacaoRepository());

        var resultado = await useCase.ExecuteAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.UnidadeNegocioNaoEncontrada, resultado.Falha);
    }

    [Fact]
    public async Task ObterConfiguracaoNotificacao_Should_Return_Null_When_Not_Yet_Configured()
    {
        var (unidades, buId) = ArrangeUnidade();
        var useCase = new ObterConfiguracaoNotificacaoUseCase(unidades, new FakeConfiguracaoNotificacaoRepository());

        var resultado = await useCase.ExecuteAsync(buId, CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.Null(resultado.Valor);
    }

    [Fact]
    public async Task SalvarConfiguracaoNotificacao_Should_Return_NotFound_When_UnidadeNegocio_Does_Not_Exist()
    {
        var unidades = new FakeUnidadeNegocioRepositoryMinimo();
        var useCase = new SalvarConfiguracaoNotificacaoUseCase(
            unidades, new FakeConfiguracaoNotificacaoRepository(), TimeProvider.System, NullLogger<SalvarConfiguracaoNotificacaoUseCase>.Instance);

        var resultado = await useCase.ExecuteAsync(Guid.NewGuid(), new ConfiguracaoNotificacaoInput(false, null, null), CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.UnidadeNegocioNaoEncontrada, resultado.Falha);
    }

    [Fact]
    public async Task SalvarConfiguracaoNotificacao_Should_Create_With_Safe_Defaults_When_Disabled_And_No_Email()
    {
        var (unidades, buId) = ArrangeUnidade();
        var repo = new FakeConfiguracaoNotificacaoRepository();
        var useCase = new SalvarConfiguracaoNotificacaoUseCase(
            unidades, repo, TimeProvider.System, NullLogger<SalvarConfiguracaoNotificacaoUseCase>.Instance);

        var resultado = await useCase.ExecuteAsync(buId, new ConfiguracaoNotificacaoInput(false, null, null), CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.False(resultado.Valor!.EmailAtivado);
        Assert.Null(resultado.Valor.EmailRemetente);
        Assert.Single(repo.All);
    }

    [Fact]
    public async Task SalvarConfiguracaoNotificacao_Should_Reject_Activation_Without_Sender_Email()
    {
        var (unidades, buId) = ArrangeUnidade();
        var useCase = new SalvarConfiguracaoNotificacaoUseCase(
            unidades, new FakeConfiguracaoNotificacaoRepository(), TimeProvider.System, NullLogger<SalvarConfiguracaoNotificacaoUseCase>.Instance);

        var resultado = await useCase.ExecuteAsync(buId, new ConfiguracaoNotificacaoInput(true, null, "SOMA"), CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.EmailRemetenteInvalido, resultado.Falha);
    }

    [Fact]
    public async Task SalvarConfiguracaoNotificacao_Should_Reject_Invalid_Email_Format()
    {
        var (unidades, buId) = ArrangeUnidade();
        var useCase = new SalvarConfiguracaoNotificacaoUseCase(
            unidades, new FakeConfiguracaoNotificacaoRepository(), TimeProvider.System, NullLogger<SalvarConfiguracaoNotificacaoUseCase>.Instance);

        var resultado = await useCase.ExecuteAsync(buId, new ConfiguracaoNotificacaoInput(true, "nao-e-um-email", "SOMA"), CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.EmailRemetenteInvalido, resultado.Falha);
    }

    [Fact]
    public async Task SalvarConfiguracaoNotificacao_Should_Activate_With_Valid_Email_And_Persist_Sender_Name()
    {
        var (unidades, buId) = ArrangeUnidade();
        var repo = new FakeConfiguracaoNotificacaoRepository();
        var useCase = new SalvarConfiguracaoNotificacaoUseCase(
            unidades, repo, TimeProvider.System, NullLogger<SalvarConfiguracaoNotificacaoUseCase>.Instance);

        var resultado = await useCase.ExecuteAsync(buId, new ConfiguracaoNotificacaoInput(true, "Notificacoes@Soma.Com.Br", "SOMA Grupo"), CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.True(resultado.Valor!.EmailAtivado);
        Assert.Equal("notificacoes@soma.com.br", resultado.Valor.EmailRemetente);
        Assert.Equal("SOMA Grupo", resultado.Valor.NomeRemetente);
    }

    [Fact]
    public async Task SalvarConfiguracaoNotificacao_Should_Update_Existing_Configuration_Idempotently()
    {
        var (unidades, buId) = ArrangeUnidade();
        var repo = new FakeConfiguracaoNotificacaoRepository();
        var useCase = new SalvarConfiguracaoNotificacaoUseCase(
            unidades, repo, TimeProvider.System, NullLogger<SalvarConfiguracaoNotificacaoUseCase>.Instance);

        var criado = await useCase.ExecuteAsync(buId, new ConfiguracaoNotificacaoInput(true, "a@soma.com.br", "A"), CancellationToken.None);
        Assert.True(criado.Sucesso);
        Assert.Single(repo.All);

        var atualizado = await useCase.ExecuteAsync(buId, new ConfiguracaoNotificacaoInput(false, "a@soma.com.br", "B"), CancellationToken.None);

        Assert.True(atualizado.Sucesso);
        Assert.Single(repo.All);
        Assert.Equal(criado.Valor!.Id, atualizado.Valor!.Id);
        Assert.False(atualizado.Valor.EmailAtivado);
        Assert.Equal("B", atualizado.Valor.NomeRemetente);
    }

    [Fact]
    public async Task SalvarConfiguracaoNotificacao_Should_Isolate_Configurations_Between_Business_Units()
    {
        var repoUnidades = new FakeUnidadeNegocioRepositoryMinimo();
        var unidadeA = new UnidadeNegocio("SOMA A", "soma-a");
        var unidadeB = new UnidadeNegocio("SOMA B", "soma-b");
        repoUnidades.All.Add(unidadeA);
        repoUnidades.All.Add(unidadeB);

        var repo = new FakeConfiguracaoNotificacaoRepository();
        var useCase = new SalvarConfiguracaoNotificacaoUseCase(
            repoUnidades, repo, TimeProvider.System, NullLogger<SalvarConfiguracaoNotificacaoUseCase>.Instance);

        await useCase.ExecuteAsync(unidadeA.Id, new ConfiguracaoNotificacaoInput(true, "a@soma.com.br", "A"), CancellationToken.None);
        await useCase.ExecuteAsync(unidadeB.Id, new ConfiguracaoNotificacaoInput(true, "b@soma.com.br", "B"), CancellationToken.None);

        Assert.Equal(2, repo.All.Count);

        var obterUseCase = new ObterConfiguracaoNotificacaoUseCase(repoUnidades, repo);
        var configA = await obterUseCase.ExecuteAsync(unidadeA.Id, CancellationToken.None);
        var configB = await obterUseCase.ExecuteAsync(unidadeB.Id, CancellationToken.None);

        Assert.Equal("a@soma.com.br", configA.Valor!.EmailRemetente);
        Assert.Equal("b@soma.com.br", configB.Valor!.EmailRemetente);
        Assert.NotEqual(configA.Valor.Id, configB.Valor.Id);
    }
}
