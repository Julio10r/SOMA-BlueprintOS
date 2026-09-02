using BlueprintOS.Domain.Identity;

namespace BlueprintOS.Application.Identity.Contracts;

/// <summary>Persistência dos metadados locais +Compras de Conta Contábil (B3 — Bloco 1).</summary>
public interface IContaContabilMetadadoRepository
{
    Task<IReadOnlyDictionary<string, ContaContabilMetadado>> ListarPorUnidadeNegocioAsync(Guid unidadeNegocioId, CancellationToken ct);

    Task<ContaContabilMetadado?> ObterPorCodigoErpAsync(string codigoErp, Guid unidadeNegocioId, CancellationToken ct);

    Task AdicionarAsync(ContaContabilMetadado metadado, CancellationToken ct);

    Task SalvarAlteracoesAsync(CancellationToken ct);
}
