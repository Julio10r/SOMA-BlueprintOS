namespace BlueprintOS.Domain.Procurement.Suppliers;

public sealed class FornecedorCnpjConsultaHistorico
{
    private FornecedorCnpjConsultaHistorico() { }

    public FornecedorCnpjConsultaHistorico(Guid id, string cnpjCpf, string fonteConsulta, DateTimeOffset dataConsulta,
        Guid usuario, string status, string resultado, string? mensagemErro, string correlationId,
        string businessUnit, string? erpSistema)
    {
        if (string.IsNullOrWhiteSpace(cnpjCpf)) throw new ArgumentException("Cnpj_Cpf is required.", nameof(cnpjCpf));
        if (string.IsNullOrWhiteSpace(fonteConsulta)) throw new ArgumentException("FonteConsulta is required.", nameof(fonteConsulta));
        if (usuario == Guid.Empty) throw new ArgumentException("Usuario is required.", nameof(usuario));
        if (string.IsNullOrWhiteSpace(status)) throw new ArgumentException("Status is required.", nameof(status));
        if (string.IsNullOrWhiteSpace(resultado)) throw new ArgumentException("Resultado is required.", nameof(resultado));
        if (string.IsNullOrWhiteSpace(correlationId)) throw new ArgumentException("CorrelationId is required.", nameof(correlationId));
        if (string.IsNullOrWhiteSpace(businessUnit)) throw new ArgumentException("BusinessUnit is required.", nameof(businessUnit));

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        Cnpj_Cpf = cnpjCpf.Trim(); FonteConsulta = fonteConsulta.Trim(); DataConsulta = dataConsulta;
        Usuario = usuario; Status = status.Trim(); Resultado = resultado.Trim(); MensagemErro = mensagemErro?.Trim();
        CorrelationId = correlationId.Trim(); BusinessUnit = businessUnit.Trim(); ErpSistema = erpSistema?.Trim();
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
}
