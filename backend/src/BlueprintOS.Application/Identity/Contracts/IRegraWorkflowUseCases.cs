using BlueprintOS.Application.Identity.Models;

namespace BlueprintOS.Application.Identity.Contracts;

public interface IListarRegrasWorkflowUseCase
{
    Task<IReadOnlyList<RegraWorkflowDto>> ExecuteAsync(Guid unidadeNegocioId, CancellationToken ct);
}

public interface ICriarRegraWorkflowUseCase
{
    Task<RbacResultado<RegraWorkflowDto>> ExecuteAsync(RegraWorkflowInput input, Guid unidadeNegocioId, CancellationToken ct);
}

public interface IAtualizarRegraWorkflowUseCase
{
    Task<RbacResultado<RegraWorkflowDto>> ExecuteAsync(Guid id, RegraWorkflowInput input, Guid unidadeNegocioId, CancellationToken ct);
}

public interface IAlterarStatusRegraWorkflowUseCase
{
    Task<RbacResultado<RegraWorkflowDto>> ExecuteAsync(Guid id, bool ativo, Guid unidadeNegocioId, CancellationToken ct);
}
