using BlueprintOS.Application.Knowledge.Linx.Contracts;
using BlueprintOS.Application.Knowledge.Linx.Models;
using BlueprintOS.Domain.Knowledge.Linx;
using BlueprintOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BlueprintOS.Infrastructure.Knowledge.Linx;

public sealed class LinxKnowledgeRepository(BlueprintOSDbContext db) : ILinxKnowledgeRepository
{
    public async Task AdicionarAsync(LinxKnowledgeEntry entrada, CancellationToken ct)
    {
        db.LinxConhecimentoEntradas.Add(entrada);
        await db.SaveChangesAsync(ct);
    }

    public async Task<LinxKnowledgeEntry?> ObterPorIdAsync(Guid id, CancellationToken ct) =>
        await db.LinxConhecimentoEntradas.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<LinxKnowledgeEntry?> ObterUltimaVersaoAsync(Guid versaoRaizId, CancellationToken ct) =>
        await db.LinxConhecimentoEntradas
            .Where(x => x.VersaoRaizId == versaoRaizId)
            .OrderByDescending(x => x.Versao)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<LinxKnowledgeEntry>> ObterHistoricoAsync(Guid versaoRaizId, CancellationToken ct) =>
        await db.LinxConhecimentoEntradas
            .Where(x => x.VersaoRaizId == versaoRaizId)
            .OrderBy(x => x.Versao)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<LinxKnowledgeEntry>> BuscarUltimasVersoesAsync(LinxKnowledgeFiltro filtro, CancellationToken ct)
    {
        // MVP de busca textual/estruturada (Work Order, seção 13): filtra por especialista/categoria/
        // proveniência mínima/BU/tags no banco, e por texto em memória sobre o subconjunto já filtrado —
        // ponto de extensão futuro para embeddings/RAG sem redesenho do contrato.
        var query = db.LinxConhecimentoEntradas.AsQueryable();

        if (filtro.Especialista is { } especialista) query = query.Where(x => x.Especialista == especialista);
        if (filtro.Categoria is { } categoria) query = query.Where(x => x.Categoria == categoria);
        if (filtro.UnidadeNegocioId is { } bu) query = query.Where(x => x.UnidadeNegocioId == null || x.UnidadeNegocioId == bu);
        if (filtro.ProvenienciaMinima is { } provenienciaMinima) query = query.Where(x => x.Proveniencia >= provenienciaMinima);

        var todasVersoes = await query.ToListAsync(ct);

        var ultimasVersoes = todasVersoes
            .GroupBy(x => x.VersaoRaizId)
            .Select(g => g.OrderByDescending(x => x.Versao).First())
            .AsEnumerable();

        if (!string.IsNullOrWhiteSpace(filtro.Texto))
        {
            ultimasVersoes = ultimasVersoes.Where(x =>
                x.Assunto.Contains(filtro.Texto, StringComparison.OrdinalIgnoreCase) ||
                x.Conteudo.Contains(filtro.Texto, StringComparison.OrdinalIgnoreCase));
        }

        if (filtro.Tags is { Count: > 0 } tags)
        {
            ultimasVersoes = ultimasVersoes.Where(x => tags.Any(t => x.Tags.Contains(t, StringComparer.OrdinalIgnoreCase)));
        }

        return ultimasVersoes
            .OrderByDescending(x => x.AtualizadoEm)
            .Take(Math.Clamp(filtro.MaxResultados <= 0 ? 20 : filtro.MaxResultados, 1, 200))
            .ToArray();
    }

    public async Task AtualizarProvenienciaAsync(LinxKnowledgeEntry entrada, CancellationToken ct)
    {
        db.LinxConhecimentoEntradas.Update(entrada);
        await db.SaveChangesAsync(ct);
    }
}
