using BlueprintOS.Application.Procurement.Suppliers.Contracts;

namespace BlueprintOS.Application.Procurement.Suppliers;

public sealed class ListarMunicipiosPorUfUseCase(IMunicipioProvider provider) : IListarMunicipiosPorUfUseCase
{
    public async Task<IReadOnlyList<string>> ExecuteAsync(string uf, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(uf)) throw new ArgumentException("Uf is required.", nameof(uf));
        try
        {
            return await provider.ListarPorUfAsync(uf.Trim().ToUpperInvariant(), cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return [];
        }
    }
}
