using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Domain.Identity;
using BlueprintOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BlueprintOS.Infrastructure.Identity;

public sealed class CentroCustoUnidadeAlocacaoRepository(BlueprintOSDbContext db) : ICentroCustoUnidadeAlocacaoRepository
{
    public async Task<IReadOnlyList<CentroCustoUnidadeAlocacao>> ListarPorCentroCustoMetadadoAsync(
        Guid centroCustoMetadadoId, CancellationToken ct) =>
        await db.CentrosCustoUnidadesAlocacao
            .Where(x => x.CentroCustoMetadadoId == centroCustoMetadadoId)
            .ToListAsync(ct);

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<CentroCustoUnidadeAlocacao>>> ListarPorCentrosCustoMetadadoAsync(
        IReadOnlyCollection<Guid> centroCustoMetadadoIds, CancellationToken ct)
    {
        if (centroCustoMetadadoIds.Count == 0) return new Dictionary<Guid, IReadOnlyList<CentroCustoUnidadeAlocacao>>();

        var vinculos = await db.CentrosCustoUnidadesAlocacao
            .Where(x => centroCustoMetadadoIds.Contains(x.CentroCustoMetadadoId))
            .ToListAsync(ct);

        return vinculos
            .GroupBy(x => x.CentroCustoMetadadoId)
            .ToDictionary(grupo => grupo.Key, grupo => (IReadOnlyList<CentroCustoUnidadeAlocacao>)grupo.ToArray());
    }

    /// <summary>Substitui integralmente o conjunto de vínculos de um Centro de Custo. Usa operações
    /// imediatas (<c>ExecuteDeleteAsync</c>/<c>ExecuteUpdateAsync</c>) em vez de entidades rastreadas para
    /// a remoção/zeragem de <c>Padrao</c> — evita violar o índice único filtrado (no máximo um vínculo
    /// <c>Padrao=1</c> por Centro de Custo) por causa de ordenação não determinística do change tracker do
    /// EF Core ao salvar múltiplas linhas na mesma transação. Apenas a inserção de vínculos novos permanece
    /// rastreada (<see cref="SalvarAlteracoesAsync"/> confirma).</summary>
    public async Task SubstituirVinculosAsync(
        Guid centroCustoMetadadoId, IReadOnlyList<(Guid UnidadeAlocacaoId, bool Padrao)> vinculos, CancellationToken ct)
    {
        var desejados = vinculos.ToDictionary(x => x.UnidadeAlocacaoId, x => x.Padrao);

        var idsAtuais = await db.CentrosCustoUnidadesAlocacao
            .Where(x => x.CentroCustoMetadadoId == centroCustoMetadadoId)
            .Select(x => x.UnidadeAlocacaoId)
            .ToListAsync(ct);

        var remover = idsAtuais.Where(id => !desejados.ContainsKey(id)).ToArray();
        if (remover.Length > 0)
        {
            await db.CentrosCustoUnidadesAlocacao
                .Where(x => x.CentroCustoMetadadoId == centroCustoMetadadoId && remover.Contains(x.UnidadeAlocacaoId))
                .ExecuteDeleteAsync(ct);
        }

        // Zera Padrao dos vínculos remanescentes antes de reaplicar — garante que, no momento em que o
        // novo Padrao for definido, nenhuma outra linha deste Centro de Custo ainda esteja marcada.
        await db.CentrosCustoUnidadesAlocacao
            .Where(x => x.CentroCustoMetadadoId == centroCustoMetadadoId)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Padrao, false), ct);

        var jaExistentes = idsAtuais.Where(id => desejados.ContainsKey(id)).ToHashSet();
        var agora = DateTimeOffset.UtcNow;
        foreach (var unidadeAlocacaoId in desejados.Keys.Where(id => !jaExistentes.Contains(id)))
        {
            db.CentrosCustoUnidadesAlocacao.Add(
                new CentroCustoUnidadeAlocacao(centroCustoMetadadoId, unidadeAlocacaoId, desejados[unidadeAlocacaoId], agora));
        }

        var padraoId = desejados.Where(kv => kv.Value).Select(kv => kv.Key).Cast<Guid?>().FirstOrDefault();
        if (padraoId is Guid id && jaExistentes.Contains(id))
        {
            await db.CentrosCustoUnidadesAlocacao
                .Where(x => x.CentroCustoMetadadoId == centroCustoMetadadoId && x.UnidadeAlocacaoId == id)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.Padrao, true), ct);
        }
    }

    /// <summary>Traduz violação de índice único (mesmo cuidado de <c>CentroCustoMetadadoRepository</c>,
    /// O1.7) para <see cref="DuplicateRecordException"/> — cobre tanto a ancoragem concorrente do
    /// <c>CentroCustoMetadado</c> criado sob demanda (compartilha o mesmo <c>DbContext</c>/transação desta
    /// chamada) quanto uma dupla inserção concorrente do mesmo par (CentroCustoMetadadoId,
    /// UnidadeAlocacaoId). Sem esta tradução, a corrida vazaria como <c>DbUpdateException</c> não tratada
    /// (500) em vez de um 409 de negócio limpo.</summary>
    public async Task SalvarAlteracoesAsync(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            throw new DuplicateRecordException(
                "Este vínculo já foi alterado por outra requisição concorrente (Centro de Custo ancorado ou vínculo duplicado).");
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex) =>
        ex.InnerException?.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true
        || ex.InnerException?.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) == true;
}
