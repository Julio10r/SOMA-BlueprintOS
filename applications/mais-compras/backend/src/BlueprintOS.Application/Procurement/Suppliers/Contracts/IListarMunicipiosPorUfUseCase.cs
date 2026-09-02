namespace BlueprintOS.Application.Procurement.Suppliers.Contracts;

public interface IListarMunicipiosPorUfUseCase
{
    Task<IReadOnlyList<string>> ExecuteAsync(string uf, CancellationToken cancellationToken = default);
}
