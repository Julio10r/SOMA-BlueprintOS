using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using BlueprintOS.Application.Procurement.Suppliers.Models;
using BlueprintOS.Domain.Procurement.Suppliers;
using BlueprintOS.Infrastructure.Integrations.ERP.Contracts;
using BlueprintOS.Infrastructure.Integrations.ERP.Soma;
using BlueprintOS.Infrastructure.Persistence;
using BlueprintOS.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BlueprintOS.UnitTests.Application.Procurement.Suppliers;

public sealed class SincronizarFornecedoresErpUseCaseTests
{
    [Fact]
    public async Task Execute_Should_Map_Erp_Data_To_Domain_And_Insert_New_Supplier()
    {
        await using var context = NewContext();
        var identity = new FakeIdentity();
        var reader = new FakeReader(new FornecedorErpIntegracaoDto("ERP-10", "SOMA_DESENV", Canonical("Fornecedor ERP", "Fantasia ERP", "12345678000195", "hash-1"), DateTimeOffset.UtcNow));

        var result = await Create(context, identity, reader).ExecuteAsync(new("BU-A", 100, "corr-erp"));

        var stored = await context.Fornecedores.SingleAsync();
        var execucao = await context.SincronizacoesFornecedores.SingleAsync();
        Assert.Equal(result.ExecucaoId, execucao.Id);
        Assert.Equal("Sucesso", result.Status);
        Assert.Equal(1, result.Consultados);
        Assert.Equal(1, result.Incluidos);
        Assert.Equal("Fornecedor ERP", stored.RazaoSocial);
        Assert.Equal("Fantasia ERP", stored.NomeFantasia);
        Assert.Equal("ERP", stored.OrigemInformacao);
        Assert.Equal("SOMA_DESENV", stored.ErpSistema);
        Assert.Equal("ERP-10", stored.ErpFornecedorId);
    }

    [Fact]
    public async Task Execute_Should_Update_Existing_Supplier_And_Preserve_NomeFantasia_Only_For_Erp_Source()
    {
        await using var context = NewContext();
        var identity = new FakeIdentity();
        var existing = new Fornecedor(Guid.NewGuid(), "Fornecedor Antigo", DocumentoFiscal.Create("12345678000195"), null, null, null, null,
            null, "Rio de Janeiro", "RJ", "BR", "Ativo", null, identity.UserId, DateTimeOffset.UtcNow.AddDays(-1));
        existing.AplicarContratoCanonico(Canonical("Fornecedor Antigo", "Fantasia Original ERP", "12345678000195", "old"), "ERP", DateTimeOffset.UtcNow.AddDays(-1));
        await new FornecedorRepository(context).AdicionarAsync(existing);

        var reader = new FakeReader(new FornecedorErpIntegracaoDto("ERP-10", "SOMA_DESENV", Canonical("Fornecedor Atualizado", "Fantasia Nova ERP", "12345678000195", "hash-2"), DateTimeOffset.UtcNow));
        var result = await Create(context, identity, reader).ExecuteAsync(new("BU-A", 100, null));

        var stored = await context.Fornecedores.SingleAsync();
        Assert.Equal(1, result.Atualizados);
        Assert.Equal("Fornecedor Atualizado", stored.RazaoSocial);
        Assert.Equal("Fantasia Nova ERP", stored.NomeFantasia);

        stored.AplicarContratoCanonico(Canonical("Alteracao MaisCompras", "Fantasia Manual", "12345678000195", "manual"), "MaisCompras", DateTimeOffset.UtcNow);
        Assert.Equal("Fantasia Nova ERP", stored.NomeFantasia);
    }

    [Fact]
    public async Task Execute_Should_Count_Unchanged_When_Hash_Matches()
    {
        await using var context = NewContext();
        var identity = new FakeIdentity();
        var dados = Canonical("Fornecedor ERP", "Fantasia ERP", "12345678000195", "hash-1");
        var existing = new Fornecedor(Guid.NewGuid(), dados.RazaoSocial, DocumentoFiscal.Create(dados.DocumentoFiscal), dados.TipoPessoa, null, null,
            null, null, dados.Cidade, dados.Uf, dados.Pais, "Ativo", null, identity.UserId, DateTimeOffset.UtcNow, "BU-A", "SOMA_DESENV", "ERP-10");
        existing.AplicarContratoCanonico(dados, "ERP", DateTimeOffset.UtcNow);
        await new FornecedorRepository(context).AdicionarAsync(existing);

        var result = await Create(context, identity, new FakeReader(new FornecedorErpIntegracaoDto("ERP-10", "SOMA_DESENV", dados, DateTimeOffset.UtcNow)))
            .ExecuteAsync(new("BU-A", 100, null));

        Assert.Equal(1, result.SemAlteracao);
        Assert.Equal(0, result.Atualizados);
    }

