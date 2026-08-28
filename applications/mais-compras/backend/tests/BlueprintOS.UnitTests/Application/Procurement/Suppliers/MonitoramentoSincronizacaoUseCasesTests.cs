using BlueprintOS.Application.Procurement.Suppliers;
using BlueprintOS.Application.Procurement.Suppliers.Models;
using BlueprintOS.Domain.Procurement.Suppliers;
using BlueprintOS.Infrastructure.Persistence;
using BlueprintOS.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BlueprintOS.UnitTests.Application.Procurement.Suppliers;

/// <summary>O1.13 — Leitura pura sobre as execuções em lote de sincronização de fornecedores já
/// existentes (B2.1.3). Testes usam o mesmo padrão InMemory dos demais testes de
/// <c>Procurement.Suppliers</c> (ver <see cref="SincronizarFornecedoresErpUseCaseTests"/>).
///
/// DEB-03 (Gate Final da Onda 1, 11/08/2026) — testes de regressão de isolamento multi-BU: a Unidade de
/// Negócio da sessão que chama <c>ListarUseCase</c>/<c>ObterUseCase</c> nunca pode ver ou ler por Id uma
/// execução pertencente a outra Unidade de Negócio, mesmo conhecendo o Id exato (IDOR).</summary>
public sealed class MonitoramentoSincronizacaoUseCasesTests
{
    private static readonly Guid UnidadeA = Guid.NewGuid();
    private static readonly Guid UnidadeB = Guid.NewGuid();

    [Fact]
    public async Task Listar_Should_Return_Empty_When_No_Execucoes()
    {
        await using var context = NewContext();
        var resultado = await ListarUseCase(context).ExecuteAsync(UnidadeA, new(null, null, 1, 20), CancellationToken.None);

        Assert.Empty(resultado.Itens);
        Assert.Equal(0, resultado.TotalRegistros);
    }

    [Fact]
    public async Task Listar_Should_Order_By_DataInicio_Desc_And_Paginate()
    {
        await using var context = NewContext();
        var inicio = DateTimeOffset.UtcNow;
        await AdicionarExecucaoAsync(context, UnidadeA, "BU-A", "Sucesso", inicio.AddMinutes(-10));
        await AdicionarExecucaoAsync(context, UnidadeA, "BU-A", "Sucesso", inicio.AddMinutes(-5));
        await AdicionarExecucaoAsync(context, UnidadeA, "BU-A", "Sucesso", inicio);

        var resultado = await ListarUseCase(context).ExecuteAsync(UnidadeA, new(null, null, 1, 2), CancellationToken.None);

        Assert.Equal(3, resultado.TotalRegistros);
        Assert.Equal(2, resultado.Itens.Count);
        Assert.Equal(inicio, resultado.Itens[0].DataInicio);
    }

    [Fact]
    public async Task Listar_Should_Filter_By_Status_And_BusinessUnit()
    {
        await using var context = NewContext();
        var inicio = DateTimeOffset.UtcNow;
        await AdicionarExecucaoAsync(context, UnidadeA, "BU-A", "Sucesso", inicio);
        await AdicionarExecucaoAsync(context, UnidadeA, "BU-A", "Erro", inicio.AddMinutes(-1));
        await AdicionarExecucaoAsync(context, UnidadeA, "BU-B", "Sucesso", inicio.AddMinutes(-2));

        var porStatus = await ListarUseCase(context).ExecuteAsync(UnidadeA, new("Erro", null, 1, 20), CancellationToken.None);
        Assert.Single(porStatus.Itens);
        Assert.Equal("Erro", porStatus.Itens[0].Status);

        var porBu = await ListarUseCase(context).ExecuteAsync(UnidadeA, new(null, "BU-B", 1, 20), CancellationToken.None);
        Assert.Single(porBu.Itens);
        Assert.Equal("BU-B", porBu.Itens[0].BusinessUnit);
    }

