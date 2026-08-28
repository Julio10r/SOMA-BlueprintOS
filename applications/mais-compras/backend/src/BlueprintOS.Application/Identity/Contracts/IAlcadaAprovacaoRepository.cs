using BlueprintOS.Domain.Identity;

namespace BlueprintOS.Application.Identity.Contracts;

/// <summary>O1.12 — Alçadas de Aprovação. Toda leitura é obrigatoriamente escopada por UnidadeNegocioId.</summary>
public interface IAlcadaAprovacaoRepository
{
    Task<IReadOnlyList<AlcadaAprovacao>> ListarPorUnidadeNegocioAsync(Guid unidadeNegocioId, CancellationToken ct);

    Task<AlcadaAprovacao?> ObterPorIdEUnidadeNegocioAsync(Guid id, Guid unidadeNegocioId, CancellationToken ct);

    Task AdicionarAsync(AlcadaAprovacao alcadaAprovacao, CancellationToken ct);

    Task SalvarAlteracoesAsync(CancellationToken ct);
}
