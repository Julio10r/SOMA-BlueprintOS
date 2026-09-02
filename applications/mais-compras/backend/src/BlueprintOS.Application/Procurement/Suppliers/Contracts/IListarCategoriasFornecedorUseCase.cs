using BlueprintOS.Application.Procurement.Suppliers.Models;

namespace BlueprintOS.Application.Procurement.Suppliers.Contracts;

public interface IListarCategoriasFornecedorUseCase
{
    Task<IReadOnlyList<CategoriaFornecedorDto>> ExecuteAsync(CancellationToken cancellationToken = default);
}
