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
}
