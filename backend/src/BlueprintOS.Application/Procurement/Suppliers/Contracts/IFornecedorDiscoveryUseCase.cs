using BlueprintOS.Application.Procurement.Suppliers.Models;

namespace BlueprintOS.Application.Procurement.Suppliers.Contracts;

public interface IDescobrirFornecedoresUseCase
{
    Task<IReadOnlyList<FornecedorDescobertoDto>> ExecuteAsync(DescobrirFornecedoresDto dto, CancellationToken cancellationToken = default);
}

public interface IListarDescobertasUseCase
{
    Task<IReadOnlyList<FornecedorDescobertoDto>> ExecuteAsync(CancellationToken cancellationToken = default);
    Task<FornecedorDescobertoDto?> ExecuteAsync(Guid id, CancellationToken cancellationToken = default);
}
