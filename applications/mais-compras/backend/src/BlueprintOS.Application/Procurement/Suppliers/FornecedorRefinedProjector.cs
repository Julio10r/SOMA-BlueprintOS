using BlueprintOS.Application.Procurement.Suppliers.Models;
using BlueprintOS.Domain.Integrations.Occurrences;
using BlueprintOS.Domain.Procurement.Suppliers;
using BlueprintOS.Domain.Procurement.Suppliers.Raw;

namespace BlueprintOS.Application.Procurement.Suppliers;

/// <summary>
/// B3 — Bloco 5A.9, Gate REFINED (autorizado pelo Product Owner em 2026-09-03, "FULL LINX → RAW HOMOLOGADA
/// PELO PO"): computação pura, determinística, sem I/O, que projeta um lote de linhas RAW
/// (<see cref="RawLinxFornecedorSnapshotRegistro"/>) mais o estado atual do domínio em um plano de mudanças —
/// nunca aplica nada sozinho (isso é responsabilidade do orquestrador/DOMÍNIO). Reproduz, como operação
/// em lote agrupada por CNPJ/CPF, exatamente as mesmas regras já homologadas por
/// <c>SincronizarFornecedoresErpUseCase</c> (per-registro):
/// <list type="bullet">
/// <item>Ativo do vínculo: <c>!InativoFornecedores &amp;&amp; !InativoCadastroCliFor</c> (CADASTRO_CLI_FOR é
/// master, decisão do PO).</item>
/// <item>Fonte cadastral (LWW): sempre o vínculo ATIVO com maior <c>UltimaAlteracao</c> — nunca o Principal.
/// Sem vínculo ativo, os campos cadastrais permanecem inalterados (nunca zerados).</item>
/// <item>Principal: "Caso A" (Fornecedor novo) e "Caso B" (Fornecedor sem NENHUM Principal, mesmo histórico)
/// são o MESMO algoritmo generalizado aqui para N vínculos simultâneos — dispara sempre que, após aplicar
/// todas as mudanças do lote, nenhum vínculo do Fornecedor é Principal-e-Ativo: escolhe o vínculo Ativo com
/// maior <c>UltimaAlteracao</c>; em empate exato, NUNCA inventa desempate — registra o conflito e deixa sem
/// Principal operacional.</item>
/// <item>Principal nunca é trocado automaticamente só porque um vínculo mais recente apareceu, uma vez que já
/// existe.</item>
/// <item>Caso-limite defensivo de reativação: um vínculo historicamente Principal que volta a ficar Ativo
/// nunca reassume Principal se outro vínculo já é o Principal ativo atual.</item>
/// </list>
/// </summary>
public static class FornecedorRefinedProjector
{
    public const string ErpSistema = "SOMA_DESENV";

