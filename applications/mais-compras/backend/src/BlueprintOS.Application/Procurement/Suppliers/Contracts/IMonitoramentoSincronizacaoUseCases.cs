using BlueprintOS.Application.Procurement.Suppliers.Models;

namespace BlueprintOS.Application.Procurement.Suppliers.Contracts;

public interface IListarSincronizacoesFornecedoresUseCase
{
    Task<ListarSincronizacoesFornecedoresResultado> ExecuteAsync(Guid unidadeNegocioId, ListarSincronizacoesFornecedoresFiltro filtro, CancellationToken ct);
}

public interface IObterSincronizacaoFornecedorUseCase
{
    Task<SincronizacaoFornecedorDetalheDto?> ExecuteAsync(Guid unidadeNegocioId, Guid id, CancellationToken ct);
}
