using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using BlueprintOS.Application.Procurement.Suppliers.Models;
using BlueprintOS.Domain.Procurement.Suppliers;
using BlueprintOS.Infrastructure.Integrations.ERP.Contracts;
using BlueprintOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BlueprintOS.Infrastructure.Integrations.ERP.Soma;

/// <summary>B3 — Bloco 5A/5A.9: sincronização Linx -> +Compras de Fornecedores. Modelo homologado pelo
/// Product Owner (GAPs KALUNGA/PLATINUM descobertos no Bloco 5A): 1 CNPJ/CPF = 1 <see cref="Fornecedor"/>
/// +Compras, que pode possuir N <see cref="FornecedorLinxVinculo"/> — um por `COD_FORNECEDOR` Linx real
/// (comprovado real: 1.856 CNPJs no Linx com 2+ códigos, 1.302 já sincronizados localmente antes deste
/// modelo, sujeitos à sobrescrita não determinística que este redesenho elimina).
///
/// Regras homologadas aplicadas por linha do Linx processada:
/// - Vínculo ATIVO exige `CADASTRO_CLI_FOR.INATIVO=0 AND FORNECEDORES.INATIVO=0` (CADASTRO_CLI_FOR é
///   master — decisão do Product Owner, Bloco 5A.9).
/// - Fonte cadastral do Fornecedor (RazaoSocial/NomeFantasia/endereço/etc.) é sempre o vínculo ATIVO com
///   maior `FORNECEDORES.DATA_PARA_TRANSFERENCIA` entre os vínculos do mesmo CNPJ — nunca o Principal, nunca
///   CLIFOR, nunca ordem de SELECT/sincronização. "Mais recente" e "Principal" são conceitos independentes.
/// - Principal só é atribuído automaticamente quando o CNPJ entra pela primeira vez OU quando o Fornecedor
///   não tem NENHUM vínculo com Principal=true (mesmo histórico/inativo) — nunca substituído
///   automaticamente por um vínculo mais recente. Empate no maior DATA_PARA_TRANSFERENCIA entre vínucos
///   ativos elegíveis não inventa desempate — fica sem Principal, registrado como ocorrência.
/// - Um vínculo Principal que se torna inativo mantém Principal=true (histórico) — nunca promove outro
///   automaticamente; a escolha de um novo Principal ativo pertence exclusivamente ao comprador.
/// - `Fornecedor.ErpFornecedorId`/`ErpSistema` (identidade ERP legada, pré-Bloco 5A.9) são mantidos por
///   compatibilidade e espelham o vínculo Principal ATIVO atual — nunca mais a fonte canônica para novas
///   resolvões de identidade (essa é sempre <see cref="FornecedorLinxVinculo"/>).
///
/// GAP KALUNGA: qualquer exceção não tratada fora do laço por registro finaliza a execução com status
/// terminal explícito (nunca mais fica presa em "EmAndamento" indefinidamente).</summary>
public sealed class SincronizarFornecedoresErpUseCase(
    IFornecedorErpReader reader,
    IFornecedorRepository repository,
    IFornecedorLinxVinculoRepository vinculoRepository,
    ISincronizacaoFornecedorMonitorRepository monitorRepository,
    ICurrentIdentity identity,
    BlueprintOSDbContext context,
    ILogger<SincronizarFornecedoresErpUseCase> logger) : ISincronizarFornecedoresErpUseCase
{
    private const string ErpSistema = "SOMA_DESENV";
    private const int TamanhoPaginaPadrao = 500;
    private const decimal LimiarInativacaoAnormal = 0.30m;
    private const int TamanhoBatchTracker = 300;

    public async Task<SincronizacaoFornecedoresErpResumo> ExecuteAsync(SincronizarFornecedoresErpDto dto, CancellationToken cancellationToken = default)
    {
        var identidadeAtual = identity.GetRequired();
        if (identidadeAtual.UnidadeNegocioId is null || identidadeAtual.UnidadeNegocioId == Guid.Empty)
        {
            throw new InvalidOperationException("A sessão atual não possui Unidade de Negócio resolvida; sincronização ERP não pode ser iniciada.");
        }
        var unidadeNegocioId = identidadeAtual.UnidadeNegocioId.Value;
        var correlationId = string.IsNullOrWhiteSpace(dto.CorrelationId) ? Guid.NewGuid().ToString("N") : dto.CorrelationId.Trim()[..Math.Min(dto.CorrelationId.Trim().Length, 100)];
        var businessUnit = string.IsNullOrWhiteSpace(dto.BusinessUnit) ? "DEFAULT" : dto.BusinessUnit.Trim();
        var dryRun = dto.DryRun;
        var limiteExplicito = dto.Limite > 0 ? dto.Limite : (int?)null;

        if (!dryRun)
        {
            var jaEmAndamento = await monitorRepository.ExisteEmAndamentoAsync(unidadeNegocioId, businessUnit, cancellationToken);
            if (jaEmAndamento)
            {
                throw new InvalidOperationException(
                    $"Já existe uma sincronização de fornecedores ERP em andamento para a BusinessUnit '{businessUnit}'. Aguarde a conclusão antes de disparar uma nova execução, ou recupere administrativamente uma execução comprovadamente abandonada.");
            }
        }

        var totalAtivosAntes = await repository.ContarAtivosAsync(unidadeNegocioId, cancellationToken);
        var inicio = DateTimeOffset.UtcNow;
        var execucao = new SincronizacaoFornecedor(Guid.NewGuid(), ErpSistema, businessUnit, inicio, unidadeNegocioId);

        if (!dryRun)
        {
            execucao.MarcarEmAndamento();
            await context.SincronizacoesFornecedores.AddAsync(execucao, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }

        try
        {
            return await ExecutarAsync(dto, execucao, businessUnit, unidadeNegocioId, correlationId, limiteExplicito, dryRun, totalAtivosAntes, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // GAP KALUNGA (Bloco 5A.9) — causa raiz comprovada: sem este catch, qualquer falha fora do
            // tratamento por registro (conexão perdida, timeout, processo encerrado) propagava sem nunca
            // finalizar `execucao`, deixando o registro preso em "EmAndamento" para sempre e bloqueando a
            // guarda de concorrência de toda execução real futura. Usa CancellationToken.None
            // deliberadamente: esta finalização precisa persistir mesmo que o token da chamada original
            // não seja mais confiável.
            if (!dryRun)
            {
                DesanexarFornecedoresRastreados();
                var fimFatal = DateTimeOffset.UtcNow;
                execucao.AbortarPorFalhaFatal(fimFatal, ex);
                await context.SaveChangesAsync(CancellationToken.None);
                logger.LogError(ex, "Falha fatal na sincronizacao de fornecedores ERP — execucao abortada com status terminal. ExecucaoId {ExecucaoId}", execucao.Id);
            }
            throw;
        }
    }

    private async Task<SincronizacaoFornecedoresErpResumo> ExecutarAsync(
        SincronizarFornecedoresErpDto dto, SincronizacaoFornecedor execucao, string businessUnit, Guid unidadeNegocioId, string correlationId,
        int? limiteExplicito, bool dryRun, int totalAtivosAntes, CancellationToken cancellationToken)
    {
        var inicio = execucao.DataInicio;
        var skip = 0;
        var primeiraPagina = true;
        var possivelmenteTruncado = false;
        var registrosParaInativar = new List<(Fornecedor Local, FornecedorErpIntegracaoDto Externo, DateTimeOffset AlteradoEm)>();
        var registrosDesdeUltimoBatch = 0;
        var ocorrenciasVinculos = new List<string>();

        logger.LogInformation("Sincronizacao de fornecedores ERP iniciada. ExecucaoId {ExecucaoId}. BusinessUnit {BusinessUnit}. LimiteExplicito {LimiteExplicito}. DryRun {DryRun}. CorrelationId {CorrelationId}",
            execucao.Id, businessUnit, limiteExplicito, dryRun, correlationId);

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
                var fimAborto = DateTimeOffset.UtcNow;
                execucao.AbortarFonteVazia(fimAborto);
                if (!dryRun) await context.SaveChangesAsync(cancellationToken);
                logger.LogWarning("Sincronizacao de fornecedores ERP abortada: fonte retornou zero registros na primeira pagina. ExecucaoId {ExecucaoId}. BusinessUnit {BusinessUnit}", execucao.Id, businessUnit);
                return Resumo(execucao, inicio, fimAborto, businessUnit, correlationId, totalInativados: 0, possivelmenteTruncado: false, ocorrenciasVinculos);
            }

            primeiraPagina = false;
            if (lote.Count == 0) break;

            foreach (var externo in lote)
            {
                execucao.RegistrarConsultado();
                try
                {
                    var inativou = await SincronizarFornecedorAsync(externo, businessUnit, unidadeNegocioId, execucao, dryRun, registrosParaInativar, ocorrenciasVinculos, cancellationToken);
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
                    DesanexarFornecedoresRastreados();
                    registrosDesdeUltimoBatch = 0;
                    execucao.RegistrarErro(Identificar(externo), ex, DateTimeOffset.UtcNow);
                    context.Add(execucao.Erros.Last());
                    logger.LogError(ex, "Erro parcial na sincronizacao de fornecedor ERP. ExecucaoId {ExecucaoId}. Fornecedor {FornecedorIdentificacao}", execucao.Id, Identificar(externo));
                }
            }

            logger.LogInformation("Lote de fornecedores ERP processado. ExecucaoId {ExecucaoId}. Skip {Skip}. ProcessadosNoLote {ProcessadosNoLote}. Consultados {Consultados}. Erros {Erros}",
                execucao.Id, skip, lote.Count, execucao.TotalConsultado, execucao.TotalErro);
            skip += lote.Count;

            if (limiteExplicito.HasValue && execucao.TotalConsultado >= limiteExplicito.Value)
            {
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
            totalInativados = totalAtivosCandidatos;
            var fimDryRun = DateTimeOffset.UtcNow;
            execucao.ConcluirDryRun(fimDryRun);
            logger.LogInformation("Sincronizacao de fornecedores ERP (dry-run) finalizada. ExecucaoId {ExecucaoId}. Consultados {Consultados}. Incluidos {Incluidos}. Atualizados {Atualizados}. SemAlteracao {SemAlteracao}. Erros {Erros}. TotalInativadosSimulado {TotalInativados}",
                execucao.Id, execucao.TotalConsultado, execucao.TotalIncluido, execucao.TotalAtualizado, execucao.TotalSemAlteracao, execucao.TotalErro, totalInativados);
            return Resumo(execucao, inicio, fimDryRun, businessUnit, correlationId, totalInativados, possivelmenteTruncado, ocorrenciasVinculos);
        }

        if (percentualInativacao > LimiarInativacaoAnormal)
        {
            totalInativados = 0;
            var fimAbortoInativacao = DateTimeOffset.UtcNow;
            execucao.AbortarInativacaoAnormal(fimAbortoInativacao);
            await context.SaveChangesAsync(cancellationToken);
            logger.LogWarning("Sincronizacao de fornecedores ERP: percentual de inativacao anormal detectado e abortado. ExecucaoId {ExecucaoId}. CandidatosInativacao {CandidatosInativacao}. TotalAtivosAntes {TotalAtivosAntes}. Percentual {Percentual:P1}",
                execucao.Id, totalAtivosCandidatos, totalAtivosAntes, percentualInativacao);
            return Resumo(execucao, inicio, fimAbortoInativacao, businessUnit, correlationId, totalInativados, possivelmenteTruncado, ocorrenciasVinculos);
        }

        foreach (var (local, _, alteradoEm) in registrosParaInativar)
        {
            // A linha que causou a inativação nunca é fonte cadastral (vínculo inativo) — só a situação
            // (Ativo -> Inativo) muda; os campos descritivos preservam o último valor de uma fonte ativa.
            local.AlterarStatus(false, alteradoEm, "ERP");
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

        return Resumo(execucao, inicio, fim, businessUnit, correlationId, totalInativados, possivelmenteTruncado, ocorrenciasVinculos);
    }

    private void DesanexarFornecedoresRastreados()
    {
        foreach (var entry in context.ChangeTracker.Entries<Fornecedor>().ToList()) entry.State = EntityState.Detached;
        foreach (var entry in context.ChangeTracker.Entries<FornecedorLinxVinculo>().ToList()) entry.State = EntityState.Detached;
    }

    private static SincronizacaoFornecedoresErpResumo Resumo(SincronizacaoFornecedor execucao, DateTimeOffset inicio, DateTimeOffset fim,
        string businessUnit, string correlationId, int totalInativados, bool possivelmenteTruncado, IReadOnlyList<string> ocorrenciasVinculos) => new(
        execucao.Id, execucao.Status, inicio, fim, execucao.TotalConsultado, execucao.TotalIncluido,
        execucao.TotalAtualizado, execucao.TotalSemAlteracao, execucao.TotalErro, execucao.TempoExecucaoMs,
        businessUnit, ErpSistema, correlationId, fim, totalInativados, possivelmenteTruncado, ocorrenciasVinculos);

    /// <summary>Processa uma linha do Linx (um `COD_FORNECEDOR`). Retorna true quando o Fornecedor "seria
    /// inativado" (persistência adiada para a guarda de inativação em massa) — mesmo contrato de antes.</summary>
    private async Task<bool> SincronizarFornecedorAsync(FornecedorErpIntegracaoDto externo, string businessUnit, Guid unidadeNegocioId,
        SincronizacaoFornecedor execucao, bool dryRun,
        List<(Fornecedor Local, FornecedorErpIntegracaoDto Externo, DateTimeOffset AlteradoEm)> registrosParaInativar,
        List<string> ocorrenciasVinculos, CancellationToken cancellationToken)
    {
        var dados = externo.Dados;
        if (string.IsNullOrWhiteSpace(dados.RazaoSocial)) throw new ArgumentException("RazaoSocial do fornecedor ERP e obrigatoria.");
        if (string.IsNullOrWhiteSpace(dados.DocumentoFiscal)) throw new ArgumentException("DocumentoFiscal do fornecedor ERP e obrigatorio.");

        var documento = DocumentoFiscal.Create(dados.DocumentoFiscal).Value;
        var alteradoEm = externo.UltimaAlteracaoEm ?? DateTimeOffset.UtcNow;
        // Decisão do Product Owner (Bloco 5A.9): CADASTRO_CLI_FOR é master — vínculo só é Ativo quando
        // NENHUMA das duas tabelas o marca inativo.
        var inativoFornecedores = !dados.Ativo;
        var vinculoAtivoLinx = !inativoFornecedores && !externo.InativoCadastroCliFor;

        var local = await repository.ObterPorCnpjSemRastreamentoAsync(documento, unidadeNegocioId, cancellationToken);

        if (local is null)
        {
            if (dryRun)
            {
                execucao.RegistrarIncluido();
                return false;
            }

            var novo = new Fornecedor(Guid.NewGuid(), dados.RazaoSocial, DocumentoFiscal.Create(dados.DocumentoFiscal), dados.TipoPessoa, null,
                dados.EmailComercial, dados.Telefone, null, dados.Cidade, dados.Uf, dados.Pais, dados.Ativo ? "Ativo" : "Inativo", null,
                alteradoEm, unidadeNegocioId, businessUnit, externo.ErpSistema, externo.ErpFornecedorId);
            novo.AplicarContratoCanonico(dados, "ERP", alteradoEm);
            // Caso A (entrada pela primeira vez): o único vínculo conhecido só pode ser Principal se
            // Ativo — um vínculo inativo nunca pode ser Principal (decisão do Product Owner).
            var vinculo = new FornecedorLinxVinculo(novo.Id, unidadeNegocioId, externo.ErpSistema, externo.ErpFornecedorId, dados.NomeFantasia ?? string.Empty,
                inativoFornecedores, externo.InativoCadastroCliFor, externo.UltimaAlteracaoEm, principal: vinculoAtivoLinx, agora: alteradoEm);
            if (vinculoAtivoLinx)
            {
                novo.RegistrarVinculoErp(businessUnit, externo.ErpSistema, externo.ErpFornecedorId);
            }
            await repository.AdicionarAsync(novo, cancellationToken);
            await vinculoRepository.AdicionarAsync(vinculo, cancellationToken);
            await vinculoRepository.SalvarAlteracoesAsync(cancellationToken);
            execucao.RegistrarIncluido();
            return false;
        }

        // Fornecedor já existe (mesmo CNPJ) — upsert do vínculo específico deste COD_FORNECEDOR, nunca
        // sobrescrevendo os demais (causa raiz do GAP PLATINUM eliminada: cada COD_FORNECEDOR tem sua
        // própria linha, nunca compete por um único campo escalar).
        if (dryRun)
        {
            var vinculoExistenteDry = await vinculoRepository.ObterPorErpSistemaECodigoAsync(externo.ErpSistema, externo.ErpFornecedorId, unidadeNegocioId, cancellationToken);

            // Simula (sem persistir) se, após este upsert (novo vínculo OU atualização de um existente), o
            // Fornecedor ficaria sem NENHUM vínculo ativo — mesma regra de "seria inativado" do caminho
            // real, sem gating pela fonte cadastral (uma linha que chega inativa nunca é fonte cadastral,
            // mas pode ainda assim inativar o Fornecedor).
            var todosVinculosDry = await vinculoRepository.ListarPorFornecedorAsync(local.Id, cancellationToken);
            var outrosAtivos = todosVinculosDry.Count(v => (vinculoExistenteDry is null || v.Id != vinculoExistenteDry.Id) && v.Ativo);
            var deveEstarAtivoDry = outrosAtivos > 0 || vinculoAtivoLinx;
            if (local.Status == "Ativo" && !deveEstarAtivoDry)
            {
                registrosParaInativar.Add((local, externo, alteradoEm));
                execucao.RegistrarAtualizado();
                return false;
            }

            if (vinculoExistenteDry is null)
            {
                execucao.RegistrarIncluido();
            }
            else if (VinculoDivergiu(vinculoExistenteDry, dados.NomeFantasia, inativoFornecedores, externo.InativoCadastroCliFor, externo.UltimaAlteracaoEm))
            {
                execucao.RegistrarAtualizado();
            }
            else
            {
                execucao.RegistrarSemAlteracao();
            }
            return false;
        }

        var vinculoExistente = await vinculoRepository.ObterPorErpSistemaECodigoAsync(externo.ErpSistema, externo.ErpFornecedorId, unidadeNegocioId, cancellationToken);
        FornecedorLinxVinculo vinculoAtual;
        if (vinculoExistente is null)
        {
            vinculoAtual = new FornecedorLinxVinculo(local.Id, unidadeNegocioId, externo.ErpSistema, externo.ErpFornecedorId, dados.NomeFantasia ?? string.Empty,
                inativoFornecedores, externo.InativoCadastroCliFor, externo.UltimaAlteracaoEm, principal: false, agora: alteradoEm);
            await vinculoRepository.AdicionarAsync(vinculoAtual, cancellationToken);
        }
        else
        {
            // Caso-limite defensivo (não coberto explicitamente pelo Product Owner, necessário para nunca
            // violar a invariante de unicidade de Principal ativo): um vínculo historicamente Principal
            // que estava inativo e volta a ficar Ativo nesta sincronização não pode reassumir a posição de
            // Principal ativo se OUTRO vínculo já é o Principal ativo atual — a escolha explícita do
            // comprador (ou uma atribuição automática anterior) nunca é substituída silenciosamente.
            if (vinculoExistente.Principal && !vinculoExistente.Ativo && vinculoAtivoLinx)
            {
                var siblingsAntes = await vinculoRepository.ListarPorFornecedorAsync(local.Id, cancellationToken);
                var outroPrincipalAtivo = siblingsAntes.Any(v => v.Id != vinculoExistente.Id && v.Principal && v.Ativo);
                if (outroPrincipalAtivo)
                {
                    vinculoExistente.RemoverComoPrincipal(alteradoEm);
                    ocorrenciasVinculos.Add($"CNPJ {documento}: vínculo {externo.ErpFornecedorId} reativado tinha Principal histórico, mas outro vínculo já é Principal ativo — Principal histórico removido para preservar a invariante.");
                }
            }
            vinculoExistente.AtualizarDadosErp(dados.NomeFantasia ?? string.Empty, inativoFornecedores, externo.InativoCadastroCliFor, externo.UltimaAlteracaoEm, alteradoEm);
            vinculoAtual = vinculoExistente;
        }

        var todosVinculos = await vinculoRepository.ListarPorFornecedorAsync(local.Id, cancellationToken);
        if (!todosVinculos.Any(v => v.Id == vinculoAtual.Id)) todosVinculos = [.. todosVinculos, vinculoAtual];

        // Caso B (Fornecedor sem NENHUM Principal definido, mesmo histórico/inativo) — nunca substitui um
        // Principal já existente, mesmo que ele esteja inativo (decisão do Product Owner, Bloco 5A.9/§5-6).
        var legadoAlterado = false;
        if (!todosVinculos.Any(v => v.Principal))
        {
            var ativos = todosVinculos.Where(v => v.Ativo).ToList();
            if (ativos.Count > 0)
            {
                var maiorData = ativos.Max(v => v.DataParaTransferencia);
                var candidatos = ativos.Where(v => v.DataParaTransferencia == maiorData).ToList();
                if (candidatos.Count == 1)
                {
                    candidatos[0].DefinirComoPrincipal(alteradoEm);
                    local.RegistrarVinculoErp(businessUnit, candidatos[0].ErpSistema, candidatos[0].CodigoErp);
                    legadoAlterado = true;
                }
                else
                {
                    // §7 — empate no maior DATA_PARA_TRANSFERENCIA entre vínculos ativos elegíveis: nunca
                    // inventar desempate (nunca CLIFOR, ordem de SELECT ou de sincronização). Fica sem
                    // Principal operacional até decisão do comprador; registrado como ocorrência.
                    ocorrenciasVinculos.Add($"CNPJ {documento}: empate na definição automática de Principal entre {candidatos.Count} vínculos ativos com DATA_PARA_TRANSFERENCIA={maiorData:O} — nenhum definido automaticamente.");
                }
            }
        }

        // Fonte cadastral do Fornecedor (campos descritivos — RazaoSocial/NomeFantasia/endereço/etc.):
        // sempre o vínculo ATIVO com maior DATA_PARA_TRANSFERENCIA — nunca o Principal (Principal e "mais
        // recente" são conceitos independentes, decisão do Product Owner). Situação cadastral (Ativo/
        // Inativo) é uma decisão SEPARADA: reflete se o Fornecedor ainda tem QUALQUER vínculo ativo — nunca
        // gated pela mesma condição da fonte cadastral, senão uma linha que chega inativa (portanto nunca
        // "fonte cadastral") nunca conseguiria inativar o Fornecedor.
        var ativosParaFonte = todosVinculos.Where(v => v.Ativo).ToList();
        var deveEstarAtivo = ativosParaFonte.Count > 0;
        var ehFonteCadastral = vinculoAtivoLinx && deveEstarAtivo
            && (vinculoAtual.DataParaTransferencia ?? DateTimeOffset.MinValue) >= ativosParaFonte.Max(v => v.DataParaTransferencia ?? DateTimeOffset.MinValue);
        var camposDescritivosDivergem = ehFonteCadastral && !EstaSemAlteracao(local, dados);
        var statusAtualEhAtivo = local.Status == "Ativo";
        var statusVaiMudar = statusAtualEhAtivo != deveEstarAtivo;
        var seriaInativado = statusAtualEhAtivo && !deveEstarAtivo;

        if (seriaInativado)
        {
            // Persistência do Fornecedor adiada até a guarda de inativação em massa (mesmo `local`,
            // já com o espelho legado atualizado em memória se aplicável) — o vínculo é salvo já. Nunca
            // aplica dados descritivos da linha que causou a inativação (ela nunca é fonte cadastral).
            await vinculoRepository.SalvarAlteracoesAsync(cancellationToken);
            registrosParaInativar.Add((local, externo, alteradoEm));
            return true;
        }

        var reativacaoSemFonteCadastral = statusVaiMudar && deveEstarAtivo && !camposDescritivosDivergem;
        if (camposDescritivosDivergem)
        {
            AplicarDadosCadastrais(local, externo, alteradoEm);
        }
        else if (reativacaoSemFonteCadastral)
        {
            local.AlterarStatus(true, alteradoEm, "ERP");
        }

        if (camposDescritivosDivergem || reativacaoSemFonteCadastral || legadoAlterado)
        {
            await repository.AtualizarAsync(local, cancellationToken);
            await vinculoRepository.SalvarAlteracoesAsync(cancellationToken);
            execucao.RegistrarAtualizado();
        }
        else
        {
            await vinculoRepository.SalvarAlteracoesAsync(cancellationToken);
            execucao.RegistrarSemAlteracao();
        }

        return false;
    }

    /// <summary>Aplica os dados canônicos do vínculo vencedor (fonte cadastral mais recente) ao Fornecedor
    /// e espelha a identidade ERP legada (<c>ErpFornecedorId</c>/<c>ErpSistema</c>) a partir do vínculo
    /// Principal ATIVO atual — nunca do vínculo vencedor de recência quando os dois divergem (o legado
    /// representa identidade operacional/Principal, não frescor cadastral).</summary>
    private void AplicarDadosCadastrais(Fornecedor local, FornecedorErpIntegracaoDto externo, DateTimeOffset alteradoEm) =>
        local.AplicarContratoCanonico(externo.Dados, "ERP", alteradoEm);

    private static bool VinculoDivergiu(FornecedorLinxVinculo existente, string? nomeClifor, bool inativoFornecedores, bool inativoCadastroCliFor, DateTimeOffset? dataParaTransferencia) =>
        existente.NomeClifor != (nomeClifor ?? string.Empty).Trim()
        || existente.InativoFornecedores != inativoFornecedores
        || existente.InativoCadastroCliFor != inativoCadastroCliFor
        || existente.DataParaTransferencia != dataParaTransferencia;

    private static string Identificar(FornecedorErpIntegracaoDto externo) =>
        string.Join(" | ", new[] { externo.ErpSistema, externo.ErpFornecedorId, externo.Dados.DocumentoFiscal }.Where(x => !string.IsNullOrWhiteSpace(x)));

    private static bool EstaSemAlteracao(Fornecedor local, FornecedorCanonico dados)
    {
        if (!string.IsNullOrWhiteSpace(local.HashDadosSincronizaveis) && local.HashDadosSincronizaveis == dados.HashDadosSincronizaveis) return true;
        return local.RazaoSocial == dados.RazaoSocial.Trim()
            && local.Cnpj_Cpf == DocumentoFiscal.Create(dados.DocumentoFiscal).Value
            && local.NomeFantasia == dados.NomeFantasia?.Trim().ToUpperInvariant()
            && local.TipoPessoa == dados.TipoPessoa?.Trim()
            && local.Cidade == dados.Cidade
            && local.Estado == dados.Uf
            && local.Status == (dados.Ativo ? "Ativo" : "Inativo");
    }
}
