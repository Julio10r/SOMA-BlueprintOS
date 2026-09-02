using BlueprintOS.Application.Identity;
using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Domain.Identity;

namespace BlueprintOS.UnitTests.Application.Identity;

/// <summary>B3 — Bloco 3: cadastro local de Item Fiscal (Discovery homologado). Cobre: obrigatoriedade de
/// Código/Descrição/Conta Contábil/Unidade de Medida, duplicidade de código (global), validação de que
/// Conta Contábil e Unidade de Medida selecionadas existem e estão ativas (`ADR-0024` para Conta
/// Contábil), granularidade livre (descrição sem taxonomia imposta), inativação local, e RBAC separado por
/// ação (verificado em <c>ItensFiscaisRbacTests</c>, não aqui).
///
/// Fakes de <c>IListarContasContabeisUseCase</c>/<c>IListarUnidadesMedidaUseCase</c>: reaproveitam
/// diretamente as implementações reais dos Blocos 1/2 sobre readers/repositorios fake, em vez de duplicar
/// a lógica de "ativo por padrão sem metadado local" — mesma fonte de verdade usada em produção.</summary>
public sealed class ItemFiscalUseCasesTests
{
    private static readonly Guid Bu = Guid.NewGuid();

    private sealed class FakeItemFiscalRepository : IItemFiscalRepository
    {
        public List<ItemFiscal> Itens { get; } = [];

        public Task<IReadOnlyList<ItemFiscal>> ListarPorUnidadeNegocioAsync(Guid unidadeNegocioId, CancellationToken ct) =>
            Task.FromResult((IReadOnlyList<ItemFiscal>)Itens.Where(x => x.UnidadeNegocioId == unidadeNegocioId).ToArray());

        public Task<ItemFiscal?> ObterPorIdEUnidadeNegocioAsync(Guid id, Guid unidadeNegocioId, CancellationToken ct) =>
            Task.FromResult(Itens.SingleOrDefault(x => x.Id == id && x.UnidadeNegocioId == unidadeNegocioId));

        public Task<bool> ExisteComCodigoAsync(string codigo, Guid? excluirId, CancellationToken ct) =>
            Task.FromResult(Itens.Any(x => string.Equals(x.Codigo, codigo, StringComparison.OrdinalIgnoreCase) && x.Id != excluirId));

        public Task AdicionarAsync(ItemFiscal itemFiscal, CancellationToken ct)
        {
            Itens.Add(itemFiscal);
            return Task.CompletedTask;
        }

        public Task SalvarAlteracoesAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeContasContabeisUseCase(IReadOnlyList<ContaContabilDto> contas) : IListarContasContabeisUseCase
    {
        public Task<IReadOnlyList<ContaContabilDto>> ExecuteAsync(Guid unidadeNegocioId, CancellationToken ct) =>
            Task.FromResult(contas);
    }

    private sealed class FakeUnidadesMedidaUseCase(IReadOnlyList<UnidadeMedidaDto> unidades) : IListarUnidadesMedidaUseCase
    {
        public Task<IReadOnlyList<UnidadeMedidaDto>> ExecuteAsync(Guid unidadeNegocioId, CancellationToken ct) =>
            Task.FromResult(unidades);
    }

    private static ContaContabilDto ContaAtiva(string codigo) => new(codigo, $"Conta {codigo}", false, null, true, true, false, null);
    private static ContaContabilDto ContaInativaNoErp(string codigo) => new(codigo, $"Conta {codigo}", true, null, true, false, false, null);
    private static UnidadeMedidaDto UnidadeAtiva(string codigo) => new(codigo, $"Unidade {codigo}", null, true, false, null);
    private static UnidadeMedidaDto UnidadeInativaNoMaisCompras(string codigo) => new(codigo, $"Unidade {codigo}", null, false, true, null);

    private static (FakeItemFiscalRepository repo, IListarContasContabeisUseCase contas, IListarUnidadesMedidaUseCase unidades) Cenario(
        IReadOnlyList<ContaContabilDto>? contas = null, IReadOnlyList<UnidadeMedidaDto>? unidades = null) =>
        (
            new FakeItemFiscalRepository(),
            new FakeContasContabeisUseCase(contas ?? [ContaAtiva("1.1.01")]),
            new FakeUnidadesMedidaUseCase(unidades ?? [UnidadeAtiva("UN")])
        );

