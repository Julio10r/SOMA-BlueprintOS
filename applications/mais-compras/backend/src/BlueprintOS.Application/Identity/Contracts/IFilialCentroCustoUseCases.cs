using BlueprintOS.Application.Identity.Models;

namespace BlueprintOS.Application.Identity.Contracts;

public interface IListarFiliaisUseCase
{
    Task<IReadOnlyList<FilialDto>> ExecuteAsync(Guid unidadeNegocioId, CancellationToken ct);
}

public interface IAtualizarMetadadoFilialUseCase
{
    Task<ErpMetadadoResultado<FilialDto>> ExecuteAsync(string codigoCliFor, FilialMetadadoInput input, Guid unidadeNegocioId, CancellationToken ct);
}

public interface IListarCentrosCustoUseCase
{
    Task<IReadOnlyList<CentroCustoDto>> ExecuteAsync(Guid unidadeNegocioId, CancellationToken ct);
}

public interface IAtualizarMetadadoCentroCustoUseCase
{
    Task<ErpMetadadoResultado<CentroCustoDto>> ExecuteAsync(string codigoErp, CentroCustoMetadadoInput input, Guid unidadeNegocioId, CancellationToken ct);
}

/// <summary>O1.9 — vínculo N:N Centro de Custo × Unidade de Alocação.</summary>
public interface IListarVinculosUnidadeAlocacaoUseCase
{
    Task<ErpMetadadoResultado<IReadOnlyList<UnidadeAlocacaoVinculoDto>>> ExecuteAsync(string codigoErp, Guid unidadeNegocioId, CancellationToken ct);
}

public interface ISubstituirVinculosUnidadeAlocacaoUseCase
{
    Task<ErpMetadadoResultado<IReadOnlyList<UnidadeAlocacaoVinculoDto>>> ExecuteAsync(
        string codigoErp, SubstituirVinculosUnidadeAlocacaoInput input, Guid unidadeNegocioId, CancellationToken ct);
}
