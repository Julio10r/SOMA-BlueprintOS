using BlueprintOS.Domain.Identity;

namespace BlueprintOS.Application.Identity.Contracts;

/// <summary>O1.12 — Regras Orçamentárias (apenas cadastro, sem motor de consumo/saldo). Toda leitura é
/// obrigatoriamente escopada por UnidadeNegocioId.</summary>
public interface IRegraOrcamentariaRepository
{
    Task<IReadOnlyList<RegraOrcamentaria>> ListarPorUnidadeNegocioAsync(Guid unidadeNegocioId, CancellationToken ct);

    Task<RegraOrcamentaria?> ObterPorIdEUnidadeNegocioAsync(Guid id, Guid unidadeNegocioId, CancellationToken ct);

    Task AdicionarAsync(RegraOrcamentaria regraOrcamentaria, CancellationToken ct);

    Task SalvarAlteracoesAsync(CancellationToken ct);
}
