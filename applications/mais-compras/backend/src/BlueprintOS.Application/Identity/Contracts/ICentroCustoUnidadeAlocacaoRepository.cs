using BlueprintOS.Domain.Identity;

namespace BlueprintOS.Application.Identity.Contracts;

/// <summary>Persistência do vínculo N:N Centro de Custo × Unidade de Alocação (O1.9).</summary>
public interface ICentroCustoUnidadeAlocacaoRepository
{
    Task<IReadOnlyList<CentroCustoUnidadeAlocacao>> ListarPorCentroCustoMetadadoAsync(Guid centroCustoMetadadoId, CancellationToken ct);

    /// <summary>Vínculos de todos os Centros de Custo informados, para montar a listagem sem N+1.</summary>
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<CentroCustoUnidadeAlocacao>>> ListarPorCentrosCustoMetadadoAsync(
        IReadOnlyCollection<Guid> centroCustoMetadadoIds, CancellationToken ct);

    /// <summary>Substitui integralmente o conjunto de vínculos de um Centro de Custo. Idempotente — mesmo
    /// padrão de <c>IUsuarioRepository.SubstituirPerfisAsync</c>.</summary>
    Task SubstituirVinculosAsync(
        Guid centroCustoMetadadoId, IReadOnlyList<(Guid UnidadeAlocacaoId, bool Padrao)> vinculos, CancellationToken ct);

    Task SalvarAlteracoesAsync(CancellationToken ct);
}
