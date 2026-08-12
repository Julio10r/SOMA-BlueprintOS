namespace BlueprintOS.Domain.Procurement.Suppliers;

public sealed class SincronizacaoFornecedor
{
    private readonly List<ErroSincronizacaoFornecedor> _erros = [];

    private SincronizacaoFornecedor() { }

    public SincronizacaoFornecedor(Guid id, string sistemaOrigem, string businessUnit, DateTimeOffset dataInicio, Guid unidadeNegocioId)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        SistemaOrigem = sistemaOrigem.Trim();
        BusinessUnit = businessUnit.Trim();
        DataInicio = dataInicio;
        Status = "Erro";
        UnidadeNegocioId = unidadeNegocioId;
    }

    public Guid Id { get; private set; }
    public string SistemaOrigem { get; private set; } = null!;
    public string BusinessUnit { get; private set; } = null!;

    /// <summary>DEB-03 (Gate Final da Onda 1) — Unidade de Negócio da identidade que disparou a
    /// execução, resolvida pelo backend (nunca pelo <see cref="BusinessUnit"/> de texto livre informado
    /// pelo chamador). Único campo confiável para escopar leitura por BU; ver
    /// <c>MonitoramentoOperacionalController</c>.</summary>
    public Guid UnidadeNegocioId { get; private set; }
    public DateTimeOffset DataInicio { get; private set; }
    public DateTimeOffset? DataFim { get; private set; }
    public string Status { get; private set; } = null!;
    public int TotalConsultado { get; private set; }
    public int TotalIncluido { get; private set; }
    public int TotalAtualizado { get; private set; }
    public int TotalSemAlteracao { get; private set; }
    public int TotalErro { get; private set; }
    public long TempoExecucaoMs { get; private set; }
    public IReadOnlyCollection<ErroSincronizacaoFornecedor> Erros => _erros;

    public void RegistrarConsultado() => TotalConsultado++;
    public void RegistrarIncluido() => TotalIncluido++;
    public void RegistrarAtualizado() => TotalAtualizado++;
    public void RegistrarSemAlteracao() => TotalSemAlteracao++;

    public void RegistrarErro(string? fornecedorIdentificacao, Exception exception, DateTimeOffset dataHora)
    {
        TotalErro++;
        _erros.Add(new ErroSincronizacaoFornecedor(Guid.NewGuid(), Id, fornecedorIdentificacao, Sanitizar(exception.Message, 1000),
            Sanitizar(exception.ToString(), 2000), dataHora));
    }

    public void Finalizar(DateTimeOffset dataFim)
    {
        DataFim = dataFim;
        TempoExecucaoMs = Math.Max(0, (long)(dataFim - DataInicio).TotalMilliseconds);
        Status = TotalErro == 0 ? "Sucesso" : TotalConsultado > TotalErro ? "Parcial" : "Erro";
    }

    private static string Sanitizar(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return "Erro nao informado.";
        var sanitized = value.ReplaceLineEndings(" ").Trim();
        return sanitized.Length <= maxLength ? sanitized : sanitized[..maxLength];
    }
}
