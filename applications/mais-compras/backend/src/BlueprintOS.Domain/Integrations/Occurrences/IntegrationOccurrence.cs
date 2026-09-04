namespace BlueprintOS.Domain.Integrations.Occurrences;

/// <summary>Etapa do pipeline RAW→REFINED→DOMÍNIO em que a ocorrência foi detectada — genérico, não
/// específico de nenhum dataset.</summary>
public enum IntegrationStage
{
    Raw = 1,
    Refined = 2,
    Domain = 3,
    Reconciliation = 4,
}

/// <summary>Não é todo evento que é erro — decisão do Product Owner (B3): CNPJ/CPF inválido é rejeição real
/// (Error); um empate de Principal previsto pela própria regra homologada é Conflict, não Error; uma
/// divergência não material conhecida é Warning.</summary>
public enum IntegrationOccurrenceSeverity
{
    Error = 1,
    Warning = 2,
    Conflict = 3,
}

/// <summary>Estado operacional da ocorrência — suporte a evolução futura (workflow/UI de Administração),
/// não implementado nesta rodada. Toda ocorrência nasce Pendente.</summary>
public enum IntegrationOccurrenceStatus
{
    Pendente = 1,
    Resolvido = 2,
    IgnoradoAceito = 3,
}

/// <summary>
/// B3 — Bloco 5A.9, complemento à decisão "FULL LINX → RAW HOMOLOGADA" (2026-09-03): estrutura GENÉRICA
/// (não específica de Fornecedor) de ocorrências operacionais de uma integração — rejeições, conflitos e
/// avisos relevantes a nível de REGISTRO, nunca deixados apenas em log técnico/console. Deliberadamente
/// distinta da auditoria técnica (<c>GovernanceAuditEvent</c>/<c>RawLinxFornecedorSnapshotExecucao</c>, que
/// provam o que a execução FEZ) — esta tabela explica o que aconteceu com REGISTROS específicos, para
/// consulta e tratamento operacional posterior (futuro módulo Administração, não implementado agora).
///
/// Identidade/deduplicação (item de gate do PO): cada execução gera seu próprio conjunto de ocorrências —
/// nunca sobrescreve execuções anteriores (histórico entre execuções é preservado deliberadamente, não é
/// duplicidade). Dentro de UMA execução, cada registro de origem produz no máximo uma ocorrência por
/// combinação (Dataset, Stage, Code, OriginRecordKey) — reforçado por índice único, nunca só por convenção
/// de código.
/// </summary>
public sealed class IntegrationOccurrence
{
    public Guid Id { get; private set; }

    /// <summary>Onda 2 (Multi-BU/Multi-ERP, 03/09/2026, decisão do Product Owner): compõe a identidade/
    /// dedupe da ocorrência junto com <see cref="ExecutionId"/>/<see cref="Dataset"/>/<see cref="Stage"/>/
    /// <see cref="Code"/>/<see cref="OriginRecordKey"/> — duas Unidades de Negócio processando o mesmo
    /// dataset nunca compartilham nem colidem em ocorrências.</summary>
    public Guid UnidadeNegocioId { get; private set; }
    public Guid ExecutionId { get; private set; }
    public string Dataset { get; private set; } = string.Empty;
    public IntegrationStage Stage { get; private set; }
    public IntegrationOccurrenceSeverity Severity { get; private set; }

    /// <summary>Código legível por máquina (ex.: "CNPJ_CPF_INVALIDO", "PRINCIPAL_EMPATE") — nunca uma frase
    /// livre; a frase livre vive em <see cref="Message"/>.</summary>
    public string Code { get; private set; } = string.Empty;

    public string Message { get; private set; } = string.Empty;

    /// <summary>Identificação SEGURA do registro de origem — decisão do PO: nunca dado pessoal
    /// desnecessário. Para um documento fiscal inválido, é o código ERP (ex.: CodigoFornecedor), nunca o
    /// próprio CNPJ/CPF malformado. Para um conflito entre vínculos de um CNPJ já válido, o próprio CNPJ é
    /// aceitável (já é identidade corporativa usada em todo o domínio, não uma nova exposição).</summary>
    public string? OriginRecordKey { get; private set; }

    public DateTimeOffset OcorridoEm { get; private set; }
    public IntegrationOccurrenceStatus Status { get; private set; }

    /// <summary>Contexto técnico mínimo necessário para diagnóstico — nunca segredo, nunca dado pessoal além
    /// do estritamente necessário.</summary>
    public string? ContextoTecnico { get; private set; }

    private IntegrationOccurrence()
    {
    }

    public static IntegrationOccurrence Registrar(
        Guid executionId, Guid unidadeNegocioId, string dataset, IntegrationStage stage, IntegrationOccurrenceSeverity severity,
        string code, string message, string? originRecordKey, DateTimeOffset ocorridoEm, string? contextoTecnico = null)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Code é obrigatório.", nameof(code));
        if (string.IsNullOrWhiteSpace(message)) throw new ArgumentException("Message é obrigatório.", nameof(message));
        if (unidadeNegocioId == Guid.Empty) throw new ArgumentException("UnidadeNegocioId é obrigatória (Onda 2, Multi-BU) — nunca inferida, sempre da execução.", nameof(unidadeNegocioId));

        return new IntegrationOccurrence
        {
            Id = Guid.NewGuid(),
            UnidadeNegocioId = unidadeNegocioId,
            ExecutionId = executionId,
            Dataset = dataset,
            Stage = stage,
            Severity = severity,
            Code = code,
            Message = message,
            OriginRecordKey = originRecordKey,
            OcorridoEm = ocorridoEm,
            Status = IntegrationOccurrenceStatus.Pendente,
            ContextoTecnico = contextoTecnico,
        };
    }

    /// <summary>Uma execução POSTERIOR que processa corretamente o mesmo registro não altera esta linha
    /// retroativamente (histórico preservado) — a transição de status é sempre uma ação futura explícita
    /// (manual ou por um job de reconciliação de status, nenhum dos dois implementado nesta rodada).</summary>
    public void MarcarResolvido(DateTimeOffset quando) => Status = IntegrationOccurrenceStatus.Resolvido;

    public void MarcarIgnoradoAceito(DateTimeOffset quando) => Status = IntegrationOccurrenceStatus.IgnoradoAceito;
}
