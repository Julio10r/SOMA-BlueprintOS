namespace BlueprintOS.Domain.Procurement.Suppliers;

public sealed class FornecedorSincronizacao
{
    private FornecedorSincronizacao() { }

    public FornecedorSincronizacao(Guid id, string businessUnit, string erpSistema, string erpFornecedorId,
        Guid? fornecedorId, string direcao, string status, string correlationId, DateTimeOffset executadaEm, string? mensagemErro)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        BusinessUnit = businessUnit.Trim(); ErpSistema = erpSistema.Trim(); ErpFornecedorId = erpFornecedorId.Trim();
        FornecedorId = fornecedorId; Direcao = direcao.Trim(); Status = status.Trim();
        CorrelationId = correlationId.Trim(); ExecutadaEm = executadaEm; MensagemErro = mensagemErro?.Trim();
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
}
