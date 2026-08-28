using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using BlueprintOS.Application.Procurement.Suppliers.Models;
using BlueprintOS.Domain.Procurement.Suppliers;
using BlueprintOS.Infrastructure.Integrations.ERP.Contracts;
using BlueprintOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BlueprintOS.Infrastructure.Integrations.ERP.Soma;

public sealed class SincronizarFornecedoresErpUseCase(
    IFornecedorErpReader reader,
    IFornecedorRepository repository,
    ISincronizacaoFornecedorMonitorRepository monitorRepository,
    ICurrentIdentity identity,
    BlueprintOSDbContext context,
    ILogger<SincronizarFornecedoresErpUseCase> logger) : ISincronizarFornecedoresErpUseCase
{
    private const string ErpSistema = "SOMA_DESENV";
    private const int TamanhoPaginaPadrao = 500;

    // Requisito 4b — limiar de inativação anormal: escolhido como 30% dos fornecedores hoje Ativos.
    // Racional: uma sincronização legítima costuma inativar uma fração pequena da base por execução
    // (rescisões pontuais de contrato/fornecedores desativados no ERP); um salto acima de 30% é muito
    // mais compatível com um problema estrutural do lado do ERP (filtro errado, corte de conexão no meio
    // da paginação, campo "Ativo" invertido/nulo tratado como falso) do que com uma alteração de negócio
    // real. É deliberadamente conservador: preferimos abortar inativações legítimas raras (que podem ser
    // reprocessadas manualmente) a aplicar uma inativação em massa incorreta, que é destrutiva para o
    // fluxo de compras (fornecedor Inativo para de aparecer em cotações/pedidos).
    private const decimal LimiarInativacaoAnormal = 0.30m;

    // Requisito 5 — estratégia de batching: cada Fornecedor processado ainda passa por
    // repository.AdicionarAsync/AtualizarAsync, que persistem individualmente (SaveChangesAsync por
    // registro) — isso é preservado deliberadamente porque é o que garante o isolamento de erro parcial
    // já testado/homologado em B2.9 (um registro com falha de SaveChanges não derruba os demais). O que
    // o código legado NÃO fazia era liberar as entidades de Fornecedor já salvas do ChangeTracker — ao
    // longo de 78-96k iterações, o ChangeTracker acumulava todas elas (cada AutoDetectChanges/SaveChanges
    // subsequente varre um grafo cada vez maior). A cada TamanhoBatchTracker registros de Fornecedor
    // processados com sucesso, desanexamos apenas as entradas do tipo Fornecedor do ChangeTracker
    // (nunca um Clear() geral): como cada escrita já foi persistida individualmente antes de desanexar,
    // isso é seguro, e preserva a própria SincronizacaoFornecedor (e os erros já registrados nela)
    // rastreada durante toda a execução — um Clear() geral a desanexaria também, fazendo com que o
    // SaveChangesAsync final não persistisse nem o status final nem os erros acumulados. Combinado com
    // as leituras via ObterPorCnpjSemRastreamentoAsync (AsNoTracking), o ChangeTracker nunca cresce além
    // de um pequeno número de entidades de Fornecedor por vez.
    private const int TamanhoBatchTracker = 300;

    public async Task<SincronizacaoFornecedoresErpResumo> ExecuteAsync(SincronizarFornecedoresErpDto dto, CancellationToken cancellationToken = default)
    {
        var identidadeAtual = identity.GetRequired();
        var userId = identidadeAtual.UserId;
        // DEB-03 (Gate Final da Onda 1) — a Unidade de Negocio da execucao e sempre a da sessao que a
        // disparou, nunca inferida do BusinessUnit de texto livre informado no corpo da requisicao;
        // falha fechado se a sessao nao tiver Unidade de Negocio resolvida (RequestIdentity.cs).
        if (identidadeAtual.UnidadeNegocioId is null || identidadeAtual.UnidadeNegocioId == Guid.Empty)
        {
            throw new InvalidOperationException("A sessão atual não possui Unidade de Negócio resolvida; sincronização ERP não pode ser iniciada.");
        }
        var unidadeNegocioId = identidadeAtual.UnidadeNegocioId.Value;
        var correlationId = string.IsNullOrWhiteSpace(dto.CorrelationId) ? Guid.NewGuid().ToString("N") : dto.CorrelationId.Trim()[..Math.Min(dto.CorrelationId.Trim().Length, 100)];
        var businessUnit = string.IsNullOrWhiteSpace(dto.BusinessUnit) ? "DEFAULT" : dto.BusinessUnit.Trim();
        var dryRun = dto.DryRun;

        // Requisito 3 — Limite <= 0 (ou não informado) deixa de ter um teto artificial (era
        // Math.Clamp(..., 1, 5000)): agora pagina até a fonte devolver uma página vazia (fim natural).
        // Limite > 0 continua sendo respeitado como teto explícito informado pelo chamador.
        var limiteExplicito = dto.Limite > 0 ? dto.Limite : (int?)null;

        // Requisito 4c — proteção contra execução concorrente para a mesma BU. Não se aplica a dry-run:
        // dry-run não persiste nada e pode ser disparado livremente para inspecionar o estado atual,
        // inclusive enquanto uma execução real está em andamento.
        if (!dryRun)
        {
            var jaEmAndamento = await monitorRepository.ExisteEmAndamentoAsync(unidadeNegocioId, businessUnit, cancellationToken);
            if (jaEmAndamento)
            {
                throw new InvalidOperationException(
                    $"Já existe uma sincronização de fornecedores ERP em andamento para a BusinessUnit '{businessUnit}'. Aguarde a conclusão antes de disparar uma nova execução.");
            }
        }

        // Requisito 4b — denominador do percentual de inativação: total de fornecedores já Ativos
        // localmente ANTES desta execução.
        var totalAtivosAntes = await repository.ContarAtivosAsync(userId, cancellationToken);

        var inicio = DateTimeOffset.UtcNow;
        var execucao = new SincronizacaoFornecedor(Guid.NewGuid(), ErpSistema, businessUnit, inicio, unidadeNegocioId);

        if (!dryRun)
        {
            // Persistido imediatamente como "EmAndamento" para que a guarda de concorrência de outra
            // execução disparada em paralelo consiga encontrar este registro. Este objeto permanece
            // rastreado pelo DbContext durante toda a execução (ver comentário de TamanhoBatchTracker).
            execucao.MarcarEmAndamento();
            await context.SincronizacoesFornecedores.AddAsync(execucao, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }

        logger.LogInformation("Sincronizacao de fornecedores ERP iniciada. ExecucaoId {ExecucaoId}. BusinessUnit {BusinessUnit}. LimiteExplicito {LimiteExplicito}. DryRun {DryRun}. CorrelationId {CorrelationId}",
            execucao.Id, businessUnit, limiteExplicito, dryRun, correlationId);

        var skip = 0;
        var primeiraPagina = true;
        var possivelmenteTruncado = false;
        var registrosParaInativar = new List<(Fornecedor Local, FornecedorErpIntegracaoDto Externo, DateTimeOffset AlteradoEm)>();
        var registrosDesdeUltimoBatch = 0;

        while (true)
        {
            int tamanhoPagina;
            if (limiteExplicito.HasValue)
            {
                var restante = limiteExplicito.Value - execucao.TotalConsultado;
                if (restante <= 0) break;
                tamanhoPagina = Math.Min(TamanhoPaginaPadrao, restante);
            }
            else
            {
                tamanhoPagina = TamanhoPaginaPadrao;
            }

            var lote = await reader.BuscarFornecedoresAsync(skip, tamanhoPagina, cancellationToken);

            if (primeiraPagina && lote.Count == 0)
            {
                // Guarda 4a — primeira página vazia é tratada como anomalia da fonte, nunca como
                // "nada para sincronizar". Não persistimos como sucesso vazio.
                var fimAborto = DateTimeOffset.UtcNow;
                execucao.AbortarFonteVazia(fimAborto);
                if (!dryRun)
                {
                    await context.SaveChangesAsync(cancellationToken);
                }

                logger.LogWarning("Sincronizacao de fornecedores ERP abortada: fonte retornou zero registros na primeira pagina. ExecucaoId {ExecucaoId}. BusinessUnit {BusinessUnit}",
                    execucao.Id, businessUnit);

                return Resumo(execucao, inicio, fimAborto, businessUnit, correlationId, totalInativados: 0, possivelmenteTruncado: false);
            }

            primeiraPagina = false;
            if (lote.Count == 0) break;

            foreach (var externo in lote)
            {
                execucao.RegistrarConsultado();
                try
                {
                    var inativou = await SincronizarFornecedorAsync(externo, businessUnit, userId, execucao, dryRun, registrosParaInativar, cancellationToken);
                    if (!dryRun && !inativou)
                    {
                        registrosDesdeUltimoBatch++;
                        if (registrosDesdeUltimoBatch >= TamanhoBatchTracker)
                        {
                            DesanexarFornecedoresRastreados();
                            registrosDesdeUltimoBatch = 0;
                        }
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // Uma entidade cujo SaveChangesAsync falhou continua rastreada como Added/Modified.
                    // Sem desanexá-la, o proximo SaveChangesAsync (inclusive o final, ao persistir a
                    // SincronizacaoFornecedor) tenta salva-la de novo e repete a falha, transformando um
                    // erro parcial em erro fatal para a execucao inteira. Desanexamos só as entradas de
                    // Fornecedor (nunca a própria execucao, que precisa permanecer rastreada).
                    DesanexarFornecedoresRastreados();
                    registrosDesdeUltimoBatch = 0;
                    execucao.RegistrarErro(Identificar(externo), ex, DateTimeOffset.UtcNow);
                    // Adiciona explicitamente o novo erro ao ChangeTracker como Added: a entidade é
                    // criada dentro do agregado (Guid client-side já preenchido) e só aparece na coleção
                    // Erros depois que a SincronizacaoFornecedor já está rastreada — sem isto, a detecção
                    // automática de mudanças pode reconciliá-la como "Modified" (achando que já existe no
                    // banco por já ter uma chave não vazia) e o SaveChangesAsync final falha tentando
                    // atualizar uma linha que nunca foi inserida.
                    context.Add(execucao.Erros.Last());
                    logger.LogError(ex, "Erro parcial na sincronizacao de fornecedor ERP. ExecucaoId {ExecucaoId}. Fornecedor {FornecedorIdentificacao}",
                        execucao.Id, Identificar(externo));
                }
            }

            logger.LogInformation("Lote de fornecedores ERP processado. ExecucaoId {ExecucaoId}. Skip {Skip}. ProcessadosNoLote {ProcessadosNoLote}. Consultados {Consultados}. Erros {Erros}",
                execucao.Id, skip, lote.Count, execucao.TotalConsultado, execucao.TotalErro);
            skip += lote.Count;

            if (limiteExplicito.HasValue && execucao.TotalConsultado >= limiteExplicito.Value)
            {
                // Requisito 3 — se paramos por ter batido no teto explícito e a última página ainda
                // trazia registros (tamanho do lote igual ao solicitado), há indício de que a fonte tem
                // mais dados do que processamos.
                possivelmenteTruncado = lote.Count == tamanhoPagina;
                break;
            }
        }

        var totalAtivosCandidatos = registrosParaInativar.Count;
        var percentualInativacao = totalAtivosAntes == 0
            ? (totalAtivosCandidatos > 0 ? 1m : 0m)
            : (decimal)totalAtivosCandidatos / totalAtivosAntes;

        int totalInativados;
        if (dryRun)
        {
            // Em dry-run nada é persistido; apenas reportamos quantos registros SERIAM inativados.
            totalInativados = totalAtivosCandidatos;
            var fimDryRun = DateTimeOffset.UtcNow;
            execucao.ConcluirDryRun(fimDryRun);
            logger.LogInformation("Sincronizacao de fornecedores ERP (dry-run) finalizada. ExecucaoId {ExecucaoId}. Consultados {Consultados}. Incluidos {Incluidos}. Atualizados {Atualizados}. SemAlteracao {SemAlteracao}. Erros {Erros}. TotalInativadosSimulado {TotalInativados}",
                execucao.Id, execucao.TotalConsultado, execucao.TotalIncluido, execucao.TotalAtualizado, execucao.TotalSemAlteracao, execucao.TotalErro, totalInativados);
            return Resumo(execucao, inicio, fimDryRun, businessUnit, correlationId, totalInativados, possivelmenteTruncado);
        }

        if (percentualInativacao > LimiarInativacaoAnormal)
        {
            // Guarda 4b — decisão de projeto: abortamos APENAS as inativações, não a execução inteira.
            // Os registros Incluídos/Atualizados/SemAlteração já persistidos ao longo do laço acima são
            // mudanças independentes e legítimas (nada nelas indica anomalia); descartá-los também não
            // reduziria o risco identificado (inativação em massa) e ainda obrigaria reprocessar dados
            // corretos. As inativações candidatas simplesmente não são aplicadas: os fornecedores
            // envolvidos permanecem com o Status anterior (Ativo) até uma nova execução ou revisão manual.
            totalInativados = 0;
            var fimAbortoInativacao = DateTimeOffset.UtcNow;
            execucao.AbortarInativacaoAnormal(fimAbortoInativacao);
            await context.SaveChangesAsync(cancellationToken);

            logger.LogWarning("Sincronizacao de fornecedores ERP: percentual de inativacao anormal detectado e abortado. ExecucaoId {ExecucaoId}. CandidatosInativacao {CandidatosInativacao}. TotalAtivosAntes {TotalAtivosAntes}. Percentual {Percentual:P1}",
                execucao.Id, totalAtivosCandidatos, totalAtivosAntes, percentualInativacao);

            return Resumo(execucao, inicio, fimAbortoInativacao, businessUnit, correlationId, totalInativados, possivelmenteTruncado);
        }

        foreach (var (local, externo, alteradoEm) in registrosParaInativar)
        {
            local.AplicarContratoCanonico(externo.Dados, "ERP", alteradoEm);
            local.RegistrarVinculoErp(businessUnit, externo.ErpSistema, externo.ErpFornecedorId);
            local.RegistrarSincronizacao("Sincronizado", DateTimeOffset.UtcNow);
            await repository.AtualizarAsync(local, cancellationToken);
            execucao.RegistrarAtualizado();
            registrosDesdeUltimoBatch++;
            if (registrosDesdeUltimoBatch >= TamanhoBatchTracker)
            {
                DesanexarFornecedoresRastreados();
                registrosDesdeUltimoBatch = 0;
            }
        }
        totalInativados = registrosParaInativar.Count;

        var fim = DateTimeOffset.UtcNow;
        execucao.Finalizar(fim);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Sincronizacao de fornecedores ERP finalizada. ExecucaoId {ExecucaoId}. Status {Status}. Consultados {Consultados}. Incluidos {Incluidos}. Atualizados {Atualizados}. SemAlteracao {SemAlteracao}. Erros {Erros}. TotalInativados {TotalInativados}. DuracaoMs {DuracaoMs}",
            execucao.Id, execucao.Status, execucao.TotalConsultado, execucao.TotalIncluido, execucao.TotalAtualizado, execucao.TotalSemAlteracao, execucao.TotalErro, totalInativados, execucao.TempoExecucaoMs);

        return Resumo(execucao, inicio, fim, businessUnit, correlationId, totalInativados, possivelmenteTruncado);
    }

    /// <summary>Requisito 5 — desanexa apenas as entradas do tipo <see cref="Fornecedor"/> do
    /// ChangeTracker (nunca um Clear() geral), preservando a <see cref="SincronizacaoFornecedor"/> desta
    /// execução (e os erros já registrados nela) rastreada do início ao fim.</summary>
    private void DesanexarFornecedoresRastreados()
    {
        foreach (var entry in context.ChangeTracker.Entries<Fornecedor>().ToList())
        {
            entry.State = EntityState.Detached;
        }
    }

    private static SincronizacaoFornecedoresErpResumo Resumo(SincronizacaoFornecedor execucao, DateTimeOffset inicio, DateTimeOffset fim,
        string businessUnit, string correlationId, int totalInativados, bool possivelmenteTruncado) => new(
        execucao.Id, execucao.Status, inicio, fim, execucao.TotalConsultado, execucao.TotalIncluido,
        execucao.TotalAtualizado, execucao.TotalSemAlteracao, execucao.TotalErro, execucao.TempoExecucaoMs,
        businessUnit, ErpSistema, correlationId, fim, totalInativados, possivelmenteTruncado);

    /// <summary>Classifica e (fora do modo dry-run e fora do caso "seria inativado") persiste um
    /// registro do ERP. Retorna true quando o registro seria uma inativação (Ativo -> Inativo): nesse
    /// caso a persistência é deliberadamente adiada — o chamador só a aplica depois de confirmar, ao
    /// final do laço de páginas, que o percentual de inativação da execução não é anormal (guarda 4b).</summary>
    private async Task<bool> SincronizarFornecedorAsync(FornecedorErpIntegracaoDto externo, string businessUnit, Guid userId,
        SincronizacaoFornecedor execucao, bool dryRun,
        List<(Fornecedor Local, FornecedorErpIntegracaoDto Externo, DateTimeOffset AlteradoEm)> registrosParaInativar,
        CancellationToken cancellationToken)
    {
        var dados = externo.Dados;
        if (string.IsNullOrWhiteSpace(dados.RazaoSocial)) throw new ArgumentException("RazaoSocial do fornecedor ERP e obrigatoria.");
        if (string.IsNullOrWhiteSpace(dados.DocumentoFiscal)) throw new ArgumentException("DocumentoFiscal do fornecedor ERP e obrigatorio.");

        var documento = DocumentoFiscal.Create(dados.DocumentoFiscal).Value;
        // Requisito 5(a) — leitura sem rastreamento: neste ponto ainda não sabemos se vamos escrever
        // (e, se formos, o AtualizarAsync/DbSet.Update reatacha explicitamente na hora de persistir).
        var local = await repository.ObterPorCnpjSemRastreamentoAsync(documento, userId, cancellationToken);
        var alteradoEm = externo.UltimaAlteracaoEm ?? DateTimeOffset.UtcNow;

        if (local is null)
        {
            if (dryRun)
            {
                execucao.RegistrarIncluido();
                return false;
            }

            var novo = new Fornecedor(Guid.NewGuid(), dados.RazaoSocial, DocumentoFiscal.Create(dados.DocumentoFiscal), dados.TipoPessoa, null,
                dados.EmailComercial, dados.Telefone, null, dados.Cidade, dados.Uf, dados.Pais, dados.Ativo ? "Ativo" : "Inativo", null,
                userId, alteradoEm, businessUnit, externo.ErpSistema, externo.ErpFornecedorId);
            novo.AplicarContratoCanonico(dados, "ERP", alteradoEm);
            novo.RegistrarVinculoErp(businessUnit, externo.ErpSistema, externo.ErpFornecedorId);
            novo.RegistrarSincronizacao("Sincronizado", DateTimeOffset.UtcNow);
            await repository.AdicionarAsync(novo, cancellationToken);
            execucao.RegistrarIncluido();
            return false;
        }

        if (EstaSemAlteracao(local, dados))
        {
            if (dryRun)
            {
                execucao.RegistrarSemAlteracao();
                return false;
            }

            local.RegistrarVinculoErp(businessUnit, externo.ErpSistema, externo.ErpFornecedorId);
            local.RegistrarSincronizacao("Sincronizado", DateTimeOffset.UtcNow);
            await repository.AtualizarAsync(local, cancellationToken);
            execucao.RegistrarSemAlteracao();
            return false;
        }

        // Requisito 2/4b — "seria inativado": já é Ativo localmente e o ERP agora diz Inativo.
        var seriaInativado = local.Status == "Ativo" && !dados.Ativo;

        if (dryRun)
        {
            execucao.RegistrarAtualizado();
            if (seriaInativado)
            {
                registrosParaInativar.Add((local, externo, alteradoEm));
            }
            return false;
        }

        if (seriaInativado)
        {
            // Persistência adiada até sabermos que o percentual de inativação da execução é seguro.
            registrosParaInativar.Add((local, externo, alteradoEm));
            return true;
        }

        local.AplicarContratoCanonico(dados, "ERP", alteradoEm);
        local.RegistrarVinculoErp(businessUnit, externo.ErpSistema, externo.ErpFornecedorId);
        local.RegistrarSincronizacao("Sincronizado", DateTimeOffset.UtcNow);
        await repository.AtualizarAsync(local, cancellationToken);
        execucao.RegistrarAtualizado();
        return false;
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
