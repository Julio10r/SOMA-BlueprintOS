using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Domain.Identity;
using BlueprintOS.Infrastructure.Administration;
using BlueprintOS.Infrastructure.Integrations.ERP.Contracts;

namespace BlueprintOS.UnitTests.Application.Identity;

/// <summary>B3 — Bloco 1: Conta Contábil, cadastro de apoio importado do Linx (Discovery homologado). Cobre:
/// listagem combinando ERP + metadados locais ("ativo por padrão sem metadado local"), criação do metadado
/// na primeira edição, atualização do existente, rejeição de código ERP inexistente, e a regra distintiva
/// deste cadastro (`ADR-0024`): uma conta inativa no Linx nunca aparece efetivamente ativa no +Compras,
/// mesmo com metadado local marcado como ativo.</summary>
public sealed class ContaContabilUseCasesTests
{
    private static readonly Guid Bu = Guid.NewGuid();

    private sealed class FakeContaContabilErpReader : IContaContabilErpReader
    {
        public List<ContaContabilErpDto> Contas { get; } = [];

        public Task<IReadOnlyList<ContaContabilErpDto>> BuscarContasContabeisAsync(int skip, int take, CancellationToken ct) =>
            Task.FromResult((IReadOnlyList<ContaContabilErpDto>)Contas.Skip(skip).Take(take).ToArray());

        public Task<ContaContabilErpDto?> BuscarPorCodigoAsync(string codigoErp, CancellationToken ct) =>
            Task.FromResult(Contas.FirstOrDefault(x => x.CodigoErp == codigoErp));
    }

    private sealed class FakeContaContabilMetadadoRepository : IContaContabilMetadadoRepository
    {
        public List<ContaContabilMetadado> Registros { get; } = [];

        public Task<IReadOnlyDictionary<string, ContaContabilMetadado>> ListarPorUnidadeNegocioAsync(Guid unidadeNegocioId, CancellationToken ct) =>
            Task.FromResult((IReadOnlyDictionary<string, ContaContabilMetadado>)Registros
                .Where(x => x.UnidadeNegocioId == unidadeNegocioId)
                .ToDictionary(x => x.CodigoErp, StringComparer.OrdinalIgnoreCase));

        public Task<ContaContabilMetadado?> ObterPorCodigoErpAsync(string codigoErp, Guid unidadeNegocioId, CancellationToken ct) =>
            Task.FromResult(Registros.SingleOrDefault(x => x.CodigoErp == codigoErp && x.UnidadeNegocioId == unidadeNegocioId));

        public Task AdicionarAsync(ContaContabilMetadado metadado, CancellationToken ct)
        {
            Registros.Add(metadado);
            return Task.CompletedTask;
        }

        public Task SalvarAlteracoesAsync(CancellationToken ct) => Task.CompletedTask;
    }

    [Fact]
    public async Task ListarContasContabeis_Should_Combine_Erp_With_Local_Metadata_And_Default_To_Active()
    {
        var reader = new FakeContaContabilErpReader();
        reader.Contas.Add(new ContaContabilErpDto("1.1.01", "Caixa", false, DateTimeOffset.UtcNow));
        reader.Contas.Add(new ContaContabilErpDto("1.1.02", "Bancos", false, DateTimeOffset.UtcNow));
        var metadados = new FakeContaContabilMetadadoRepository();
        metadados.Registros.Add(new ContaContabilMetadado("1.1.02", Bu, DateTimeOffset.UtcNow, "Conta corrente principal", false));

        var useCase = new ListarContasContabeisUseCase(reader, metadados);
        var resultado = await useCase.ExecuteAsync(Bu, CancellationToken.None);

        Assert.Equal(2, resultado.Count);
        var semMetadado = resultado.Single(x => x.CodigoErp == "1.1.01");
        Assert.True(semMetadado.AtivoNoMaisCompras);
        Assert.True(semMetadado.AtivoEfetivo);
        Assert.False(semMetadado.TemMetadadoLocal);

        var comMetadado = resultado.Single(x => x.CodigoErp == "1.1.02");
        Assert.False(comMetadado.AtivoNoMaisCompras);
        Assert.False(comMetadado.AtivoEfetivo);
        Assert.Equal("Conta corrente principal", comMetadado.DescricaoMaisCompras);
        Assert.True(comMetadado.TemMetadadoLocal);
    }

    [Fact]
    public async Task ListarContasContabeis_Should_Never_Show_Active_When_Erp_Marks_It_Inactive_Even_With_Local_Metadata_Active()
    {
        // ADR-0024: em ambiguidade/conflito de autoridade, o Linx prevalece — o +Compras só pode ser MAIS
        // restritivo (inativar localmente algo que o Linx ainda considera ativo), nunca reativar algo que o
        // Linx já marcou como inativo.
        var reader = new FakeContaContabilErpReader();
        reader.Contas.Add(new ContaContabilErpDto("2.9.99", "Conta Encerrada", true, DateTimeOffset.UtcNow));
        var metadados = new FakeContaContabilMetadadoRepository();
        metadados.Registros.Add(new ContaContabilMetadado("2.9.99", Bu, DateTimeOffset.UtcNow, ativoNoMaisCompras: true));

        var useCase = new ListarContasContabeisUseCase(reader, metadados);
        var resultado = await useCase.ExecuteAsync(Bu, CancellationToken.None);

        var item = Assert.Single(resultado);
        Assert.True(item.InativaNoErp);
        Assert.True(item.AtivoNoMaisCompras);
        Assert.False(item.AtivoEfetivo);
    }

    [Fact]
    public async Task AtualizarMetadadoContaContabil_Should_Create_On_First_Edit_And_Update_After()
    {
        var reader = new FakeContaContabilErpReader();
        reader.Contas.Add(new ContaContabilErpDto("1.1.01", "Caixa", false, null));
        var metadados = new FakeContaContabilMetadadoRepository();
        var useCase = new AtualizarMetadadoContaContabilUseCase(reader, metadados, TimeProvider.System);

        var primeira = await useCase.ExecuteAsync("1.1.01", new ContaContabilMetadadoInput("Caixa da matriz", false), Bu, CancellationToken.None);
        Assert.True(primeira.Sucesso);
        Assert.Single(metadados.Registros);
        Assert.Equal("Caixa da matriz", primeira.Valor!.DescricaoMaisCompras);
        Assert.False(primeira.Valor.AtivoNoMaisCompras);

        var segunda = await useCase.ExecuteAsync("1.1.01", new ContaContabilMetadadoInput("Outra descrição", true), Bu, CancellationToken.None);
        Assert.True(segunda.Sucesso);
        Assert.Single(metadados.Registros);
        Assert.Equal("Outra descrição", segunda.Valor!.DescricaoMaisCompras);
        Assert.True(segunda.Valor.AtivoNoMaisCompras);
    }

    [Fact]
    public async Task AtualizarMetadadoContaContabil_Should_Reject_Unknown_Erp_Code()
    {
        var useCase = new AtualizarMetadadoContaContabilUseCase(new FakeContaContabilErpReader(), new FakeContaContabilMetadadoRepository(), TimeProvider.System);

        var resultado = await useCase.ExecuteAsync("9.9.99", new ContaContabilMetadadoInput(null, true), Bu, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(ErpMetadadoFalha.CodigoErpNaoEncontrado, resultado.Falha);
    }
}
