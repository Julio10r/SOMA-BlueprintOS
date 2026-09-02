using BlueprintOS.Application.Identity.Models;

namespace BlueprintOS.Application.Identity.Contracts;

/// <summary>Casos de uso do cadastro local de Item Fiscal (B3 — Bloco 3), seguindo o mesmo padrão de
/// <c>IUnidadeAlocacaoUseCases</c>: <c>unidadeNegocioId</c> sempre resolvido pela API a partir da
/// identidade autenticada, nunca do corpo da requisição.</summary>
public interface IListarItensFiscaisUseCase
{
    Task<IReadOnlyList<ItemFiscalDto>> ExecuteAsync(Guid unidadeNegocioId, CancellationToken ct);
}

public interface IObterItemFiscalUseCase
{
    Task<ItemFiscalDto?> ExecuteAsync(Guid id, Guid unidadeNegocioId, CancellationToken ct);
}

public interface ICriarItemFiscalUseCase
{
    Task<RbacResultado<ItemFiscalDto>> ExecuteAsync(ItemFiscalCriarInput input, Guid unidadeNegocioId, CancellationToken ct);
}

public interface IAtualizarItemFiscalUseCase
{
    Task<RbacResultado<ItemFiscalDto>> ExecuteAsync(Guid id, ItemFiscalAtualizarInput input, Guid unidadeNegocioId, CancellationToken ct);
}

public interface IAlterarStatusItemFiscalUseCase
{
    Task<RbacResultado<ItemFiscalDto>> ExecuteAsync(Guid id, bool ativo, Guid unidadeNegocioId, CancellationToken ct);
}
