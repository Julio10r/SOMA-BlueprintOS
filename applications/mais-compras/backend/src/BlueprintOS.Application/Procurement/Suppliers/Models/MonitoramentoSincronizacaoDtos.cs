namespace BlueprintOS.Application.Procurement.Suppliers.Models;

/// <summary>O1.13 — Filtros de listagem paginada das execuções em lote de <c>SincronizacaoFornecedor</c>.
/// Reaproveita 100% a infraestrutura de sincronização de B2.1.3 — apenas leitura, nenhum motor novo.</summary>
public sealed record ListarSincronizacoesFornecedoresFiltro(string? Status, string? BusinessUnit, int Pagina, int TamanhoPagina);

public sealed record ErroSincronizacaoFornecedorDto(Guid Id, string? FornecedorIdentificacao, string Mensagem, DateTimeOffset DataHora);

public sealed record SincronizacaoFornecedorResumoDto(
    Guid Id, string SistemaOrigem, string BusinessUnit, DateTimeOffset DataInicio, DateTimeOffset? DataFim, string Status,
    int TotalConsultado, int TotalIncluido, int TotalAtualizado, int TotalSemAlteracao, int TotalErro, long TempoExecucaoMs);

public sealed record SincronizacaoFornecedorDetalheDto(
    Guid Id, string SistemaOrigem, string BusinessUnit, DateTimeOffset DataInicio, DateTimeOffset? DataFim, string Status,
    int TotalConsultado, int TotalIncluido, int TotalAtualizado, int TotalSemAlteracao, int TotalErro, long TempoExecucaoMs,
    IReadOnlyList<ErroSincronizacaoFornecedorDto> Erros);

public sealed record ListarSincronizacoesFornecedoresResultado(
    IReadOnlyList<SincronizacaoFornecedorResumoDto> Itens, int TotalRegistros, int Pagina, int TamanhoPagina);
