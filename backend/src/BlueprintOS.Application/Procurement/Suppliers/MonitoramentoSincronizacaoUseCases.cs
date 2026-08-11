using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using BlueprintOS.Application.Procurement.Suppliers.Models;
using BlueprintOS.Domain.Procurement.Suppliers;

namespace BlueprintOS.Application.Procurement.Suppliers;

/// <summary>O1.13 — Administração Operacional e Monitoramento. Leitura pura sobre a execução em lote de
/// sincronização de fornecedores já existente (B2.1.3). Nenhum motor novo de sincronização é criado aqui.</summary>
internal static class SincronizacaoFornecedorProjection
{
    public static SincronizacaoFornecedorResumoDto ProjetarResumo(SincronizacaoFornecedor execucao) => new(
        execucao.Id, execucao.SistemaOrigem, execucao.BusinessUnit, execucao.DataInicio, execucao.DataFim, execucao.Status,
        execucao.TotalConsultado, execucao.TotalIncluido, execucao.TotalAtualizado, execucao.TotalSemAlteracao,
        execucao.TotalErro, execucao.TempoExecucaoMs);

    public static SincronizacaoFornecedorDetalheDto ProjetarDetalhe(SincronizacaoFornecedor execucao) => new(
        execucao.Id, execucao.SistemaOrigem, execucao.BusinessUnit, execucao.DataInicio, execucao.DataFim, execucao.Status,
        execucao.TotalConsultado, execucao.TotalIncluido, execucao.TotalAtualizado, execucao.TotalSemAlteracao,
        execucao.TotalErro, execucao.TempoExecucaoMs,
        execucao.Erros.OrderBy(e => e.DataHora)
            .Select(e => new ErroSincronizacaoFornecedorDto(e.Id, e.FornecedorIdentificacao, e.Mensagem, e.DataHora))
            .ToArray());
}

public sealed class ListarSincronizacoesFornecedoresUseCase(ISincronizacaoFornecedorMonitorRepository repositorio)
    : IListarSincronizacoesFornecedoresUseCase
{
    public async Task<ListarSincronizacoesFornecedoresResultado> ExecuteAsync(
        ListarSincronizacoesFornecedoresFiltro filtro, CancellationToken ct)
    {
        var pagina = filtro.Pagina <= 0 ? 1 : filtro.Pagina;
        var tamanhoPagina = filtro.TamanhoPagina <= 0 ? 20 : Math.Min(filtro.TamanhoPagina, 200);
        var filtroNormalizado = filtro with { Pagina = pagina, TamanhoPagina = tamanhoPagina };

        var (itens, total) = await repositorio.ListarAsync(filtroNormalizado, ct);
        return new ListarSincronizacoesFornecedoresResultado(
            itens.Select(SincronizacaoFornecedorProjection.ProjetarResumo).ToArray(), total, pagina, tamanhoPagina);
    }
}

public sealed class ObterSincronizacaoFornecedorUseCase(ISincronizacaoFornecedorMonitorRepository repositorio)
    : IObterSincronizacaoFornecedorUseCase
{
    public async Task<SincronizacaoFornecedorDetalheDto?> ExecuteAsync(Guid id, CancellationToken ct)
    {
        var execucao = await repositorio.ObterPorIdComErrosAsync(id, ct);
        return execucao is null ? null : SincronizacaoFornecedorProjection.ProjetarDetalhe(execucao);
    }
}