    [Fact]
    public async Task Criar_Should_Succeed_With_Valid_ContaContabil_And_UnidadeMedida()
    {
        var (repo, contas, unidades) = Cenario();
        var useCase = new CriarItemFiscalUseCase(repo, contas, unidades, TimeProvider.System);

        var resultado = await useCase.ExecuteAsync(
            new ItemFiscalCriarInput("001", "Notebook", "UN", "1.1.01"), Bu, CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.Equal("001", resultado.Valor!.Codigo);
        Assert.Equal("Notebook", resultado.Valor.Descricao);
        Assert.True(resultado.Valor.Ativo);
        Assert.Single(repo.Itens);
    }

    [Theory]
    [InlineData("", "Notebook", "UN", "1.1.01", RbacFalha.CodigoObrigatorio)]
    [InlineData("001", "", "UN", "1.1.01", RbacFalha.DescricaoObrigatoria)]
    [InlineData("001", "Notebook", "", "1.1.01", RbacFalha.UnidadeMedidaObrigatoria)]
    [InlineData("001", "Notebook", "UN", "", RbacFalha.ContaContabilObrigatoria)]
    public async Task Criar_Should_Reject_Missing_Required_Fields(string codigo, string descricao, string unidade, string conta, RbacFalha falhaEsperada)
    {
        var (repo, contas, unidades) = Cenario();
        var useCase = new CriarItemFiscalUseCase(repo, contas, unidades, TimeProvider.System);

        var resultado = await useCase.ExecuteAsync(new ItemFiscalCriarInput(codigo, descricao, unidade, conta), Bu, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(falhaEsperada, resultado.Falha);
        Assert.Empty(repo.Itens);
    }

    [Fact]
    public async Task Criar_Should_Reject_Duplicate_Codigo()
    {
        var (repo, contas, unidades) = Cenario();
        repo.Itens.Add(new ItemFiscal("001", "Existente", "UN", "1.1.01", Bu, DateTimeOffset.UtcNow));
        var useCase = new CriarItemFiscalUseCase(repo, contas, unidades, TimeProvider.System);

        var resultado = await useCase.ExecuteAsync(new ItemFiscalCriarInput("001", "Notebook Duplicado", "UN", "1.1.01"), Bu, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.CodigoDuplicado, resultado.Falha);
        Assert.Single(repo.Itens);
    }

    [Fact]
    public async Task Criar_Should_Reject_Unknown_ContaContabil()
    {
        var (repo, contas, unidades) = Cenario();
        var useCase = new CriarItemFiscalUseCase(repo, contas, unidades, TimeProvider.System);

        var resultado = await useCase.ExecuteAsync(new ItemFiscalCriarInput("001", "Notebook", "UN", "9.9.99"), Bu, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.ContaContabilInvalidaOuInativa, resultado.Falha);
    }

    [Fact]
    public async Task Criar_Should_Reject_ContaContabil_Inativa_No_Erp_Even_If_Local_Metadata_Says_Active()
    {
        // ADR-0024: uma conta inativa no Linx nunca pode ser selecionada, mesmo que o metadado local do
        // +Compras diga "ativo" — AtivoEfetivo já resolve isso na origem (ListarContasContabeisUseCase).
        var (repo, _, unidades) = Cenario();
        var contas = new FakeContasContabeisUseCase([ContaInativaNoErp("2.9.99")]);
        var useCase = new CriarItemFiscalUseCase(repo, contas, unidades, TimeProvider.System);

        var resultado = await useCase.ExecuteAsync(new ItemFiscalCriarInput("001", "Notebook", "UN", "2.9.99"), Bu, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.ContaContabilInvalidaOuInativa, resultado.Falha);
    }

    [Fact]
    public async Task Criar_Should_Reject_Unknown_Or_Inactive_UnidadeMedida()
    {
        var (repo, contas, _) = Cenario();
        var unidades = new FakeUnidadesMedidaUseCase([UnidadeInativaNoMaisCompras("KG")]);
        var useCase = new CriarItemFiscalUseCase(repo, contas, unidades, TimeProvider.System);

        var resultado = await useCase.ExecuteAsync(new ItemFiscalCriarInput("001", "Notebook", "KG", "1.1.01"), Bu, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.UnidadeMedidaInvalidaOuInativa, resultado.Falha);
    }

    [Fact]
    public async Task Criar_Should_Not_Impose_Granularity_Generic_Or_Specific_Both_Valid()
    {
        // Discovery B3 homologado: granularidade e decisao da area de Compras — generico ("Notebook") e
        // especifico ("MacBook Pro 14") sao igualmente validos, sem regra do +Compras impondo um nivel.
        var (repo, contas, unidades) = Cenario();
        var useCase = new CriarItemFiscalUseCase(repo, contas, unidades, TimeProvider.System);

        var generico = await useCase.ExecuteAsync(new ItemFiscalCriarInput("001", "Notebook", "UN", "1.1.01"), Bu, CancellationToken.None);
        var especifico = await useCase.ExecuteAsync(new ItemFiscalCriarInput("002", "MacBook Pro 14", "UN", "1.1.01"), Bu, CancellationToken.None);

        Assert.True(generico.Sucesso);
        Assert.True(especifico.Sucesso);
    }

    [Fact]
    public async Task Atualizar_Should_Change_Descricao_Unidade_And_Conta_But_Never_Codigo()
    {
        var (repo, contas, unidades) = Cenario(
            contas: [ContaAtiva("1.1.01"), ContaAtiva("1.1.02")],
            unidades: [UnidadeAtiva("UN"), UnidadeAtiva("KG")]);
        var item = new ItemFiscal("001", "Notebook", "UN", "1.1.01", Bu, DateTimeOffset.UtcNow);
        repo.Itens.Add(item);
        var useCase = new AtualizarItemFiscalUseCase(repo, contas, unidades, TimeProvider.System);

        var resultado = await useCase.ExecuteAsync(item.Id, new ItemFiscalAtualizarInput("Notebook Dell", "KG", "1.1.02"), Bu, CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.Equal("001", resultado.Valor!.Codigo);
        Assert.Equal("Notebook Dell", resultado.Valor.Descricao);
        Assert.Equal("KG", resultado.Valor.UnidadeMedidaCodigoErp);
        Assert.Equal("1.1.02", resultado.Valor.ContaContabilCodigoErp);
    }

    [Fact]
    public async Task Atualizar_Should_Reject_Unknown_Item()
    {
        var (repo, contas, unidades) = Cenario();
        var useCase = new AtualizarItemFiscalUseCase(repo, contas, unidades, TimeProvider.System);

        var resultado = await useCase.ExecuteAsync(Guid.NewGuid(), new ItemFiscalAtualizarInput("X", "UN", "1.1.01"), Bu, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.ItemFiscalNaoEncontrado, resultado.Falha);
    }

    [Fact]
    public async Task AlterarStatus_Should_Ativar_E_Inativar_Localmente()
    {
        var (repo, contas, unidades) = Cenario();
        var item = new ItemFiscal("001", "Notebook", "UN", "1.1.01", Bu, DateTimeOffset.UtcNow);
        repo.Itens.Add(item);
        var useCase = new AlterarStatusItemFiscalUseCase(repo, contas, unidades, TimeProvider.System);

        var inativado = await useCase.ExecuteAsync(item.Id, ativo: false, Bu, CancellationToken.None);
        Assert.True(inativado.Sucesso);
        Assert.False(inativado.Valor!.Ativo);

        var reativado = await useCase.ExecuteAsync(item.Id, ativo: true, Bu, CancellationToken.None);
        Assert.True(reativado.Sucesso);
        Assert.True(reativado.Valor!.Ativo);
    }

    [Fact]
    public async Task Listar_Should_Enrich_With_ContaContabil_And_UnidadeMedida_Descriptions()
    {
        var (repo, contas, unidades) = Cenario();
        repo.Itens.Add(new ItemFiscal("001", "Notebook", "UN", "1.1.01", Bu, DateTimeOffset.UtcNow));
        var useCase = new ListarItensFiscaisUseCase(repo, contas, unidades);

        var resultado = await useCase.ExecuteAsync(Bu, CancellationToken.None);

        var item = Assert.Single(resultado);
        Assert.Equal("Conta 1.1.01", item.ContaContabilDescricao);
        Assert.Equal("Unidade UN", item.UnidadeMedidaDescricao);
    }
}
