using BlueprintOS.Application.Identity.Models;

namespace BlueprintOS.Application.Identity.Contracts;

public interface IListarRegrasOrcamentariasUseCase
{
    Task<IReadOnlyList<RegraOrcamentariaDto>> ExecuteAsync(Guid unidadeNegocioId, CancellationToken ct);
}

public interface ICriarRegraOrcamentariaUseCase
{
    Task<RbacResultado<RegraOrcamentariaDto>> ExecuteAsync(RegraOrcamentariaInput input, Guid unidadeNegocioId, CancellationToken ct);
}

public interface IAtualizarRegraOrcamentariaUseCase
{
    Task<RbacResultado<RegraOrcamentariaDto>> ExecuteAsync(Guid id, RegraOrcamentariaInput input, Guid unidadeNegocioId, CancellationToken ct);
}

public interface IAlterarStatusRegraOrcamentariaUseCase
{
    Task<RbacResultado<RegraOrcamentariaDto>> ExecuteAsync(Guid id, bool ativo, Guid unidadeNegocioId, CancellationToken ct);
}
