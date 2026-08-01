namespace BlueprintOS.Application.Procurement.Suppliers.Models;

public enum DirecaoSincronizacao { ErpParaMaisCompras, MaisComprasParaErp }
public enum OperacaoFornecedor { Sincronizar, Inativar }

public sealed record SincronizarFornecedorDto(string BusinessUnit, string ErpSistema, string? ErpFornecedorId,
    Guid? FornecedorId, DirecaoSincronizacao Direcao, string? CorrelationId, OperacaoFornecedor Operacao = OperacaoFornecedor.Sincronizar);
public sealed record SincronizacaoFornecedorResultado(Guid? FornecedorId, string BusinessUnit, string ErpSistema,
    string? ErpFornecedorId, string Status, string CorrelationId, DateTimeOffset ExecutadaEm, string? Mensagem);
public sealed record SincronizarFornecedoresLoteDto(string BusinessUnit, string ErpSistema, IReadOnlyList<Guid> FornecedorIds,
    int Limite, string? CorrelationId);
