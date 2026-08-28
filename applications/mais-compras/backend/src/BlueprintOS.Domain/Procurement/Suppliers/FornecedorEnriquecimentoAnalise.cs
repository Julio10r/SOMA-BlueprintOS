namespace BlueprintOS.Domain.Procurement.Suppliers;

public sealed class FornecedorEnriquecimentoAnalise
{
    private FornecedorEnriquecimentoAnalise() { }

    public FornecedorEnriquecimentoAnalise(Guid id, Guid fornecedorId, string cnpjCpf, Guid? consultaId, string campo,
        string? valorAnterior, string? valorNovo, string decisao, Guid usuario, DateTimeOffset dataHora,
        string correlationId, string businessUnit, string? erpSistema, string fonte)
    {
        if (fornecedorId == Guid.Empty) throw new ArgumentException("FornecedorId is required.", nameof(fornecedorId));
        if (string.IsNullOrWhiteSpace(cnpjCpf)) throw new ArgumentException("Cnpj_Cpf is required.", nameof(cnpjCpf));
        if (string.IsNullOrWhiteSpace(campo)) throw new ArgumentException("Campo is required.", nameof(campo));
        if (string.IsNullOrWhiteSpace(decisao)) throw new ArgumentException("Decisao is required.", nameof(decisao));
        if (usuario == Guid.Empty) throw new ArgumentException("Usuario is required.", nameof(usuario));
        if (string.IsNullOrWhiteSpace(correlationId)) throw new ArgumentException("CorrelationId is required.", nameof(correlationId));
        if (string.IsNullOrWhiteSpace(businessUnit)) throw new ArgumentException("BusinessUnit is required.", nameof(businessUnit));
        if (string.IsNullOrWhiteSpace(fonte)) throw new ArgumentException("Fonte is required.", nameof(fonte));

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        FornecedorId = fornecedorId; Cnpj_Cpf = cnpjCpf.Trim(); ConsultaId = consultaId; Campo = campo.Trim();
        ValorAnterior = valorAnterior?.Trim(); ValorNovo = valorNovo?.Trim(); Decisao = decisao.Trim();
        Usuario = usuario; DataHora = dataHora; CorrelationId = correlationId.Trim(); BusinessUnit = businessUnit.Trim();
        ErpSistema = erpSistema?.Trim(); Fonte = fonte.Trim();
    }

    public Guid Id { get; private set; }
    public Guid FornecedorId { get; private set; }
    public string Cnpj_Cpf { get; private set; } = null!;
    public Guid? ConsultaId { get; private set; }
    public string Campo { get; private set; } = null!;
    public string? ValorAnterior { get; private set; }
    public string? ValorNovo { get; private set; }
    public string Decisao { get; private set; } = null!;
    public Guid Usuario { get; private set; }
    public DateTimeOffset DataHora { get; private set; }
    public string CorrelationId { get; private set; } = null!;
    public string BusinessUnit { get; private set; } = null!;
    public string? ErpSistema { get; private set; }
    public string Fonte { get; private set; } = null!;
}
