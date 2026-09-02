using BlueprintOS.Application.Identity.Models;

namespace BlueprintOS.Application.Identity.Contracts;

public interface IListarContasContabeisUseCase
{
    Task<IReadOnlyList<ContaContabilDto>> ExecuteAsync(Guid unidadeNegocioId, CancellationToken ct);
}

public interface IAtualizarMetadadoContaContabilUseCase
{
    Task<ErpMetadadoResultado<ContaContabilDto>> ExecuteAsync(string codigoErp, ContaContabilMetadadoInput input, Guid unidadeNegocioId, CancellationToken ct);
}
