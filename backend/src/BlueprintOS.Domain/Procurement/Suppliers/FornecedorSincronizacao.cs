namespace BlueprintOS.Domain.Procurement.Suppliers;

public sealed class FornecedorSincronizacao
{
    private FornecedorSincronizacao() { }

    public FornecedorSincronizacao(Guid id, string businessUnit, string erpSistema, string erpFornecedorId,
        Guid? fornecedorId, string direcao, string status, string correlationId, DateTimeOffset executadaEm, string? mensagemErro)
        : this(id, businessUnit, erpSistema, erpFornecedorId, fornecedorId, direcao, status, correlationId, executadaEm, mensagemErro,
            null, null, null, null, null, null, "Alterado", null, null, null, null, null, 1, 0)
    {
    }

    public FornecedorSincronizacao(Guid id, string businessUnit, string erpSistema, string erpFornecedorId,
        Guid? fornecedorId, string direcao, string status, string correlationId, DateTimeOffset executadaEm, string? mensagemErro,
        string? origem, string? destino, string? timestampComprasOriginal, string? timestampErpOriginal,
        string? timestampComprasNormalizado, string? timestampErpNormalizado, string decisao, string? camposAlterados,
        string? dadosAntes, string? dadosDepois, string? hashAntes, string? hashDepois, int tentativa, int duracaoMs)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        BusinessUnit = businessUnit.Trim(); ErpSistema = erpSistema.Trim(); ErpFornecedorId = erpFornecedorId.Trim();
        FornecedorId = fornecedorId; Direcao = direcao.Trim(); Status = status.Trim();
        CorrelationId = correlationId.Trim(); ExecutadaEm = executadaEm; MensagemErro = mensagemErro?.Trim();
        Origem = origem?.Trim(); Destino = destino?.Trim(); TimestampComprasOriginal = timestampComprasOriginal;
        TimestampErpOriginal = timestampErpOriginal; TimestampComprasNormalizado = timestampComprasNormalizado;
        TimestampErpNormalizado = timestampErpNormalizado; Decisao = decisao.Trim(); CamposAlterados = camposAlterados;
        DadosAntes = dadosAntes; DadosDepois = dadosDepois; HashAntes = hashAntes; HashDepois = hashDepois;
        Tentativa = tentativa; DuracaoMs = duracaoMs;
    }

    public Guid Id { get; private set; }
    public string BusinessUnit { get; private set; } = null!;
    public string ErpSistema { get; private set; } = null!;
    public string ErpFornecedorId { get; private set; } = null!;
    public Guid? FornecedorId { get; private set; }
    public string Direcao { get; private set; } = null!;
    public string Status { get; private set; } = null!;
    public string CorrelationId { get; private set; } = null!;
    public DateTimeOffset ExecutadaEm { get; private set; }
    public string? MensagemErro { get; private set; }
    public string? Origem { get; private set; }
    public string? Destino { get; private set; }
    public string? TimestampComprasOriginal { get; private set; }
    public string? TimestampErpOriginal { get; private set; }
    public string? TimestampComprasNormalizado { get; private set; }
    public string? TimestampErpNormalizado { get; private set; }
    public string Decisao { get; private set; } = "Alterado";
    public string? CamposAlterados { get; private set; }
    public string? DadosAntes { get; private set; }
    public string? DadosDepois { get; private set; }
    public string? HashAntes { get; private set; }
    public string? HashDepois { get; private set; }
    public int Tentativa { get; private set; }
    public int DuracaoMs { get; private set; }
}
