using BlueprintOS.Domain.Procurement.Suppliers;

namespace BlueprintOS.Application.Procurement.Suppliers.Contracts;

public sealed record FornecedorDiscoveryQuery(string CodigoItem, string? Descricao, string? Categoria);
public sealed record ErpFornecedorCandidate(string Nome, string? Cnpj, string? CodigoFornecedor,
    bool ItemExato, bool Familia, bool Categoria, bool Historico);

public interface IErpFornecedorDiscoveryRepository
{
    Task<IReadOnlyList<ErpFornecedorCandidate>> DescobrirAsync(FornecedorDiscoveryQuery query, CancellationToken cancellationToken = default);
}

public interface IFornecedorDescobertoRepository
{
    Task AdicionarAsync(FornecedorDescoberto descoberta, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FornecedorDescoberto>> ListarAsync(Guid temporaryUserId, CancellationToken cancellationToken = default);
    Task<FornecedorDescoberto?> ObterPorIdAsync(Guid id, Guid temporaryUserId, CancellationToken cancellationToken = default);
}
