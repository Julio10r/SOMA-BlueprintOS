namespace BlueprintOS.Domain.Procurement.Suppliers;

public sealed class ErroSincronizacaoFornecedor
{
    private ErroSincronizacaoFornecedor() { }

    public ErroSincronizacaoFornecedor(Guid id, Guid sincronizacaoFornecedorId, string? fornecedorIdentificacao,
        string mensagem, string? stackTrace, DateTimeOffset dataHora)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        SincronizacaoFornecedorId = sincronizacaoFornecedorId;
        FornecedorIdentificacao = string.IsNullOrWhiteSpace(fornecedorIdentificacao) ? null : fornecedorIdentificacao.Trim();
        Mensagem = mensagem.Trim();
        StackTrace = string.IsNullOrWhiteSpace(stackTrace) ? null : stackTrace.Trim();
        DataHora = dataHora;
    }

    public Guid Id { get; private set; }
    public Guid SincronizacaoFornecedorId { get; private set; }
    public string? FornecedorIdentificacao { get; private set; }
    public string Mensagem { get; private set; } = null!;
    public string? StackTrace { get; private set; }
    public DateTimeOffset DataHora { get; private set; }
}
