using BlueprintOS.Domain.Procurement.Suppliers;

namespace BlueprintOS.Infrastructure.Integrations.ERP.Contracts;

public interface IFornecedorErpReader
{
    Task<IReadOnlyList<FornecedorErpIntegracaoDto>> BuscarFornecedoresAsync(
        int limite,
        CancellationToken cancellationToken = default);
}

public sealed record FornecedorErpIntegracaoDto(
    string ErpFornecedorId,
    string ErpSistema,
    FornecedorCanonico Dados,
    DateTimeOffset? UltimaAlteracaoEm);
