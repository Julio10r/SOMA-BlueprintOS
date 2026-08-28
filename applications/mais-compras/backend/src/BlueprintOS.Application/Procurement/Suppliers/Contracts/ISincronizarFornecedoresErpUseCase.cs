using BlueprintOS.Application.Procurement.Suppliers.Models;

namespace BlueprintOS.Application.Procurement.Suppliers.Contracts;

public interface ISincronizarFornecedoresErpUseCase
{
    Task<SincronizacaoFornecedoresErpResumo> ExecuteAsync(SincronizarFornecedoresErpDto dto, CancellationToken cancellationToken = default);
}
