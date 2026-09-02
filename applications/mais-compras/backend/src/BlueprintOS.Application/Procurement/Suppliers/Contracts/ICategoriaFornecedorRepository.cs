using BlueprintOS.Domain.Procurement.Suppliers;

namespace BlueprintOS.Application.Procurement.Suppliers.Contracts;

public interface ICategoriaFornecedorRepository
{
    Task<IReadOnlyList<CategoriaFornecedor>> ListarAtivasAsync(CancellationToken cancellationToken = default);
}
