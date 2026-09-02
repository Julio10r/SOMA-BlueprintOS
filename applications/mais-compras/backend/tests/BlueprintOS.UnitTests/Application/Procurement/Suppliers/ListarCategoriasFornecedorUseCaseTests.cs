using BlueprintOS.Application.Procurement.Suppliers;
using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using BlueprintOS.Domain.Procurement.Suppliers;

namespace BlueprintOS.UnitTests.Application.Procurement.Suppliers;

/// <summary>Gate de homologação de Fornecedores (2026-09-01): Categoria deixou de ser texto livre e
/// passou a ser um catálogo pré-cadastrado próprio do +Compras (CategoriaFornecedor).</summary>
public sealed class ListarCategoriasFornecedorUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_Should_Return_Only_Ativas_Ordered_By_Descricao()
    {
        var repository = new FakeCategoriaFornecedorRepository([
            new CategoriaFornecedor(Guid.NewGuid(), "OUTROS", "Outros"),
            new CategoriaFornecedor(Guid.NewGuid(), "EMBALAGEM", "Embalagem"),
            new CategoriaFornecedor(Guid.NewGuid(), "DESCONTINUADA", "Descontinuada", ativo: false)
        ]);
        var useCase = new ListarCategoriasFornecedorUseCase(repository);

        var resultado = await useCase.ExecuteAsync();

        Assert.Equal(2, resultado.Count);
        Assert.Equal("EMBALAGEM", resultado[0].Codigo);
        Assert.Equal("OUTROS", resultado[1].Codigo);
    }

    private sealed class FakeCategoriaFornecedorRepository(IReadOnlyList<CategoriaFornecedor> todas) : ICategoriaFornecedorRepository
    {
        public Task<IReadOnlyList<CategoriaFornecedor>> ListarAtivasAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CategoriaFornecedor>>(todas.Where(c => c.Ativo).ToList());
    }
}
