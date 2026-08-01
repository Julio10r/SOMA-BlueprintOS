using BlueprintOS.Domain.Procurement.Suppliers;

namespace BlueprintOS.Application.Procurement.Suppliers.Contracts;

public interface IFornecedorEnriquecimentoAnaliseRepository
{
    Task AdicionarAsync(FornecedorEnriquecimentoAnalise analise, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FornecedorEnriquecimentoAnalise>> ListarPorFornecedorAsync(Guid fornecedorId, CancellationToken cancellationToken = default);
}
