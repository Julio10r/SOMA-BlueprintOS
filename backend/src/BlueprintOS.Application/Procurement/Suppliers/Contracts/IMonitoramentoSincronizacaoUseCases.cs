using BlueprintOS.Application.Procurement.Suppliers.Models;

namespace BlueprintOS.Application.Procurement.Suppliers.Contracts;

public interface IListarSincronizacoesFornecedoresUseCase
{
    Task<ListarSincronizacoesFornecedoresResultado> ExecuteAsync(ListarSincronizacoesFornecedoresFiltro filtro, CancellationToken ct);
}

public interface IObterSincronizacaoFornecedorUseCase
{
    Task<SincronizacaoFornecedorDetalheDto?> ExecuteAsync(Guid id, CancellationToken ct);
}