    [Fact]
    public async Task Execute_Should_Abort_When_First_Page_Is_Empty()
    {
        // Requisito 4a (guarda de seguranca): antes desta mudanca, uma primeira pagina vazia da fonte
        // era reportada como "Sucesso" com zero registros. Isso escondia falhas reais de conectividade
        // com o ERP atras de um resultado que parecia sucesso legitimo, entao o comportamento mudou para
        // abortar explicitamente com um status distinto (AbortadoFonteVazia) em vez de fingir sucesso.
        await using var context = NewContext();

        var result = await Create(context, new FakeIdentity(), new FakeReader())
            .ExecuteAsync(new("BU-A", 2, null));

        Assert.Equal("AbortadoFonteVazia", result.Status);
        Assert.Equal(0, result.Consultados);
        Assert.Equal(0, result.Incluidos);
        Assert.Equal(0, result.Erros);
        Assert.Equal("AbortadoFonteVazia", (await context.SincronizacoesFornecedores.SingleAsync()).Status);
    }

    [Fact]
    public async Task Execute_DryRun_Should_Classify_Without_Persisting_Anything()
    {
        await using var context = NewContext();
        var identity = new FakeIdentity();
        var reader = new FakeReader(new FornecedorErpIntegracaoDto("ERP-10", "SOMA_DESENV", Canonical("Fornecedor ERP", "Fantasia ERP", "12345678000195", "hash-1"), DateTimeOffset.UtcNow));

        var result = await Create(context, identity, reader).ExecuteAsync(new("BU-A", 100, "corr-erp", DryRun: true));

        Assert.Equal("DryRunConcluido", result.Status);
        Assert.Equal(1, result.Consultados);
        Assert.Equal(1, result.Incluidos);
        // Nada foi gravado: nem o Fornecedor nem a SincronizacaoFornecedor como execucao real.
        Assert.Equal(0, await context.Fornecedores.CountAsync());
        Assert.Equal(0, await context.SincronizacoesFornecedores.CountAsync());
    }

    [Fact]
    public async Task Execute_DryRun_Should_Report_Would_Be_Inactivations_Without_Applying_Them()
    {
        await using var context = NewContext();
        var identity = new FakeIdentity();
        var ativoNoErp = Canonical("Fornecedor Ativo", "Fantasia", "12345678000195", "old");
        var existente = new Fornecedor(Guid.NewGuid(), ativoNoErp.RazaoSocial, DocumentoFiscal.Create(ativoNoErp.DocumentoFiscal), ativoNoErp.TipoPessoa,
            null, null, null, null, ativoNoErp.Cidade, ativoNoErp.Uf, ativoNoErp.Pais, "Ativo", null, identity.UserId, DateTimeOffset.UtcNow);
        existente.AplicarContratoCanonico(ativoNoErp, "ERP", DateTimeOffset.UtcNow);
        await new FornecedorRepository(context).AdicionarAsync(existente);

        var agoraInativo = ativoNoErp with { Ativo = false, HashDadosSincronizaveis = "novo" };
        var reader = new FakeReader(new FornecedorErpIntegracaoDto("ERP-10", "SOMA_DESENV", agoraInativo, DateTimeOffset.UtcNow));

        var result = await Create(context, identity, reader).ExecuteAsync(new("BU-A", 100, null, DryRun: true));

        Assert.Equal("DryRunConcluido", result.Status);
        Assert.Equal(1, result.TotalInativados);
        Assert.Equal("Ativo", (await context.Fornecedores.SingleAsync()).Status);
    }

