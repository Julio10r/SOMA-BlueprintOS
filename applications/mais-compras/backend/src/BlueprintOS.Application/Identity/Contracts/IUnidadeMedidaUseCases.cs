using BlueprintOS.Application.Identity.Models;

namespace BlueprintOS.Application.Identity.Contracts;

public interface IListarUnidadesMedidaUseCase
{
    Task<IReadOnlyList<UnidadeMedidaDto>> ExecuteAsync(Guid unidadeNegocioId, CancellationToken ct);
}

public interface IAtualizarMetadadoUnidadeMedidaUseCase
{
    Task<ErpMetadadoResultado<UnidadeMedidaDto>> ExecuteAsync(string codigoErp, UnidadeMedidaMetadadoInput input, Guid unidadeNegocioId, CancellationToken ct);
}
