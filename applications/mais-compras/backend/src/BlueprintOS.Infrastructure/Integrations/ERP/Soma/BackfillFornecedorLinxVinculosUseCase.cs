using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using BlueprintOS.Domain.Procurement.Suppliers;
using BlueprintOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BlueprintOS.Infrastructure.Integrations.ERP.Soma;

public sealed class BackfillFornecedorLinxVinculosUseCase(
    BlueprintOSDbContext context, ILogger<BackfillFornecedorLinxVinculosUseCase> logger) : IBackfillFornecedorLinxVinculosUseCase
{
    public async Task<BackfillFornecedorLinxVinculosResumo> ExecuteAsync(BackfillFornecedorLinxVinculosDto dto, CancellationToken cancellationToken = default)
    {
        var inicio = DateTimeOffset.UtcNow;

        var candidatos = await context.Fornecedores.AsNoTracking()
            .Where(f => f.ErpFornecedorId != null && f.ErpSistema != null)
            .Select(f => new { f.Id, f.UnidadeNegocioId, f.ErpSistema, f.ErpFornecedorId, f.NomeFantasia, f.Status })
            .ToListAsync(cancellationToken);

        var vinculosExistentes = await context.FornecedorLinxVinculos.AsNoTracking()
            .Select(v => new { v.UnidadeNegocioId, v.ErpSistema, v.CodigoErp })
            .ToListAsync(cancellationToken);
        var chavesExistentes = vinculosExistentes.Select(v => (v.UnidadeNegocioId, v.ErpSistema, v.CodigoErp)).ToHashSet();

        int criados = 0, jaExistentes = 0;
        foreach (var f in candidatos)
        {
            var chave = (f.UnidadeNegocioId, f.ErpSistema!, f.ErpFornecedorId!);
            if (chavesExistentes.Contains(chave))
            {
                jaExistentes++;
                continue;
            }

            criados++;
            if (dto.DryRun) continue;

            var ativo = f.Status == "Ativo";
            var vinculo = new FornecedorLinxVinculo(f.Id, f.UnidadeNegocioId, f.ErpSistema!, f.ErpFornecedorId!, f.NomeFantasia ?? string.Empty,
                inativoFornecedores: !ativo, inativoCadastroCliFor: false, dataParaTransferencia: null, principal: ativo, agora: inicio);
            await context.FornecedorLinxVinculos.AddAsync(vinculo, cancellationToken);
            // Evita reprocessar a mesma chave duas vezes caso, por algum motivo, existam Fornecedores
            // distintos com o mesmo (ErpSistema, ErpFornecedorId) legado — não deveria ocorrer dado o
            // índice único já existente, mas defende contra o índice único do próprio vínculo.
            chavesExistentes.Add(chave);
        }

        if (!dto.DryRun)
        {
            await context.SaveChangesAsync(cancellationToken);
        }

        var fim = DateTimeOffset.UtcNow;
        var status = dto.DryRun ? "DryRunConcluido" : "Concluido";
        logger.LogInformation(
            "Backfill de vinculos Linx de Fornecedor finalizado. Status {Status}. FornecedoresComIdentidadeErpLegada {Total}. VinculosCriados {Criados}. VinculosJaExistentes {JaExistentes}.",
            status, candidatos.Count, criados, jaExistentes);

        return new BackfillFornecedorLinxVinculosResumo(status, inicio, fim, candidatos.Count, criados, jaExistentes, (long)(fim - inicio).TotalMilliseconds);
    }
}
