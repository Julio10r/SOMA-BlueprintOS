using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using BlueprintOS.Application.Procurement.Suppliers.Models;
using BlueprintOS.Domain.Procurement.Suppliers;
using BlueprintOS.Infrastructure.Integrations.ERP.Contracts;
using BlueprintOS.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace BlueprintOS.Infrastructure.Integrations.ERP.Soma;

public sealed class SincronizarFornecedoresErpUseCase(
    IFornecedorErpReader reader,
    IFornecedorRepository repository,
    ICurrentIdentity identity,
    BlueprintOSDbContext context,
    ILogger<SincronizarFornecedoresErpUseCase> logger) : ISincronizarFornecedoresErpUseCase
{
    private const string ErpSistema = "SOMA_DESENV";
    private const int TamanhoPaginaPadrao = 500;

    public async Task<SincronizacaoFornecedoresErpResumo> ExecuteAsync(SincronizarFornecedoresErpDto dto, CancellationToken cancellationToken = default)
    {
        var userId = identity.GetRequired().UserId;
        var correlationId = string.IsNullOrWhiteSpace(dto.CorrelationId) ? Guid.NewGuid().ToString("N") : dto.CorrelationId.Trim()[..Math.Min(dto.CorrelationId.Trim().Length, 100)];
        var businessUnit = string.IsNullOrWhiteSpace(dto.BusinessUnit) ? "DEFAULT" : dto.BusinessUnit.Trim();
        // Limite representa o teto TOTAL de fornecedores processados nesta execucao, nao o tamanho de pagina.
        var limiteTotal = Math.Clamp(dto.Limite <= 0 ? 500 : dto.Limite, 1, 5000);
        var inicio = DateTimeOffset.UtcNow;
        var execucao = new SincronizacaoFornecedor(Guid.NewGuid(), ErpSistema, businessUnit, inicio);

        logger.LogInformation("Sincronizacao de fornecedores ERP iniciada. ExecucaoId {ExecucaoId}. BusinessUnit {BusinessUnit}. LimiteTotal {LimiteTotal}. CorrelationId {CorrelationId}",
            execucao.Id, businessUnit, limiteTotal, correlationId);

        var skip = 0;
        while (execucao.TotalConsultado < limiteTotal)
        {
            var restante = limiteTotal - execucao.TotalConsultado;
            var tamanhoPagina = Math.Min(TamanhoPaginaPadrao, restante);
            var lote = await reader.BuscarFornecedoresAsync(skip, tamanhoPagina, cancellationToken);
            if (lote.Count == 0) break;

            foreach (var externo in lote)
            {
                execucao.RegistrarConsultado();
                try
                {
                    await SincronizarFornecedorAsync(externo, businessUnit, userId, execucao, cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // Uma entidade cujo SaveChangesAsync falhou continua rastreada como Added/Modified.
                    // Sem limpar o ChangeTracker, o proximo SaveChangesAsync (inclusive o final, ao
                    // persistir a SincronizacaoFornecedor) tenta salva-la de novo e repete a falha,
                    // transformando um erro parcial em erro fatal para a execucao inteira.
                    context.ChangeTracker.Clear();
                    execucao.RegistrarErro(Identificar(externo), ex, DateTimeOffset.UtcNow);
                    logger.LogError(ex, "Erro parcial na sincronizacao de fornecedor ERP. ExecucaoId {ExecucaoId}. Fornecedor {FornecedorIdentificacao}",
                        execucao.Id, Identificar(externo));
                }
            }

            logger.LogInformation("Lote de fornecedores ERP processado. ExecucaoId {ExecucaoId}. Skip {Skip}. ProcessadosNoLote {ProcessadosNoLote}. Consultados {Consultados}. Erros {Erros}",
                execucao.Id, skip, lote.Count, execucao.TotalConsultado, execucao.TotalErro);
            skip += lote.Count;
        }

        var fim = DateTimeOffset.UtcNow;
        execucao.Finalizar(fim);
        await context.SincronizacoesFornecedores.AddAsync(execucao, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Sincronizacao de fornecedores ERP finalizada. ExecucaoId {ExecucaoId}. Status {Status}. Consultados {Consultados}. Incluidos {Incluidos}. Atualizados {Atualizados}. SemAlteracao {SemAlteracao}. Erros {Erros}. DuracaoMs {DuracaoMs}",
            execucao.Id, execucao.Status, execucao.TotalConsultado, execucao.TotalIncluido, execucao.TotalAtualizado, execucao.TotalSemAlteracao, execucao.TotalErro, execucao.TempoExecucaoMs);

        return new(execucao.Id, execucao.Status, inicio, fim, execucao.TotalConsultado, execucao.TotalIncluido,
            execucao.TotalAtualizado, execucao.TotalSemAlteracao, execucao.TotalErro, execucao.TempoExecucaoMs,
            businessUnit, ErpSistema, correlationId, fim);
    }

    private async Task SincronizarFornecedorAsync(FornecedorErpIntegracaoDto externo, string businessUnit, Guid userId,
        SincronizacaoFornecedor execucao, CancellationToken cancellationToken)
    {
        var dados = externo.Dados;
        if (string.IsNullOrWhiteSpace(dados.RazaoSocial)) throw new ArgumentException("RazaoSocial do fornecedor ERP e obrigatoria.");
        if (string.IsNullOrWhiteSpace(dados.DocumentoFiscal)) throw new ArgumentException("DocumentoFiscal do fornecedor ERP e obrigatorio.");

        var local = await repository.ObterPorCnpjAsync(DocumentoFiscal.Create(dados.DocumentoFiscal).Value, userId, cancellationToken);
        var alteradoEm = externo.UltimaAlteracaoEm ?? DateTimeOffset.UtcNow;
        if (local is null)
        {
            local = new Fornecedor(Guid.NewGuid(), dados.RazaoSocial, DocumentoFiscal.Create(dados.DocumentoFiscal), dados.TipoPessoa, null,
                dados.EmailComercial, dados.Telefone, null, dados.Cidade, dados.Uf, dados.Pais, dados.Ativo ? "Ativo" : "Inativo", null,
                userId, alteradoEm, businessUnit, externo.ErpSistema, externo.ErpFornecedorId);
            local.AplicarContratoCanonico(dados, "ERP", alteradoEm);
            local.RegistrarVinculoErp(businessUnit, externo.ErpSistema, externo.ErpFornecedorId);
            local.RegistrarSincronizacao("Sincronizado", DateTimeOffset.UtcNow);
            await repository.AdicionarAsync(local, cancellationToken);
            execucao.RegistrarIncluido();
            return;
        }

        local.RegistrarVinculoErp(businessUnit, externo.ErpSistema, externo.ErpFornecedorId);
        if (EstaSemAlteracao(local, dados))
        {
            local.RegistrarSincronizacao("Sincronizado", DateTimeOffset.UtcNow);
            await repository.AtualizarAsync(local, cancellationToken);
            execucao.RegistrarSemAlteracao();
            return;
        }

        local.AplicarContratoCanonico(dados, "ERP", alteradoEm);
        local.RegistrarVinculoErp(businessUnit, externo.ErpSistema, externo.ErpFornecedorId);
        local.RegistrarSincronizacao("Sincronizado", DateTimeOffset.UtcNow);
        await repository.AtualizarAsync(local, cancellationToken);
        execucao.RegistrarAtualizado();
    }

    private static string Identificar(FornecedorErpIntegracaoDto externo) =>
        string.Join(" | ", new[] { externo.ErpSistema, externo.ErpFornecedorId, externo.Dados.DocumentoFiscal }.Where(x => !string.IsNullOrWhiteSpace(x)));

    private static bool EstaSemAlteracao(Fornecedor local, FornecedorCanonico dados)
    {
        if (!string.IsNullOrWhiteSpace(local.HashDadosSincronizaveis) && local.HashDadosSincronizaveis == dados.HashDadosSincronizaveis) return true;
        return local.RazaoSocial == dados.RazaoSocial.Trim()
            && local.Cnpj_Cpf == DocumentoFiscal.Create(dados.DocumentoFiscal).Value
            && local.NomeFantasia == dados.NomeFantasia?.Trim()
            && local.TipoPessoa == dados.TipoPessoa?.Trim()
            && local.Cidade == dados.Cidade
            && local.Estado == dados.Uf
            && local.Status == (dados.Ativo ? "Ativo" : "Inativo");
    }
}
