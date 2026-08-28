using BlueprintOS.Domain.Identity;

namespace BlueprintOS.Application.Identity.Contracts;

/// <summary>O1.12 — Regras de Workflow. Toda leitura é obrigatoriamente escopada por UnidadeNegocioId,
/// mesmo cuidado de <see cref="IUnidadeAlocacaoRepository"/>.</summary>
public interface IRegraWorkflowRepository
{
    Task<IReadOnlyList<RegraWorkflow>> ListarPorUnidadeNegocioAsync(Guid unidadeNegocioId, CancellationToken ct);

    Task<RegraWorkflow?> ObterPorIdEUnidadeNegocioAsync(Guid id, Guid unidadeNegocioId, CancellationToken ct);

    Task AdicionarAsync(RegraWorkflow regraWorkflow, CancellationToken ct);

    Task SalvarAlteracoesAsync(CancellationToken ct);
}