    [Fact]
    public async Task Execute_Should_Abort_Inactivations_When_Percentage_Is_Abnormal()
    {
        await using var context = NewContext();
        var identity = new FakeIdentity();
        var repository = new FornecedorRepository(context);

        // Um unico fornecedor Ativo hoje; o ERP manda ele como Inativo => 100% de inativacao, acima do
        // limiar de 30% (guarda 4b). A inativacao nao deve ser aplicada.
        var dadosAtivos = Canonical("Fornecedor Unico", "Fantasia", "12345678000195", "old");
        var existente = new Fornecedor(Guid.NewGuid(), dadosAtivos.RazaoSocial, DocumentoFiscal.Create(dadosAtivos.DocumentoFiscal), dadosAtivos.TipoPessoa,
            null, null, null, null, dadosAtivos.Cidade, dadosAtivos.Uf, dadosAtivos.Pais, "Ativo", null, identity.UserId, DateTimeOffset.UtcNow);
        existente.AplicarContratoCanonico(dadosAtivos, "ERP", DateTimeOffset.UtcNow);
        await repository.AdicionarAsync(existente);

        var dadosInativos = dadosAtivos with { Ativo = false, HashDadosSincronizaveis = "novo" };
        var reader = new FakeReader(new FornecedorErpIntegracaoDto("ERP-10", "SOMA_DESENV", dadosInativos, DateTimeOffset.UtcNow));

        var result = await Create(context, identity, reader, repository).ExecuteAsync(new("BU-A", 100, null));

        Assert.Equal("AbortadoInativacaoAnormal", result.Status);
        Assert.Equal(0, result.TotalInativados);
        Assert.Equal("Ativo", (await context.Fornecedores.SingleAsync()).Status);
    }

