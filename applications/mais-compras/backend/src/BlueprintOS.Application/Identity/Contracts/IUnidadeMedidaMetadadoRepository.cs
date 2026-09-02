using BlueprintOS.Domain.Identity;

namespace BlueprintOS.Application.Identity.Contracts;

/// <summary>Persistência dos metadados locais +Compras de Unidade de Medida (B3 — Bloco 2).</summary>
public interface IUnidadeMedidaMetadadoRepository
{
    Task<IReadOnlyDictionary<string, UnidadeMedidaMetadado>> ListarPorUnidadeNegocioAsync(Guid unidadeNegocioId, CancellationToken ct);

    Task<UnidadeMedidaMetadado?> ObterPorCodigoErpAsync(string codigoErp, Guid unidadeNegocioId, CancellationToken ct);

    Task AdicionarAsync(UnidadeMedidaMetadado metadado, CancellationToken ct);

    Task SalvarAlteracoesAsync(CancellationToken ct);
}
