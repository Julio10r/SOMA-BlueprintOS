using BlueprintOS.Application.Identity.Models;

namespace BlueprintOS.Application.Identity.Contracts;

public interface IListarAlcadasAprovacaoUseCase
{
    Task<IReadOnlyList<AlcadaAprovacaoDto>> ExecuteAsync(Guid unidadeNegocioId, CancellationToken ct);
}

public interface ICriarAlcadaAprovacaoUseCase
{
    Task<RbacResultado<AlcadaAprovacaoDto>> ExecuteAsync(AlcadaAprovacaoInput input, Guid unidadeNegocioId, CancellationToken ct);
}

public interface IAtualizarAlcadaAprovacaoUseCase
{
    Task<RbacResultado<AlcadaAprovacaoDto>> ExecuteAsync(Guid id, AlcadaAprovacaoInput input, Guid unidadeNegocioId, CancellationToken ct);
}

public interface IAlterarStatusAlcadaAprovacaoUseCase
{
    Task<RbacResultado<AlcadaAprovacaoDto>> ExecuteAsync(Guid id, bool ativo, Guid unidadeNegocioId, CancellationToken ct);
}