    [Fact]
    public async Task Execute_Should_Reject_Concurrent_Execution_For_Same_BusinessUnit()
    {
        await using var context = NewContext();
        var identity = new FakeIdentity();
        var emAndamento = new SincronizacaoFornecedor(Guid.NewGuid(), "SOMA_DESENV", "BU-A", DateTimeOffset.UtcNow, identity.UnidadeNegocioId);
        emAndamento.MarcarEmAndamento();
        await context.SincronizacoesFornecedores.AddAsync(emAndamento);
        await context.SaveChangesAsync();

        var reader = new FakeReader(new FornecedorErpIntegracaoDto("ERP-10", "SOMA_DESENV", Canonical("Fornecedor ERP", "Fantasia ERP", "12345678000195", "hash-1"), DateTimeOffset.UtcNow));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Create(context, identity, reader).ExecuteAsync(new("BU-A", 100, null)));
    }

    [Fact]
    public async Task Execute_Should_Paginate_Until_Source_Exhausted_When_No_Limit_Is_Informed()
    {
        await using var context = NewContext();
        var identity = new FakeIdentity();
        var documentos = new[] { "12345678000195", "11222333000181", "99888777000100", "22333444000181",
            "33444555000181", "12345678000276", "98765432000198" };
        var fornecedores = documentos.Select((doc, i) => new FornecedorErpIntegracaoDto($"ERP-{i}", "SOMA_DESENV",
                Canonical($"Fornecedor {i}", $"Fantasia {i}", doc, $"hash-{i}"), DateTimeOffset.UtcNow))
            .ToArray();
        var reader = new FakeReader(fornecedores);

        // Limite <= 0 (nao informado) => sem teto artificial: pagina até a fonte esgotar.
        var result = await Create(context, identity, reader).ExecuteAsync(new("BU-A", 0, null));

        Assert.Equal(7, result.Consultados);
        Assert.False(result.PossivelmenteTruncado);
        Assert.Equal(7, await context.Fornecedores.CountAsync());
    }

    [Fact]
    public async Task Execute_Should_Mark_PossivelmenteTruncado_When_Limite_Is_Reached_But_Source_Has_More()
    {
        await using var context = NewContext();
        var identity = new FakeIdentity();
        var reader = new FakeReader(
            new("ERP-1", "SOMA_DESENV", Canonical("Fornecedor 1", "Fantasia 1", "12345678000195", "hash-1"), DateTimeOffset.UtcNow),
            new("ERP-2", "SOMA_DESENV", Canonical("Fornecedor 2", "Fantasia 2", "11222333000181", "hash-2"), DateTimeOffset.UtcNow),
            new("ERP-3", "SOMA_DESENV", Canonical("Fornecedor 3", "Fantasia 3", "99888777000100", "hash-3"), DateTimeOffset.UtcNow));

        var result = await Create(context, identity, reader).ExecuteAsync(new("BU-A", 2, null));

        Assert.Equal(2, result.Consultados);
        Assert.True(result.PossivelmenteTruncado);
    }

    [Fact]
    public async Task Execute_Should_Process_All_Available_When_Below_Limite()
    {
        await using var context = NewContext();
        var identity = new FakeIdentity();
        var unchanged = Canonical("Fornecedor Igual", "Fantasia Igual", "12345678000195", "hash-1");
        var existing = new Fornecedor(Guid.NewGuid(), unchanged.RazaoSocial, DocumentoFiscal.Create(unchanged.DocumentoFiscal), unchanged.TipoPessoa,
            null, null, null, null, unchanged.Cidade, unchanged.Uf, unchanged.Pais, "Ativo", null, identity.UserId, DateTimeOffset.UtcNow);
        existing.AplicarContratoCanonico(unchanged, "ERP", DateTimeOffset.UtcNow);
        await new FornecedorRepository(context).AdicionarAsync(existing);

        var changed = Canonical("Fornecedor Alterado", "Fantasia Alterada", "11222333000181", "old");
        var existingChanged = new Fornecedor(Guid.NewGuid(), "Fornecedor Antes", DocumentoFiscal.Create(changed.DocumentoFiscal), changed.TipoPessoa,
            null, null, null, null, changed.Cidade, changed.Uf, changed.Pais, "Ativo", null, identity.UserId, DateTimeOffset.UtcNow);
        existingChanged.AplicarContratoCanonico(changed with { RazaoSocial = "Fornecedor Antes", HashDadosSincronizaveis = "old" }, "ERP", DateTimeOffset.UtcNow);
        await new FornecedorRepository(context).AdicionarAsync(existingChanged);

        var reader = new FakeReader(
            new("ERP-1", "SOMA_DESENV", unchanged, DateTimeOffset.UtcNow),
            new("ERP-2", "SOMA_DESENV", changed with { HashDadosSincronizaveis = "new" }, DateTimeOffset.UtcNow),
            new("ERP-3", "SOMA_DESENV", Canonical("Fornecedor Novo", "Fantasia Nova", "99888777000100", "hash-3"), DateTimeOffset.UtcNow));

        var result = await Create(context, identity, reader).ExecuteAsync(new("BU-A", 10, null));

        Assert.Equal(3, result.Consultados);
        Assert.Equal(1, result.Incluidos);
        Assert.Equal(1, result.Atualizados);
        Assert.Equal(1, result.SemAlteracao);
        Assert.Equal(0, result.Erros);
        Assert.Equal(new[] { (0, 10), (3, 7) }, reader.Calls);
    }

    [Fact]
    public async Task Execute_Should_Never_Process_More_Than_Limite_Even_When_Erp_Has_More_Records()
    {
        await using var context = NewContext();
        var identity = new FakeIdentity();
        var reader = new FakeReader(
            new("ERP-1", "SOMA_DESENV", Canonical("Fornecedor 1", "Fantasia 1", "12345678000195", "hash-1"), DateTimeOffset.UtcNow),
            new("ERP-2", "SOMA_DESENV", Canonical("Fornecedor 2", "Fantasia 2", "11222333000181", "hash-2"), DateTimeOffset.UtcNow),
            new("ERP-3", "SOMA_DESENV", Canonical("Fornecedor 3", "Fantasia 3", "99888777000100", "hash-3"), DateTimeOffset.UtcNow),
            new("ERP-4", "SOMA_DESENV", Canonical("Fornecedor 4", "Fantasia 4", "22333444000181", "hash-4"), DateTimeOffset.UtcNow),
            new("ERP-5", "SOMA_DESENV", Canonical("Fornecedor 5", "Fantasia 5", "33444555000181", "hash-5"), DateTimeOffset.UtcNow));

        var result = await Create(context, identity, reader).ExecuteAsync(new("BU-A", 3, null));

        Assert.Equal(3, result.Consultados);
        Assert.Equal(3, result.Incluidos);
        Assert.Equal(3, await context.Fornecedores.CountAsync());
        Assert.Equal(new[] { (0, 3) }, reader.Calls);
    }

    [Fact]
    public async Task Execute_Should_Register_Partial_Error_And_Continue()
    {
        await using var context = NewContext();
        var reader = new FakeReader(
            new("ERP-1", "SOMA_DESENV", Canonical("Fornecedor OK", "Fantasia OK", "12345678000195", "hash-1"), DateTimeOffset.UtcNow),
            new("ERP-2", "SOMA_DESENV", Canonical("Fornecedor Erro", "Fantasia Erro", "documento-invalido", "hash-2"), DateTimeOffset.UtcNow),
            new("ERP-3", "SOMA_DESENV", Canonical("Fornecedor OK 2", "Fantasia OK 2", "99888777000100", "hash-3"), DateTimeOffset.UtcNow));

        var result = await Create(context, new FakeIdentity(), reader).ExecuteAsync(new("BU-A", 10, null));

        Assert.Equal("Parcial", result.Status);
        Assert.Equal(3, result.Consultados);
        Assert.Equal(2, result.Incluidos);
        Assert.Equal(1, result.Erros);
        Assert.Equal(2, await context.Fornecedores.CountAsync());
        Assert.Equal(1, await context.ErrosSincronizacoesFornecedores.CountAsync());
    }

    [Fact]
    public async Task Execute_Should_Finish_As_Parcial_And_Persist_Execucao_When_Individual_SaveChanges_Fails()
    {
        await using var context = NewContext();
        var identity = new FakeIdentity();
        var innerRepository = new FornecedorRepository(context);

        // Simula o cenario real observado contra SQL Server: o repositorio rastreia a entidade
        // no DbContext e o SaveChangesAsync falha (ex.: violacao de indice unico). Sem limpar o
        // ChangeTracker apos o erro, a entidade problematica permanece "Added" e faz o proximo
        // SaveChangesAsync (inclusive o final, ao persistir a SincronizacaoFornecedor) falhar tambem.
        var repository = new ThrowingOnceFornecedorRepository(context, innerRepository, cnpjQueFalha: "12345678000195");

        var reader = new FakeReader(
            new("ERP-1", "SOMA_DESENV", Canonical("Fornecedor Falho", "Fantasia Falha", "12345678000195", "hash-falha"), DateTimeOffset.UtcNow),
            new("ERP-2", "SOMA_DESENV", Canonical("Fornecedor OK", "Fantasia OK", "99888777000100", "hash-ok"), DateTimeOffset.UtcNow));

        var result = await Create(context, identity, reader, repository).ExecuteAsync(new("BU-A", 10, null));

        Assert.Equal("Parcial", result.Status);
        Assert.Equal(2, result.Consultados);
        Assert.Equal(1, result.Incluidos);
        Assert.Equal(1, result.Erros);
        Assert.Equal(1, await context.ErrosSincronizacoesFornecedores.CountAsync());
        Assert.Equal(1, await context.SincronizacoesFornecedores.CountAsync());
        Assert.Equal("Fornecedor OK", (await context.Fornecedores.SingleAsync(x => x.TemporaryUserId == identity.UserId)).RazaoSocial);
    }

    private static SincronizarFornecedoresErpUseCase Create(BlueprintOSDbContext context, FakeIdentity identity, FakeReader reader,
        IFornecedorRepository? repository = null) =>
        new(reader, repository ?? new FornecedorRepository(context), new SincronizacaoFornecedorMonitorRepository(context),
            identity, context, NullLogger<SincronizarFornecedoresErpUseCase>.Instance);

    private static BlueprintOSDbContext NewContext() =>
        new(new DbContextOptionsBuilder<BlueprintOSDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static FornecedorCanonico Canonical(string razaoSocial, string nomeFantasia, string documento, string hash) =>
        new(razaoSocial, nomeFantasia, documento, "PJ", "BR", null, null, "01001000", "Rua ERP", "100", null, "Centro",
            "Sao Paulo", "SP", null, "11", "999999999", "erp@example.invalid", "fiscal@example.invalid", null, null, null,
            null, "001", "Fornecedor", null, null, "Normal", false, null, true, false, true, false, false, false, true,
            DateTimeOffset.UtcNow, hash);

    /// <summary>
    /// Reproduz uma falha real de SaveChangesAsync (ex.: violacao de indice unico no SQL Server):
    /// a entidade fica rastreada como Added no DbContext e a chamada lanca uma excecao, sem chamar
    /// SaveChangesAsync com sucesso.
    /// </summary>
    private sealed class ThrowingOnceFornecedorRepository(BlueprintOSDbContext context, IFornecedorRepository inner, string cnpjQueFalha) : IFornecedorRepository
    {
        public async Task AdicionarAsync(Fornecedor fornecedor, CancellationToken cancellationToken = default)
        {
            if (fornecedor.Cnpj_Cpf == DocumentoFiscal.Create(cnpjQueFalha).Value)
            {
                await context.Fornecedores.AddAsync(fornecedor, cancellationToken);
                throw new DbUpdateException("Simulated unique index violation for tests.");
            }

            await inner.AdicionarAsync(fornecedor, cancellationToken);
        }

        public Task AtualizarAsync(Fornecedor fornecedor, CancellationToken cancellationToken = default) => inner.AtualizarAsync(fornecedor, cancellationToken);
        public Task<Fornecedor?> ObterPorIdAsync(Guid id, Guid temporaryUserId, CancellationToken cancellationToken = default) => inner.ObterPorIdAsync(id, temporaryUserId, cancellationToken);
        public Task<Fornecedor?> ObterPorCnpjAsync(string cnpj, Guid temporaryUserId, CancellationToken cancellationToken = default) => inner.ObterPorCnpjAsync(cnpj, temporaryUserId, cancellationToken);
        public Task<IReadOnlyList<Fornecedor>> PesquisarAsync(string termo, Guid temporaryUserId, CancellationToken cancellationToken = default) => inner.PesquisarAsync(termo, temporaryUserId, cancellationToken);
        public Task<IReadOnlyList<Fornecedor>> ListarAsync(Guid temporaryUserId, CancellationToken cancellationToken = default) => inner.ListarAsync(temporaryUserId, cancellationToken);
        public Task<bool> ExisteAsync(string documentoFiscal, CancellationToken cancellationToken = default) => inner.ExisteAsync(documentoFiscal, cancellationToken);
        public Task<Fornecedor?> ObterPorCnpjSemRastreamentoAsync(string cnpj, Guid temporaryUserId, CancellationToken cancellationToken = default) =>
            inner.ObterPorCnpjSemRastreamentoAsync(cnpj, temporaryUserId, cancellationToken);
        public Task<int> ContarAtivosAsync(Guid temporaryUserId, CancellationToken cancellationToken = default) =>
            inner.ContarAtivosAsync(temporaryUserId, cancellationToken);
        public Task<FornecedorPesquisaPaginadaResultado> PesquisarPaginadoAsync(Guid temporaryUserId, string? termo,
            FornecedorStatusFiltro status, FornecedorOrdenacaoCampo ordenarPor, bool ordenarDescendente,
            int page, int pageSize, CancellationToken cancellationToken = default) =>
            inner.PesquisarPaginadoAsync(temporaryUserId, termo, status, ordenarPor, ordenarDescendente, page, pageSize, cancellationToken);
    }

    private sealed class FakeIdentity : ICurrentIdentity
    {
        public Guid UserId { get; } = Guid.NewGuid();
        public Guid UnidadeNegocioId { get; } = Guid.NewGuid();
        public RequestIdentity GetRequired() => new(UserId, "Buyer", UnidadeNegocioId);
    }

    private sealed class FakeReader(params FornecedorErpIntegracaoDto[] fornecedores) : IFornecedorErpReader
    {
        public List<(int Skip, int Take)> Calls { get; } = [];

        public Task<IReadOnlyList<FornecedorErpIntegracaoDto>> BuscarFornecedoresAsync(int limite, CancellationToken cancellationToken = default) =>
            BuscarFornecedoresAsync(0, limite, cancellationToken);

        public Task<IReadOnlyList<FornecedorErpIntegracaoDto>> BuscarFornecedoresAsync(int skip, int take, CancellationToken cancellationToken = default)
        {
            Calls.Add((skip, take));
            return Task.FromResult<IReadOnlyList<FornecedorErpIntegracaoDto>>(fornecedores.Skip(skip).Take(take).ToList());
        }
    }
}
