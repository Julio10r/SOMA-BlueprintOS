namespace BlueprintOS.Domain.Procurement.Suppliers;

/// <summary>Registro estrutural (permanente) e snapshot bruto opcional (retenção de
/// <see cref="RetencaoPayloadBrutoDias"/> dias) de uma consulta de CNPJ/CPF ao Provider externo.
/// Proveniência híbrida (ADR-0023, seção L): a parte estrutural (CNPJ, Fonte, Timestamp, Status,
/// TipoErro, CorrelationId) nunca é removida por este mecanismo — apenas <see cref="PayloadBrutoJson"/>
/// expira e é anulado após a retenção. O snapshot bruto NUNCA é fonte de leitura do domínio: existe
/// exclusivamente como evidência de auditoria/proveniência do que o Provider respondeu naquele momento.</summary>
public sealed class FornecedorCnpjConsultaHistorico
{
    /// <summary>Retenção do payload bruto em dias (ADR-0023). Após este período a partir de
    /// <see cref="DataConsulta"/>, o snapshot pode/deve ser anulado por <see cref="ExpirarPayloadBruto"/> —
    /// a trilha estrutural nunca é afetada.</summary>
    public const int RetencaoPayloadBrutoDias = 180;

    /// <summary>Limite de tamanho (em caracteres) aceito para o snapshot bruto sanitizado. Acima
    /// deste limite o snapshot é descartado por completo (nunca truncado de forma que produza JSON
    /// inválido) e <see cref="PayloadBrutoDescartadoPorTamanho"/> é marcado como evidência de que
    /// havia uma resposta do Provider, mas ela excedeu o limite operacional de armazenamento.</summary>
    public const int LimitePayloadBrutoCaracteres = 32_000;

    private FornecedorCnpjConsultaHistorico() { }

    public FornecedorCnpjConsultaHistorico(Guid id, string cnpjCpf, string fonteConsulta, DateTimeOffset dataConsulta,
        Guid usuario, string status, string resultado, string? mensagemErro, string correlationId,
        string businessUnit, string? erpSistema, TipoErroConsultaCnpjHistorico? tipoErro = null,
        string? payloadBrutoJson = null, bool payloadBrutoDescartadoPorTamanho = false)
    {
        if (string.IsNullOrWhiteSpace(cnpjCpf)) throw new ArgumentException("Cnpj_Cpf is required.", nameof(cnpjCpf));
        if (string.IsNullOrWhiteSpace(fonteConsulta)) throw new ArgumentException("FonteConsulta is required.", nameof(fonteConsulta));
        if (usuario == Guid.Empty) throw new ArgumentException("Usuario is required.", nameof(usuario));
        if (string.IsNullOrWhiteSpace(status)) throw new ArgumentException("Status is required.", nameof(status));
        if (string.IsNullOrWhiteSpace(resultado)) throw new ArgumentException("Resultado is required.", nameof(resultado));
        if (string.IsNullOrWhiteSpace(correlationId)) throw new ArgumentException("CorrelationId is required.", nameof(correlationId));
        if (string.IsNullOrWhiteSpace(businessUnit)) throw new ArgumentException("BusinessUnit is required.", nameof(businessUnit));
        if (payloadBrutoJson is { Length: > LimitePayloadBrutoCaracteres })
            throw new ArgumentException($"PayloadBrutoJson exceeds the {LimitePayloadBrutoCaracteres}-character limit; it must be discarded (null) before reaching the entity, not truncated here.", nameof(payloadBrutoJson));

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        Cnpj_Cpf = cnpjCpf.Trim(); FonteConsulta = fonteConsulta.Trim(); DataConsulta = dataConsulta;
        Usuario = usuario; Status = status.Trim(); Resultado = resultado.Trim(); MensagemErro = mensagemErro?.Trim();
        CorrelationId = correlationId.Trim(); BusinessUnit = businessUnit.Trim(); ErpSistema = erpSistema?.Trim();
        TipoErro = tipoErro;
        PayloadBrutoJson = string.IsNullOrWhiteSpace(payloadBrutoJson) ? null : payloadBrutoJson;
        PayloadBrutoDescartadoPorTamanho = payloadBrutoDescartadoPorTamanho;
    }

