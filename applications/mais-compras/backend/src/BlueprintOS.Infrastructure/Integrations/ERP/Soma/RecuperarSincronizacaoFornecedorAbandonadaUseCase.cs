using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using BlueprintOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BlueprintOS.Infrastructure.Integrations.ERP.Soma;

public sealed class RecuperarSincronizacaoFornecedorAbandonadaUseCase(
    BlueprintOSDbContext context,
    ICurrentIdentity identity,
    ILogger<RecuperarSincronizacaoFornecedorAbandonadaUseCase> logger) : IRecuperarSincronizacaoFornecedorAbandonadaUseCase
{
    public async Task<RecuperarSincronizacaoFornecedorAbandonadaResumo> ExecuteAsync(
        RecuperarSincronizacaoFornecedorAbandonadaDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Justificativa))
        {
            throw new ArgumentException("Justificativa é obrigatória para recuperação administrativa de execução abandonada.", nameof(dto));
        }

        var identidadeAtual = identity.GetRequired();
        var execucao = await context.SincronizacoesFornecedores.SingleOrDefaultAsync(x => x.Id == dto.ExecucaoId, cancellationToken)
            ?? throw new InvalidOperationException($"Sincronização de Fornecedores '{dto.ExecucaoId}' não encontrada.");

        if (execucao.Status != "EmAndamento")
        {
            throw new InvalidOperationException(
                $"Sincronização de Fornecedores '{dto.ExecucaoId}' não está travada (Status atual: '{execucao.Status}'). " +
                "A recuperação administrativa só se aplica a execuções comprovadamente abandonadas em 'EmAndamento'.");
        }

        var statusAnterior = execucao.Status;
        var agora = DateTimeOffset.UtcNow;
        execucao.AbortarPorRecuperacaoAdministrativa(agora, dto.Justificativa, identidadeAtual.UserId);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogWarning(
            "Recuperacao administrativa de sincronizacao de fornecedores abandonada. ExecucaoId {ExecucaoId}. UsuarioId {UsuarioId}. Justificativa {Justificativa}",
            execucao.Id, identidadeAtual.UserId, dto.Justificativa);

        return new RecuperarSincronizacaoFornecedorAbandonadaResumo(execucao.Id, statusAnterior, execucao.Status, agora, identidadeAtual.UserId);
    }
}