    [Fact]
    public async Task Obter_Should_Return_Detail_With_Erros_When_Found()
    {
        await using var context = NewContext();
        var execucao = new SincronizacaoFornecedor(Guid.NewGuid(), "SOMA_DESENV", "BU-A", DateTimeOffset.UtcNow.AddMinutes(-1), UnidadeA);
        execucao.RegistrarConsultado();
        execucao.RegistrarErro("12345678000195", new InvalidOperationException("falha simulada"), DateTimeOffset.UtcNow);
        execucao.Finalizar(DateTimeOffset.UtcNow);
        await context.SincronizacoesFornecedores.AddAsync(execucao);
        await context.SaveChangesAsync();

        var detalhe = await ObterUseCase(context).ExecuteAsync(UnidadeA, execucao.Id, CancellationToken.None);

        Assert.NotNull(detalhe);
        Assert.Equal("Erro", detalhe!.Status);
        Assert.Single(detalhe.Erros);
        Assert.Equal("12345678000195", detalhe.Erros[0].FornecedorIdentificacao);
        Assert.Equal("falha simulada", detalhe.Erros[0].Mensagem);
    }

    [Fact]
    public async Task Obter_Should_Return_Null_When_Not_Found()
    {
        await using var context = NewContext();
        var detalhe = await ObterUseCase(context).ExecuteAsync(UnidadeA, Guid.NewGuid(), CancellationToken.None);
        Assert.Null(detalhe);
    }

    [Fact]
    public async Task Listar_Should_Not_Expose_Execucoes_De_Outra_UnidadeNegocio()
    {
        await using var context = NewContext();
        var inicio = DateTimeOffset.UtcNow;
        await AdicionarExecucaoAsync(context, UnidadeA, "BU-A", "Sucesso", inicio);
        await AdicionarExecucaoAsync(context, UnidadeB, "BU-B", "Sucesso", inicio.AddMinutes(-1));

        var resultadoA = await ListarUseCase(context).ExecuteAsync(UnidadeA, new(null, null, 1, 20), CancellationToken.None);
        Assert.Single(resultadoA.Itens);

        var resultadoB = await ListarUseCase(context).ExecuteAsync(UnidadeB, new(null, null, 1, 20), CancellationToken.None);
        Assert.Single(resultadoB.Itens);
        Assert.NotEqual(resultadoA.Itens[0].Id, resultadoB.Itens[0].Id);
    }

    [Fact]
    public async Task Obter_Should_Return_Null_When_Execucao_Pertence_A_Outra_UnidadeNegocio()
    {
        // DEB-03: BU A conhece o Id exato de uma execução da BU B (ex.: por enumeração/vazamento em log)
        // e tenta lê-la diretamente — deve ser tratado como inexistente, nunca retornado.
        await using var context = NewContext();
        var execucaoDaBuB = new SincronizacaoFornecedor(Guid.NewGuid(), "SOMA_DESENV", "BU-B", DateTimeOffset.UtcNow, UnidadeB);
        execucaoDaBuB.Finalizar(DateTimeOffset.UtcNow);
        await context.SincronizacoesFornecedores.AddAsync(execucaoDaBuB);
        await context.SaveChangesAsync();

        var detalheComoUnidadeA = await ObterUseCase(context).ExecuteAsync(UnidadeA, execucaoDaBuB.Id, CancellationToken.None);
        Assert.Null(detalheComoUnidadeA);

        var detalheComoUnidadeB = await ObterUseCase(context).ExecuteAsync(UnidadeB, execucaoDaBuB.Id, CancellationToken.None);
        Assert.NotNull(detalheComoUnidadeB);
    }

    private static async Task AdicionarExecucaoAsync(
        BlueprintOSDbContext context, Guid unidadeNegocioId, string businessUnit, string statusEsperado, DateTimeOffset dataInicio)
    {
        var execucao = new SincronizacaoFornecedor(Guid.NewGuid(), "SOMA_DESENV", businessUnit, dataInicio, unidadeNegocioId);
        execucao.RegistrarConsultado();
        if (statusEsperado == "Erro")
        {
            execucao.RegistrarErro("ident", new InvalidOperationException("erro"), dataInicio);
        }
        else
        {
            execucao.RegistrarIncluido();
        }
        execucao.Finalizar(dataInicio.AddSeconds(1));
        await context.SincronizacoesFornecedores.AddAsync(execucao);
        await context.SaveChangesAsync();
    }

    private static ListarSincronizacoesFornecedoresUseCase ListarUseCase(BlueprintOSDbContext context) =>
        new(new SincronizacaoFornecedorMonitorRepository(context));

    private static ObterSincronizacaoFornecedorUseCase ObterUseCase(BlueprintOSDbContext context) =>
        new(new SincronizacaoFornecedorMonitorRepository(context));

    private static BlueprintOSDbContext NewContext() =>
        new(new DbContextOptionsBuilder<BlueprintOSDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
