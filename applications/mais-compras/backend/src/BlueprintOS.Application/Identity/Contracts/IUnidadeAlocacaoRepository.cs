using BlueprintOS.Domain.Identity;

namespace BlueprintOS.Application.Identity.Contracts;

/// <summary>Repositório de Unidade de Alocação (O1.8). Todas as leituras são obrigatoriamente escopadas
/// por UnidadeNegocioId — mesmo cuidado de <see cref="IUsuarioRepository"/>/<see cref="IPerfilRepository"/>:
/// um administrador de uma Unidade de Negócio nunca enumera nem altera Unidades de Alocação de outra.</summary>
public interface IUnidadeAlocacaoRepository
{
    Task<IReadOnlyList<UnidadeAlocacao>> ListarPorUnidadeNegocioAsync(Guid unidadeNegocioId, CancellationToken ct);

    Task<UnidadeAlocacao?> ObterPorIdEUnidadeNegocioAsync(Guid id, Guid unidadeNegocioId, CancellationToken ct);

    /// <summary>Resolução em lote para validar o vínculo Centro de Custo × Unidade de Alocação (O1.9): um
    /// Id fora da Unidade de Negócio da sessão nunca é aceito, mesmo que exista em outra BU.</summary>
    Task<IReadOnlyList<UnidadeAlocacao>> ObterPorIdsEUnidadeNegocioAsync(IReadOnlyCollection<Guid> ids, Guid unidadeNegocioId, CancellationToken ct);

    Task<bool> ExisteComNomeAsync(string nome, Guid unidadeNegocioId, Guid? excluirId, CancellationToken ct);

    Task AdicionarAsync(UnidadeAlocacao unidadeAlocacao, CancellationToken ct);

    Task SalvarAlteracoesAsync(CancellationToken ct);
}
