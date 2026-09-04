using BlueprintOS.Application.Identity;
using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using BlueprintOS.Application.Procurement.Suppliers.Models;
using BlueprintOS.Domain.Identity;

namespace BlueprintOS.UnitTests.Application.Identity;

/// <summary>B3 — Bloco 4: Referências de Item Fiscal por Fornecedor (Discovery homologado, espelho local de
/// `ITEM_FISCAL_REF_FORNECEDOR`). Cobre: item sem/uma/múltiplas referências, inclusão, alteração (somente o
/// código — Fornecedor é imutável), remoção física, fornecedor inexistente/inativo, Item Fiscal inexistente,
/// e as duas unicidades: (ItemFiscal, Fornecedor) comprovada em Linx e (Fornecedor, CodigoItemFornecedor)
/// GLOBAL autorizada pelo Product Owner na homologação do Bloco 4. RBAC verificado em
/// <c>ItemFiscalReferenciasFornecedorRbacTests</c>, não aqui.</summary>
public sealed class ItemFiscalReferenciaFornecedorUseCasesTests
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

        public Task<ItemFiscal?> ObterPorCodigoSemRastreamentoAsync(string codigo, CancellationToken ct) =>
            Task.FromResult(Itens.SingleOrDefault(x => x.Codigo == codigo));

        public Task<ItemFiscal?> ObterPorCodigoAsync(string codigo, CancellationToken ct) =>
            Task.FromResult(Itens.SingleOrDefault(x => x.Codigo == codigo));

        public Task<int> ContarAsync(CancellationToken ct) => Task.FromResult(Itens.Count);
    }

    private sealed class FakeReferenciaFornecedorRepository : IItemFiscalReferenciaFornecedorRepository
    {
        public List<ItemFiscalReferenciaFornecedor> Referencias { get; } = [];

        public Task<IReadOnlyList<ItemFiscalReferenciaFornecedor>> ListarPorItemFiscalAsync(Guid itemFiscalId, CancellationToken ct) =>
            Task.FromResult((IReadOnlyList<ItemFiscalReferenciaFornecedor>)Referencias.Where(x => x.ItemFiscalId == itemFiscalId).ToArray());

        public Task<ItemFiscalReferenciaFornecedor?> ObterPorIdAsync(Guid id, Guid itemFiscalId, CancellationToken ct) =>
            Task.FromResult(Referencias.SingleOrDefault(x => x.Id == id && x.ItemFiscalId == itemFiscalId));

        public Task<ItemFiscalReferenciaFornecedor?> ObterPorItemEFornecedorAsync(Guid itemFiscalId, Guid fornecedorId, CancellationToken ct) =>
            Task.FromResult(Referencias.SingleOrDefault(x => x.ItemFiscalId == itemFiscalId && x.FornecedorId == fornecedorId));

        public Task<bool> ExisteParaFornecedorNoItemAsync(Guid itemFiscalId, Guid fornecedorId, Guid? excluirId, CancellationToken ct) =>
            Task.FromResult(Referencias.Any(x => x.ItemFiscalId == itemFiscalId && x.FornecedorId == fornecedorId && x.Id != excluirId));

        public Task<bool> ExisteCodigoParaFornecedorAsync(Guid fornecedorId, string codigoItemFornecedor, Guid? excluirId, CancellationToken ct) =>
            Task.FromResult(Referencias.Any(x =>
                x.FornecedorId == fornecedorId &&
                string.Equals(x.CodigoItemFornecedor, codigoItemFornecedor, StringComparison.OrdinalIgnoreCase) &&
                x.Id != excluirId));

        public Task AdicionarAsync(ItemFiscalReferenciaFornecedor referencia, CancellationToken ct)
        {
            Referencias.Add(referencia);
            return Task.CompletedTask;
        }

        public Task RemoverAsync(ItemFiscalReferenciaFornecedor referencia, CancellationToken ct)
        {
            Referencias.RemoveAll(x => x.Id == referencia.Id);
            return Task.CompletedTask;
        }

        public Task SalvarAlteracoesAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeObterFornecedorUseCase(Dictionary<Guid, FornecedorDto> fornecedores) : IObterFornecedorUseCase
    {
        public Task<FornecedorDto?> ExecuteAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(fornecedores.GetValueOrDefault(id));
    }

    private static FornecedorDto FornecedorAtivo(Guid id, string nome) =>
        new(id, nome, "12345678900", null, null, null, null, null, null, null, "Ativo", null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

    private static FornecedorDto FornecedorInativo(Guid id, string nome) =>
        new(id, nome, "12345678900", null, null, null, null, null, null, null, "Inativo", null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

    private static (FakeItemFiscalRepository itens, FakeReferenciaFornecedorRepository referencias, FakeObterFornecedorUseCase fornecedores, ItemFiscal item) Cenario(
        params FornecedorDto[] fornecedoresCadastrados)
    {
        var itens = new FakeItemFiscalRepository();
        var item = new ItemFiscal("001", "Notebook", "UN", "1.1.01", Bu, DateTimeOffset.UtcNow);
        itens.Itens.Add(item);

        var mapa = fornecedoresCadastrados.ToDictionary(f => f.Id);
        return (itens, new FakeReferenciaFornecedorRepository(), new FakeObterFornecedorUseCase(mapa), item);
    }

    [Fact]
    public async Task Listar_Should_Return_Empty_When_ItemFiscal_Has_No_References()
    {
        var (itens, referencias, fornecedores, item) = Cenario();
        var useCase = new ListarReferenciasFornecedorUseCase(itens, referencias, fornecedores);

        var resultado = await useCase.ExecuteAsync(item.Id, Bu, CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.Empty(resultado.Valor!);
    }

    [Fact]
    public async Task Listar_Should_Return_Single_Reference_Enriched_With_FornecedorNome()
    {
        var amazon = FornecedorAtivo(Guid.NewGuid(), "Amazon");
        var (itens, referencias, fornecedores, item) = Cenario(amazon);
        referencias.Referencias.Add(new ItemFiscalReferenciaFornecedor(item.Id, amazon.Id, "hduahd78", DateTimeOffset.UtcNow));
        var useCase = new ListarReferenciasFornecedorUseCase(itens, referencias, fornecedores);

        var resultado = await useCase.ExecuteAsync(item.Id, Bu, CancellationToken.None);

        var referencia = Assert.Single(resultado.Valor!);
        Assert.Equal("Amazon", referencia.FornecedorNome);
        Assert.Equal("hduahd78", referencia.CodigoItemFornecedor);
    }

    [Fact]
    public async Task Listar_Should_Return_Multiple_References_From_Different_Suppliers()
    {
        var amazon = FornecedorAtivo(Guid.NewGuid(), "Amazon");
        var apple = FornecedorAtivo(Guid.NewGuid(), "Apple");
        var (itens, referencias, fornecedores, item) = Cenario(amazon, apple);
        referencias.Referencias.Add(new ItemFiscalReferenciaFornecedor(item.Id, amazon.Id, "hduahd78", DateTimeOffset.UtcNow));
        referencias.Referencias.Add(new ItemFiscalReferenciaFornecedor(item.Id, apple.Id, "jaidjabdjao", DateTimeOffset.UtcNow));
        var useCase = new ListarReferenciasFornecedorUseCase(itens, referencias, fornecedores);

        var resultado = await useCase.ExecuteAsync(item.Id, Bu, CancellationToken.None);

        Assert.Equal(2, resultado.Valor!.Count);
        Assert.Contains(resultado.Valor, r => r.FornecedorNome == "Amazon" && r.CodigoItemFornecedor == "hduahd78");
        Assert.Contains(resultado.Valor, r => r.FornecedorNome == "Apple" && r.CodigoItemFornecedor == "jaidjabdjao");
    }

    [Fact]
    public async Task Listar_Should_Reject_Unknown_ItemFiscal()
    {
        var (itens, referencias, fornecedores, _) = Cenario();
        var useCase = new ListarReferenciasFornecedorUseCase(itens, referencias, fornecedores);

        var resultado = await useCase.ExecuteAsync(Guid.NewGuid(), Bu, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.ItemFiscalNaoEncontrado, resultado.Falha);
    }

    [Fact]
    public async Task Incluir_Should_Succeed_With_Valid_Fornecedor_And_Codigo()
    {
        var amazon = FornecedorAtivo(Guid.NewGuid(), "Amazon");
        var (itens, referencias, fornecedores, item) = Cenario(amazon);
        var useCase = new IncluirReferenciaFornecedorUseCase(itens, referencias, fornecedores, TimeProvider.System);

        var resultado = await useCase.ExecuteAsync(item.Id, new ItemFiscalReferenciaFornecedorCriarInput(amazon.Id, "hduahd78"), Bu, CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.Equal("Amazon", resultado.Valor!.FornecedorNome);
        Assert.Single(referencias.Referencias);
    }

    [Fact]
    public async Task Incluir_Should_Reject_Missing_Fornecedor()
    {
        var (itens, referencias, fornecedores, item) = Cenario();
        var useCase = new IncluirReferenciaFornecedorUseCase(itens, referencias, fornecedores, TimeProvider.System);

        var resultado = await useCase.ExecuteAsync(item.Id, new ItemFiscalReferenciaFornecedorCriarInput(Guid.Empty, "codigo"), Bu, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.FornecedorObrigatorio, resultado.Falha);
    }

    [Fact]
    public async Task Incluir_Should_Reject_Missing_Codigo()
    {
        var amazon = FornecedorAtivo(Guid.NewGuid(), "Amazon");
        var (itens, referencias, fornecedores, item) = Cenario(amazon);
        var useCase = new IncluirReferenciaFornecedorUseCase(itens, referencias, fornecedores, TimeProvider.System);

        var resultado = await useCase.ExecuteAsync(item.Id, new ItemFiscalReferenciaFornecedorCriarInput(amazon.Id, ""), Bu, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.CodigoItemFornecedorObrigatorio, resultado.Falha);
    }

    [Fact]
    public async Task Incluir_Should_Reject_Unknown_Fornecedor()
    {
        var (itens, referencias, fornecedores, item) = Cenario();
        var useCase = new IncluirReferenciaFornecedorUseCase(itens, referencias, fornecedores, TimeProvider.System);

        var resultado = await useCase.ExecuteAsync(item.Id, new ItemFiscalReferenciaFornecedorCriarInput(Guid.NewGuid(), "codigo"), Bu, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.FornecedorNaoEncontrado, resultado.Falha);
    }

    [Fact]
    public async Task Incluir_Should_Reject_Inactive_Fornecedor()
    {
        var inativo = FornecedorInativo(Guid.NewGuid(), "Fornecedor Inativo");
        var (itens, referencias, fornecedores, item) = Cenario(inativo);
        var useCase = new IncluirReferenciaFornecedorUseCase(itens, referencias, fornecedores, TimeProvider.System);

        var resultado = await useCase.ExecuteAsync(item.Id, new ItemFiscalReferenciaFornecedorCriarInput(inativo.Id, "codigo"), Bu, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.FornecedorInvalidoOuInativo, resultado.Falha);
    }

    [Fact]
    public async Task Incluir_Should_Reject_Unknown_ItemFiscal()
    {
        var amazon = FornecedorAtivo(Guid.NewGuid(), "Amazon");
        var (itens, referencias, fornecedores, _) = Cenario(amazon);
        var useCase = new IncluirReferenciaFornecedorUseCase(itens, referencias, fornecedores, TimeProvider.System);

        var resultado = await useCase.ExecuteAsync(Guid.NewGuid(), new ItemFiscalReferenciaFornecedorCriarInput(amazon.Id, "codigo"), Bu, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.ItemFiscalNaoEncontrado, resultado.Falha);
    }

    [Fact]
    public async Task Incluir_Should_Reject_Second_Reference_For_Same_Fornecedor_And_ItemFiscal()
    {
        // Comprovado em Linx: KeyFieldList = FORNECEDOR, CODIGO_ITEM — um fornecedor tem no máximo uma
        // referência por Item Fiscal.
        var amazon = FornecedorAtivo(Guid.NewGuid(), "Amazon");
        var (itens, referencias, fornecedores, item) = Cenario(amazon);
        referencias.Referencias.Add(new ItemFiscalReferenciaFornecedor(item.Id, amazon.Id, "codigo-1", DateTimeOffset.UtcNow));
        var useCase = new IncluirReferenciaFornecedorUseCase(itens, referencias, fornecedores, TimeProvider.System);

        var resultado = await useCase.ExecuteAsync(item.Id, new ItemFiscalReferenciaFornecedorCriarInput(amazon.Id, "codigo-2"), Bu, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.ReferenciaJaExistenteParaFornecedor, resultado.Falha);
    }

    [Fact]
    public async Task Incluir_Should_Reject_Same_Fornecedor_Codigo_Used_By_Another_ItemFiscal()
    {
        // DECISAO DO PRODUCT OWNER (homologacao do Bloco 4): (FornecedorId, CodigoItemFornecedor) e unico
        // GLOBALMENTE — mesmo codigo do mesmo fornecedor nao pode apontar para dois Itens Fiscais diferentes.
        var amazon = FornecedorAtivo(Guid.NewGuid(), "Amazon");
        var (itens, referencias, fornecedores, itemA) = Cenario(amazon);
        var itemB = new ItemFiscal("002", "Mouse", "UN", "1.1.01", Bu, DateTimeOffset.UtcNow);
        itens.Itens.Add(itemB);
        referencias.Referencias.Add(new ItemFiscalReferenciaFornecedor(itemA.Id, amazon.Id, "mesmo-codigo", DateTimeOffset.UtcNow));
        var useCase = new IncluirReferenciaFornecedorUseCase(itens, referencias, fornecedores, TimeProvider.System);

        var resultado = await useCase.ExecuteAsync(itemB.Id, new ItemFiscalReferenciaFornecedorCriarInput(amazon.Id, "mesmo-codigo"), Bu, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.CodigoItemFornecedorDuplicadoParaFornecedor, resultado.Falha);
    }

    [Fact]
    public async Task Atualizar_Should_Change_Codigo_But_Never_Fornecedor()
    {
        var amazon = FornecedorAtivo(Guid.NewGuid(), "Amazon");
        var (itens, referencias, fornecedores, item) = Cenario(amazon);
        var referencia = new ItemFiscalReferenciaFornecedor(item.Id, amazon.Id, "codigo-antigo", DateTimeOffset.UtcNow);
        referencias.Referencias.Add(referencia);
        var useCase = new AtualizarReferenciaFornecedorUseCase(itens, referencias, fornecedores, TimeProvider.System);

        var resultado = await useCase.ExecuteAsync(item.Id, referencia.Id, new ItemFiscalReferenciaFornecedorAtualizarInput("codigo-novo"), Bu, CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.Equal("codigo-novo", resultado.Valor!.CodigoItemFornecedor);
        Assert.Equal(amazon.Id, resultado.Valor.FornecedorId);
    }

    [Fact]
    public async Task Atualizar_Should_Reject_Unknown_Reference()
    {
        var (itens, referencias, fornecedores, item) = Cenario();
        var useCase = new AtualizarReferenciaFornecedorUseCase(itens, referencias, fornecedores, TimeProvider.System);

        var resultado = await useCase.ExecuteAsync(item.Id, Guid.NewGuid(), new ItemFiscalReferenciaFornecedorAtualizarInput("codigo"), Bu, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.ItemFiscalReferenciaFornecedorNaoEncontrada, resultado.Falha);
    }

    [Fact]
    public async Task Remover_Should_Delete_Reference_Physically()
    {
        var amazon = FornecedorAtivo(Guid.NewGuid(), "Amazon");
        var (itens, referencias, fornecedores, item) = Cenario(amazon);
        var referencia = new ItemFiscalReferenciaFornecedor(item.Id, amazon.Id, "codigo", DateTimeOffset.UtcNow);
        referencias.Referencias.Add(referencia);
        var useCase = new RemoverReferenciaFornecedorUseCase(itens, referencias);

        var resultado = await useCase.ExecuteAsync(item.Id, referencia.Id, Bu, CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.Empty(referencias.Referencias);
    }

    [Fact]
    public async Task Remover_Should_Reject_Unknown_Reference()
    {
        var (itens, referencias, _, item) = Cenario();
        var useCase = new RemoverReferenciaFornecedorUseCase(itens, referencias);

        var resultado = await useCase.ExecuteAsync(item.Id, Guid.NewGuid(), Bu, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.ItemFiscalReferenciaFornecedorNaoEncontrada, resultado.Falha);
    }

    [Fact]
    public async Task Remover_Should_Reject_Unknown_ItemFiscal()
    {
        var (itens, referencias, _, _) = Cenario();
        var useCase = new RemoverReferenciaFornecedorUseCase(itens, referencias);

        var resultado = await useCase.ExecuteAsync(Guid.NewGuid(), Guid.NewGuid(), Bu, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.ItemFiscalNaoEncontrado, resultado.Falha);
    }
}
