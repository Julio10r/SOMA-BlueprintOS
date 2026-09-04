namespace BlueprintOS.Application.Identity;

public enum CadastroApoioRefinedAction
{
    Inativar,
    NoChange,
}

/// <summary>Forma genérica de uma linha RAW de cadastro de apoio (Conta Contábil, Unidade de Medida, Centro
/// de Custo) — cada dataset mapeia sua própria linha RAW tipada para esta forma antes de chamar o
/// projetor. <see cref="InativoErp"/> é <c>null</c> quando o dataset de origem simplesmente não tem conceito
/// de status no Linx (ex.: Unidades) — nesse caso o projetor nunca inativa por ausência de sinal.</summary>
public sealed record CadastroApoioRefinedItem(string CodigoErp, string? DescricaoErp, bool? InativoErp, DateTime? UltimaAlteracao, int Id = 0);

public sealed record CadastroApoioExistente(Guid Id, bool AtivoNoMaisCompras);

public sealed record CadastroApoioRefinedDecision(string CodigoErp, Guid MetadadoId, CadastroApoioRefinedAction Acao);

public sealed record CadastroApoioRefinedOcorrencia(string CodigoErp, string Code, string Mensagem);

/// <summary>
/// PO (revisão B3/Bloco 5A pós-certificação): <see cref="CodigosSemMetadadoLocal"/> nunca vira
/// <see cref="CadastroApoioRefinedOcorrencia"/>/<c>IntegrationOccurrence</c> — ausência de metadado local é
/// comportamento BY DESIGN (provisionamento é sempre sob demanda, nunca automático pelo pipeline), não uma
/// exceção a ser sinalizada. <see cref="Ocorrencias"/> é reservada para situações realmente excepcionais
/// (hoje: <c>CADASTRO_APOIO_CODIGO_LINX_AMBIGUO</c>).
/// </summary>
public sealed record CadastroApoioRefinedPlan(
    IReadOnlyList<CadastroApoioRefinedDecision> Decisoes,
    IReadOnlyList<string> CodigosSemMetadadoLocal,
    IReadOnlyList<CadastroApoioRefinedOcorrencia> Ocorrencias);

/// <summary>
/// B3 — Bloco 5A (preparação de certificação final): projetor PURO e determinístico, sem I/O, compartilhado
/// pelos cadastros de apoio estruturalmente idênticos (Conta Contábil, Unidade de Medida, Centro de Custo,
/// Filial). Aplica uma única regra de negócio, já documentada em <c>ICadastroApoioMetadado</c>: o Linx só
/// pode FORÇAR inativação local (ADR-0024), nunca reativar — reativar é decisão exclusiva do +Compras. Um
/// código visto no RAW sem metadado local correspondente nunca é criado aqui: provisionar exigiria uma
/// Unidade de Negócio, que não tem origem no Linx. Essa ausência é estado normal/lazy (PO, revisão B3/Bloco
/// 5A pós-certificação) — nunca uma ocorrência.
/// </summary>
public static class CadastroApoioRefinedProjector
{
    public static CadastroApoioRefinedPlan Projetar(
        IReadOnlyList<CadastroApoioRefinedItem> raw,
        IReadOnlyDictionary<string, CadastroApoioExistente> existentesPorCodigo)
    {
        var decisoes = new List<CadastroApoioRefinedDecision>();
        var codigosSemMetadadoLocal = new List<string>();
        var ocorrencias = new List<CadastroApoioRefinedOcorrencia>();

        // Achado real (B3/Bloco 5A, Discovery 03/09/2026 contra CTB_CENTRO_CUSTO): o Linx pode ter dois
        // códigos fisicamente distintos que só divergem em espaços em branco de formatação (ex.: um com
        // espaço à esquerda, outro com espaço à direita) — dado sujo real, não hipotético. Normalizar sem
        // detectar essa colisão faria dois registros de origem diferentes colidirem sob o mesmo código
        // aparado, violando o índice de deduplicação de ocorrências. Nunca escolhemos qual dos dois é o
        // "real": agrupamos primeiro e tratamos colisão como sua própria ocorrência.
        foreach (var grupo in raw.GroupBy(linha => linha.CodigoErp.Trim(), StringComparer.Ordinal))
        {
            var codigo = grupo.Key;

            // Achado real (Onda 2, auditoria RAW determinística, 04/09/2026): sob Incremental, RAW é
            // append-only (nunca trunca — só Full trunca), então o MESMO código Linx (idêntico, não apenas
            // "igual após trim") pode aparecer mais de uma vez — a linha antiga e a recém-anexada. Isso é
            // uma categoria DIFERENTE da ambiguidade de formatação acima (dois códigos Linx PRE-trim
            // DISTINTOS colidindo): aqui os valores pré-trim são IDÊNTICOS, então não há ambiguidade de
            // origem — é só a mesma linha vivendo duas vezes. Colapsa por valor EXATO (pré-trim) antes de
            // decidir se sobrou ambiguidade real: última versão por UltimaAlteracao, desempate por maior Id
            // (RAW só cresce sob Incremental, então Id mais alto é sempre a linha mais recente).
            var versoesPorCodigoExato = grupo
                .GroupBy(linha => linha.CodigoErp, StringComparer.Ordinal)
                .Select(g => g
                    .OrderByDescending(linha => linha.UltimaAlteracao ?? DateTime.MinValue)
                    .ThenByDescending(linha => linha.Id)
                    .First())
                .ToList();

            if (versoesPorCodigoExato.Count > 1)
            {
                ocorrencias.Add(new CadastroApoioRefinedOcorrencia(
                    codigo,
                    "CADASTRO_APOIO_CODIGO_LINX_AMBIGUO",
                    $"{versoesPorCodigoExato.Count} registros do Linx convergem para o mesmo código '{codigo}' após normalização de espaços — provável dado sujo de origem (formatação inconsistente). Nenhuma decisão foi tomada automaticamente; requer verificação manual de qual registro é o correto."));
                continue;
            }

            var linha = versoesPorCodigoExato[0];
            if (!existentesPorCodigo.TryGetValue(codigo, out var existente))
            {
                // PO (revisão B3/Bloco 5A pós-certificação): ausência de metadado local é BY DESIGN — nunca
                // gera IntegrationOccurrence. O código continua utilizável normalmente (ver Listar*UseCase de
                // cada cadastro); provisionamento permanece exclusivamente sob demanda pela tela/API.
                codigosSemMetadadoLocal.Add(codigo);
                continue;
            }

            if (linha.InativoErp == true && existente.AtivoNoMaisCompras)
            {
                decisoes.Add(new CadastroApoioRefinedDecision(codigo, existente.Id, CadastroApoioRefinedAction.Inativar));
            }
        }

        return new CadastroApoioRefinedPlan(decisoes, codigosSemMetadadoLocal, ocorrencias);
    }
}