    public Guid Id { get; private set; }
    public string Cnpj_Cpf { get; private set; } = null!;
    public string FonteConsulta { get; private set; } = null!;
    public DateTimeOffset DataConsulta { get; private set; }
    public Guid Usuario { get; private set; }
    public string Status { get; private set; } = null!;
    public string Resultado { get; private set; } = null!;
    public string? MensagemErro { get; private set; }
    public string CorrelationId { get; private set; } = null!;
    public string BusinessUnit { get; private set; } = null!;
    public string? ErpSistema { get; private set; }

    /// <summary>Classificação canônica do erro (taxonomia da ADR-0023/B2.4), persistida como
    /// complemento estrutural de <see cref="Status"/>/<see cref="MensagemErro"/> — permite saber
    /// que tipo de falha ocorreu sem parsear texto livre. Nulo em consultas bem-sucedidas.</summary>
    public TipoErroConsultaCnpjHistorico? TipoErro { get; private set; }

    /// <summary>Snapshot bruto/sanitizado (sem QSA, sem segredos) da resposta do Provider no
    /// momento da consulta. Opaco por design — nunca é modelo de domínio nem fonte de leitura
    /// operacional. Nulo quando não houve corpo de resposta útil (ex.: timeout) ou após expurgo
    /// por retenção (<see cref="ExpirarPayloadBruto"/>).</summary>
    public string? PayloadBrutoJson { get; private set; }

    /// <summary>Marca que, no momento da consulta, o Provider retornou um corpo cujo snapshot
    /// sanitizado excedia <see cref="LimitePayloadBrutoCaracteres"/> e por isso foi descartado
    /// (nunca truncado de forma insegura). Sobrevive ao expurgo por retenção — é metadado
    /// estrutural, não parte do payload.</summary>
    public bool PayloadBrutoDescartadoPorTamanho { get; private set; }

    /// <summary>Verdadeiro quando, na data de referência informada, o payload bruto já passou da
    /// retenção de <see cref="RetencaoPayloadBrutoDias"/> dias a partir de <see cref="DataConsulta"/>.
    /// Semântica de fronteira exata (ADR-0023, addendo B2.7): o payload é elegível para expurgo
    /// somente quando já se passaram MAIS de 180 dias — ou seja, <c>DataConsulta</c> estritamente
    /// anterior ao corte <c>referencia.AddDays(-180)</c>. No dia exato do 180º aniversário da
    /// consulta o payload ainda é preservado; a partir do 181º dia ele é elegível.</summary>
    public bool PayloadBrutoExpirado(DateTimeOffset referenciaUtc) =>
        DataConsulta < referenciaUtc.AddDays(-RetencaoPayloadBrutoDias);

    /// <summary>Anula o payload bruto por expurgo de retenção. Idempotente: chamar novamente sobre
    /// um registro já expurgado (payload já nulo) não lança e não altera nenhum outro campo
    /// estrutural (TipoErro/Fonte/Status/DataConsulta/etc. permanecem intocados).</summary>
    public void ExpirarPayloadBruto()
    {
        PayloadBrutoJson = null;
    }
}

/// <summary>Espelha <c>BlueprintOS.Application.Procurement.Suppliers.Models.TipoErroConsultaCnpj</c>
/// dentro do Domain (que não depende do Application layer). Persistido como string (mesma convenção
/// já usada por <see cref="FornecedorCnpjConsultaHistorico.Status"/>) via mapeamento 1:1 de nome no
/// Application/Infrastructure layer — nunca reintroduz um valor sobrecarregado para representar
/// "sem erro" (permanece nulo em sucesso).</summary>
public enum TipoErroConsultaCnpjHistorico
{
    CnpjInvalido,
    NaoEncontrado,
    FonteIndisponivel,
    Timeout,
    LimiteDeConsultas,
    ErroDeAutenticacaoDoProvider,
    RespostaInvalida,
    ErroInterno
}