    public static RefinedPlan Projetar(
        IReadOnlyList<RawLinxFornecedorSnapshotRegistro> raw,
        IReadOnlyDictionary<string, FornecedorExistente> existentesPorCnpj,
        DateTimeOffset agora)
    {
        var erros = new List<RefinedOcorrencia>();
        var conflitos = new List<RefinedOcorrencia>();
        var decisoes = new List<FornecedorRefinedDecision>();

        var linhasValidas = new List<(RawLinxFornecedorSnapshotRegistro Row, string Cnpj)>();
        foreach (var row in raw)
        {
            try
            {
                var documento = DocumentoFiscal.Create(row.CnpjCpf ?? string.Empty);
                linhasValidas.Add((row, documento.Value));
            }
            catch (ArgumentException ex)
            {
                // OriginRecordKey é o código ERP (CodigoFornecedor), nunca o próprio CNPJ/CPF malformado —
                // decisão do PO: identificação segura do registro, nunca dado pessoal desnecessário na
                // ocorrência persistida.
                erros.Add(new RefinedOcorrencia(
                    IntegrationOccurrenceSeverity.Error, "CNPJ_CPF_INVALIDO",
                    $"CNPJ/CPF inválido — {ex.Message}", row.CodigoFornecedor.Trim()));
            }
        }

        foreach (var grupo in linhasValidas.GroupBy(x => x.Cnpj, x => x.Row))
        {
            var cnpj = grupo.Key;
            existentesPorCnpj.TryGetValue(cnpj, out var existente);
            var vinculosExistentesPorCodigo = existente?.Vinculos.ToDictionary(v => v.CodigoErp) ?? [];
            // COD_FORNECEDOR/CLIFOR são char(6) no Linx — vêm com espaço à direita quando o código real tem
            // menos de 6 caracteres (ex.: "2660" chega como "2660  "). FornecedorLinxVinculo já normaliza
            // (Trim()) ao persistir, então comparar o valor CRU do RAW contra o já persistido (trimado)
            // quebra a idempotência: um vínculo já existente seria classificado como "novo" e colidiria com
            // o índice único ao tentar inserir de novo. Normalizado aqui, na entrada, de uma vez por todas.
            var codigosNoRaw = grupo.Select(r => r.CodigoFornecedor.Trim()).ToHashSet();

            // Vínculos existentes cujo CodigoErp não apareceu nesta leitura RAW: preservados como estão —
            // esta execução nunca remove/inativa um vínculo por ausência no RAW (RAW pode estar
            // deliberadamente restrito a um subconjunto em cargas incrementais; só o valor explícito
            // INATIVO=1 vindo do Linx inativa um vínculo, nunca a ausência de linha).
            var vinculosPreservados = existente?.Vinculos.Where(v => !codigosNoRaw.Contains(v.CodigoErp)).ToList() ?? [];

            // Achado real (Onda 2, bateria final de certificação B3, 04/09/2026): sob Incremental, RAW é
            // append-only (nunca trunca — só Full trunca), então o mesmo CodigoFornecedor pode aparecer mais
            // de uma vez neste `grupo` (a linha antiga e a recém-anexada). Sem esta deduplicação, o `foreach`
            // abaixo processava CADA linha independentemente — a última processada "vencia" por mera ordem de
            // enumeração do banco (sem ORDER BY, não determinística), podendo aplicar o estado ANTIGO por
            // cima do novo. Reproduzido neste mesmo teste: 2 execuções idênticas do REFINED, sem nenhuma
          // mudança real no Linx entre elas, produziram contagens de vínculos ativos DIFERENTES. Desempate
            // por Id (maior primeiro) além de UltimaAlteracao porque as duas colunas de watermark são
            // independentes (COALESCE prioriza CADASTRO_CLI_FOR — ver watermark híbrido em
            // LinxReadDatasetCatalog): uma mudança isolada em FORNECEDORES não altera UltimaAlteracao,
            // gerando empate exato entre a linha antiga e a nova — Id mais alto é sempre a linha
            // efetivamente mais recente, pois RAW só cresce (nunca trunca) sob Incremental.
            var linhasDoGrupo = grupo
                .GroupBy(r => r.CodigoFornecedor.Trim())
                .Select(g => g
                    .OrderByDescending(r => ConverterParaDateTimeOffset(r.UltimaAlteracao) ?? DateTimeOffset.MinValue)
                    .ThenByDescending(r => r.Id)
                    .First())
                .ToList();

            var decisoesVinculo = new List<VinculoRefinedDecision>();
            var estadosFinaisParaPrincipal = new List<(string CodigoErp, bool Ativo, DateTimeOffset? UltimaAlteracao, bool PrincipalFinal)>();

            foreach (var v in vinculosPreservados)
            {
                estadosFinaisParaPrincipal.Add((v.CodigoErp, v.Ativo, v.DataParaTransferencia, v.Principal));
            }

            foreach (var row in linhasDoGrupo)
            {
                var codigoErp = row.CodigoFornecedor.Trim();
                var ativo = !row.InativoFornecedores && !row.InativoCadastroCliFor;
                var ultimaAlteracao = ConverterParaDateTimeOffset(row.UltimaAlteracao);
                var nomeClifor = row.NomeFantasia ?? string.Empty;

                if (vinculosExistentesPorCodigo.TryGetValue(codigoErp, out var vExistente))
                {
                    var mudou = vExistente.NomeClifor != nomeClifor
                        || vExistente.InativoFornecedores != row.InativoFornecedores
                        || vExistente.InativoCadastroCliFor != row.InativoCadastroCliFor
                        || vExistente.DataParaTransferencia != ultimaAlteracao;

                    // Caso-limite defensivo de reativação (nunca reassume Principal se outro vínculo já é o
                    // Principal ativo atual do Fornecedor).
                    var removerPrincipal = false;
                    if (vExistente.Principal && !vExistente.Ativo && ativo)
                    {
                        var outroPrincipalAtivo = existente!.Vinculos.Any(o => o.Id != vExistente.Id && o.Principal && o.Ativo);
                        if (outroPrincipalAtivo)
                        {
                            removerPrincipal = true;
                            conflitos.Add(new RefinedOcorrencia(
                                IntegrationOccurrenceSeverity.Conflict, "PRINCIPAL_REATIVACAO_COLISAO",
                                $"Vínculo {codigoErp} reativado tinha Principal histórico, mas outro vínculo já é Principal ativo — Principal histórico removido para preservar a invariante.",
                                cnpj));
                        }
                    }

                    decisoesVinculo.Add(new VinculoRefinedDecision(
                        codigoErp, mudou ? RefinedAction.Update : RefinedAction.NoChange, nomeClifor,
                        row.InativoFornecedores, row.InativoCadastroCliFor, ultimaAlteracao, ativo,
                        vExistente.Id, AtribuirPrincipal: false, RemoverPrincipal: removerPrincipal));

                    estadosFinaisParaPrincipal.Add((codigoErp, ativo, ultimaAlteracao, vExistente.Principal && !removerPrincipal));
                }
                else
                {
                    decisoesVinculo.Add(new VinculoRefinedDecision(
                        codigoErp, RefinedAction.Insert, nomeClifor,
                        row.InativoFornecedores, row.InativoCadastroCliFor, ultimaAlteracao, ativo,
                        VinculoExistenteId: null, AtribuirPrincipal: false, RemoverPrincipal: false));

                    estadosFinaisParaPrincipal.Add((codigoErp, ativo, ultimaAlteracao, false));
                }
            }

            // Caso A + Caso B unificados: dispara sempre que, após todas as mudanças deste lote, nenhum
            // vínculo do Fornecedor é Principal-e-Ativo — cobre tanto "Fornecedor nunca visto antes" (N=1 ou
            // mais vínculos novos de uma vez) quanto "Fornecedor existente sem Principal algum, mesmo
            // histórico" com o MESMO algoritmo, nunca inventando desempate.
            if (!estadosFinaisParaPrincipal.Any(e => e.PrincipalFinal && e.Ativo))
            {
                var elegiveis = estadosFinaisParaPrincipal.Where(e => e.Ativo).ToList();
                if (elegiveis.Count > 0)
                {
                    var maiorData = elegiveis.Max(e => e.UltimaAlteracao ?? DateTimeOffset.MinValue);
                    var candidatos = elegiveis.Where(e => (e.UltimaAlteracao ?? DateTimeOffset.MinValue) == maiorData).ToList();
                    if (candidatos.Count == 1)
                    {
                        var vencedor = candidatos[0].CodigoErp;
                        var idx = decisoesVinculo.FindIndex(d => d.CodigoErp == vencedor);
                        if (idx >= 0)
                        {
                            decisoesVinculo[idx] = decisoesVinculo[idx] with { AtribuirPrincipal = true };
                        }
                        // Se o vencedor está entre os vínculos PRESERVADOS (não presentes no RAW desta
                        // execução), não há decisão de vínculo para anotar — a atribuição de Principal a um
                        // vínculo fora do escopo desta leitura fica fora do plano desta execução, registrada
                        // como conflito para investigação humana em vez de mutação silenciosa fora do RAW.
                        else
                        {
                            conflitos.Add(new RefinedOcorrencia(
                                IntegrationOccurrenceSeverity.Conflict, "PRINCIPAL_FORA_DO_ESCOPO_RAW",
                                $"Vínculo elegível para Principal ({vencedor}) não está no RAW desta execução — Principal não atribuído automaticamente, requer nova leitura ou decisão do comprador.",
                                cnpj));
                        }
                    }
                    else
                    {
                        conflitos.Add(new RefinedOcorrencia(
                            IntegrationOccurrenceSeverity.Conflict, "PRINCIPAL_EMPATE",
                            $"Empate na definição automática de Principal entre {candidatos.Count} vínculos ativos com UltimaAlteracao={maiorData:O} — nenhum definido automaticamente (regra homologada: nunca inventar desempate).",
                            cnpj));
                    }
                }
            }

            // Fonte cadastral (LWW), independente de Principal: maior UltimaAlteracao entre os vínculos
            // ATIVOS (existentes preservados + linhas deste lote); desempate determinístico por CodigoErp
            // (a regra de "nunca inventar desempate" do PO é especificamente sobre Principal — item 5 — não
            // sobre a fonte cadastral, que sempre precisa de UM valor determinístico).
            var candidatosFonte = grupo
                .Select(r => (CodigoFornecedor: r.CodigoFornecedor.Trim(), Ativo: !r.InativoFornecedores && !r.InativoCadastroCliFor,
                    UltimaAlteracao: ConverterParaDateTimeOffset(r.UltimaAlteracao), r.RazaoSocial, r.NomeFantasia, r.TipoPessoa))
                .Where(c => c.Ativo)
                .OrderByDescending(c => c.UltimaAlteracao ?? DateTimeOffset.MinValue)
                .ThenBy(c => c.CodigoFornecedor, StringComparer.Ordinal)
                .ToList();

            var existeAtivoPreservado = vinculosPreservados.Any(v => v.Ativo);
            var algumAtivo = candidatosFonte.Count > 0 || existeAtivoPreservado;

            string razaoSocial;
            string? nomeFantasia;
            string tipoPessoa;
            if (candidatosFonte.Count > 0)
            {
                var fonte = candidatosFonte[0];
                razaoSocial = string.IsNullOrWhiteSpace(fonte.RazaoSocial) ? (existente?.RazaoSocial ?? fonte.CodigoFornecedor) : fonte.RazaoSocial!.Trim();
                nomeFantasia = fonte.NomeFantasia?.Trim();
                tipoPessoa = fonte.TipoPessoa?.Trim() ?? "PJ";
            }
            else if (existente is not null)
            {
                // Nenhuma linha ativa nesta leitura, mas o Fornecedor já existe e (possivelmente) já tem
                // vínculo ativo preservado fora do escopo do RAW — cadastro permanece exatamente como está.
                razaoSocial = existente.RazaoSocial;
                nomeFantasia = existente.NomeFantasia;
                tipoPessoa = existente.TipoPessoa ?? "PJ";
            }
            else
            {
                // Fornecedor totalmente novo e totalmente inativo em todas as linhas do lote: ainda assim
                // precisa de um nome válido para nascer — usa a linha de UltimaAlteracao mais recente entre
                // TODAS (ativas ou não), já que não há nenhuma fonte "ativa" disponível.
                var qualquerFonte = grupo
                    .OrderByDescending(r => ConverterParaDateTimeOffset(r.UltimaAlteracao) ?? DateTimeOffset.MinValue)
                    .ThenBy(r => r.CodigoFornecedor, StringComparer.Ordinal)
                    .First();
                razaoSocial = string.IsNullOrWhiteSpace(qualquerFonte.RazaoSocial) ? qualquerFonte.CodigoFornecedor.Trim() : qualquerFonte.RazaoSocial!.Trim();
                nomeFantasia = qualquerFonte.NomeFantasia?.Trim();
                tipoPessoa = qualquerFonte.TipoPessoa?.Trim() ?? "PJ";
            }

            var ativoAntes = existente is not null && string.Equals(existente.Status, "Ativo", StringComparison.OrdinalIgnoreCase);
            var action = existente is null ? RefinedAction.Insert
                : (decisoesVinculo.Any(d => d.Action != RefinedAction.NoChange) || algumAtivo != ativoAntes) ? RefinedAction.Update
                : RefinedAction.NoChange;

            decisoes.Add(new FornecedorRefinedDecision(
                cnpj, action, existente?.Id, razaoSocial, nomeFantasia, tipoPessoa, algumAtivo, ativoAntes, decisoesVinculo));
        }

        return new RefinedPlan(decisoes, conflitos, erros);
    }

    /// <summary>Mesma conversão de fuso horário já usada por <c>SomaFornecedorReader.ParseDate</c>: o valor
    /// SQL Server <c>datetime</c> chega como wall-clock de horário de Brasília, sem informação de fuso.</summary>
    private static DateTimeOffset? ConverterParaDateTimeOffset(DateTime? valor)
    {
        if (valor is null) return null;
        var local = DateTime.SpecifyKind(valor.Value, DateTimeKind.Unspecified);
        var zone = TimeZoneInfo.FindSystemTimeZoneById(OperatingSystem.IsWindows() ? "E. South America Standard Time" : "America/Sao_Paulo");
        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local, zone));
    }
}
