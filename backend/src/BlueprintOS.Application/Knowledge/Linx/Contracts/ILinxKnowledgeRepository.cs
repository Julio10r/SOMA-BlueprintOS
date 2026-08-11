using BlueprintOS.Application.Knowledge.Linx.Models;
using BlueprintOS.Domain.Knowledge.Linx;

namespace BlueprintOS.Application.Knowledge.Linx.Contracts;

/// <summary>Persistência da base de conhecimento dos Agents Especialistas Linx. Nenhum método de
/// atualização in-place de conteúdo existe — apenas <see cref="AdicionarAsync"/> (toda versão é uma nova
/// linha) e <see cref="AtualizarProvenienciaAsync"/> (apenas o campo de proveniência de uma linha
/// existente, nunca o conteúdo).</summary>
public interface ILinxKnowledgeRepository
{
    Task AdicionarAsync(LinxKnowledgeEntry entrada, CancellationToken ct);

    Task<LinxKnowledgeEntry?> ObterPorIdAsync(Guid id, CancellationToken ct);

    /// <summary>A versão mais recente (maior <c>Versao</c>) de uma cadeia de versionamento.</summary>
    Task<LinxKnowledgeEntry?> ObterUltimaVersaoAsync(Guid versaoRaizId, CancellationToken ct);

    /// <summary>Histórico completo (todas as versões), em ordem crescente de <c>Versao</c> — nunca perdido,
    /// mesmo após promoções/novas versões (Work Order, seção 12).</summary>
    Task<IReadOnlyList<LinxKnowledgeEntry>> ObterHistoricoAsync(Guid versaoRaizId, CancellationToken ct);

    /// <summary>Busca apenas a versão mais recente de cada cadeia — nunca retorna versões obsoletas
    /// misturadas com a atual.</summary>
    Task<IReadOnlyList<LinxKnowledgeEntry>> BuscarUltimasVersoesAsync(LinxKnowledgeFiltro filtro, CancellationToken ct);

    Task AtualizarProvenienciaAsync(LinxKnowledgeEntry entrada, CancellationToken ct);
}
