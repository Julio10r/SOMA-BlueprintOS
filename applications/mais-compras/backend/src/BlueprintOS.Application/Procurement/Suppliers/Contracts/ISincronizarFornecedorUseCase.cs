using BlueprintOS.Application.Procurement.Suppliers.Models;

namespace BlueprintOS.Application.Procurement.Suppliers.Contracts;

public interface ISincronizarFornecedorUseCase
{
    Task<SincronizacaoFornecedorResultado> ExecuteAsync(SincronizarFornecedorDto dto, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SincronizacaoFornecedorResultado>> ExecutarLoteAsync(SincronizarFornecedoresLoteDto dto, CancellationToken cancellationToken = default);
}
