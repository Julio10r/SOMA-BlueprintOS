using BlueprintOS.Application.Identity.Models;

namespace BlueprintOS.Application.Identity.Contracts;

/// <summary>Casos de uso das Referências de Item Fiscal por Fornecedor (B3 — Bloco 4, Discovery homologado).
/// Toda operação valida primeiro que <c>itemFiscalId</c> existe e pertence à <c>unidadeNegocioId</c> da
/// identidade autenticada (mesma regra de escopo do Bloco 3) — <see cref="RbacResultado{T}"/> devolve
/// <c>ItemFiscalNaoEncontrado</c> quando não.</summary>
public interface IListarReferenciasFornecedorUseCase
{
    Task<RbacResultado<IReadOnlyList<ItemFiscalReferenciaFornecedorDto>>> ExecuteAsync(Guid itemFiscalId, Guid unidadeNegocioId, CancellationToken ct);
}

public interface IIncluirReferenciaFornecedorUseCase
{
    Task<RbacResultado<ItemFiscalReferenciaFornecedorDto>> ExecuteAsync(Guid itemFiscalId, ItemFiscalReferenciaFornecedorCriarInput input, Guid unidadeNegocioId, CancellationToken ct);
}

public interface IAtualizarReferenciaFornecedorUseCase
{
    Task<RbacResultado<ItemFiscalReferenciaFornecedorDto>> ExecuteAsync(Guid itemFiscalId, Guid referenciaId, ItemFiscalReferenciaFornecedorAtualizarInput input, Guid unidadeNegocioId, CancellationToken ct);
}

public interface IRemoverReferenciaFornecedorUseCase
{
    Task<RbacResultado<bool>> ExecuteAsync(Guid itemFiscalId, Guid referenciaId, Guid unidadeNegocioId, CancellationToken ct);
}
