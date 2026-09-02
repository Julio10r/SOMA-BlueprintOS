using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using BlueprintOS.Application.Procurement.Suppliers.Models;

namespace BlueprintOS.Application.Procurement.Suppliers;

public sealed class ListarCategoriasFornecedorUseCase(ICategoriaFornecedorRepository repository) : IListarCategoriasFornecedorUseCase
{
    public async Task<IReadOnlyList<CategoriaFornecedorDto>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var categorias = await repository.ListarAtivasAsync(cancellationToken);
        return categorias
            .OrderBy(categoria => categoria.Descricao, StringComparer.OrdinalIgnoreCase)
            .Select(categoria => new CategoriaFornecedorDto(categoria.Codigo, categoria.Descricao))
            .ToList();
    }
}
