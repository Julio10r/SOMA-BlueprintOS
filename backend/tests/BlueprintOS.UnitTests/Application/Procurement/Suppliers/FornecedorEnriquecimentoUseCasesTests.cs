using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Application.Procurement.Suppliers;
using BlueprintOS.Application.Procurement.Suppliers.Models;
using BlueprintOS.Domain.Procurement.Suppliers;
using BlueprintOS.Infrastructure.Persistence;
using BlueprintOS.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BlueprintOS.UnitTests.Application.Procurement.Suppliers;

public sealed class FornecedorEnriquecimentoUseCasesTests
{
    private readonly Guid _userId = Guid.NewGuid();

    [Fact]
    public async Task Analyze_Should_Not_Create_Divergence_When_Field_Is_Equal()
    {
        await using var context = NewContext();
        var fornecedor = Supplier();
        await context.Fornecedores.AddAsync(fornecedor);
        await context.SaveChangesAsync();

        var result = await new AnalisarEnriquecimentoFornecedorUseCase(new FornecedorRepository(context), new FakeIdentity(_userId))
            .ExecuteAsync(fornecedor.Id, new(Query(razaoSocial: "Fornecedor Atual"), null, "BU-A", "SOMA_DESENV", "corr-1"));

        Assert.NotNull(result);
        Assert.DoesNotContain(result!.Divergencias, x => x.Campo == nameof(Fornecedor.RazaoSocial));
    }

    [Fact]
    public async Task Analyze_Should_Create_Divergence_When_Field_Is_Different()
    {
        await using var context = NewContext();
        var fornecedor = Supplier();
        await context.Fornecedores.AddAsync(fornecedor);
        await context.SaveChangesAsync();

        var result = await new AnalisarEnriquecimentoFornecedorUseCase(new FornecedorRepository(context), new FakeIdentity(_userId))
            .ExecuteAsync(fornecedor.Id, new(Query(razaoSocial: "Fornecedor Novo"), null, "BU-A", null, "corr-2"));

        var divergence = Assert.Single(result!.Divergencias, x => x.Campo == nameof(Fornecedor.RazaoSocial));
        Assert.Equal("Fornecedor Atual", divergence.ValorAtual);
        Assert.Equal("Fornecedor Novo", divergence.ValorSugerido);
    }

    [Fact]
    public async Task Analyze_Should_Create_Cnae_Divergence_But_Never_Update_Fornecedor_Automatically()
    {
        // Principio absoluto (B2.8/B2.6): CONSULTA NAO ALTERA FORNECEDOR EXISTENTE AUTOMATICAMENTE.
        // A analise so calcula a divergencia; o CNAE persistido no fornecedor permanece intocado.
        await using var context = NewContext();
        var fornecedor = Supplier();
        await context.Fornecedores.AddAsync(fornecedor);
        await context.SaveChangesAsync();

        var result = await new AnalisarEnriquecimentoFornecedorUseCase(new FornecedorRepository(context), new FakeIdentity(_userId))
            .ExecuteAsync(fornecedor.Id, new(Query(cnaePrincipalCodigo: "6201501", cnaePrincipalDescricao: "Desenvolvimento de programas de computador sob encomenda"), null, "BU-A", null, "corr-cnae"));

        Assert.Contains(result!.Divergencias, x => x.Campo == nameof(Fornecedor.CnaePrincipalCodigo) && x.ValorSugerido == "6201501");
        var stored = await context.Fornecedores.SingleAsync();
        Assert.Null(stored.CnaePrincipalCodigo);
    }

    [Fact]
    public async Task Approve_Should_Persist_Cnae_Principal_Only_After_Explicit_Approval()
    {
        await using var context = NewContext();
        var fornecedor = Supplier();
        await context.Fornecedores.AddAsync(fornecedor);
        await context.SaveChangesAsync();

        var useCase = new AprovarEnriquecimentoFornecedorUseCase(new FornecedorRepository(context),
            new FornecedorEnriquecimentoAnaliseRepository(context), new FakeIdentity(_userId));
        await useCase.ExecuteAsync(fornecedor.Id, new(Query(cnaePrincipalCodigo: "62.01-5/01", cnaePrincipalDescricao: "Desenvolvimento de programas de computador sob encomenda"),
            null, [nameof(Fornecedor.CnaePrincipalCodigo), nameof(Fornecedor.CnaePrincipalDescricao)], "BU-A", null, "corr-aprova-cnae"));

        var stored = await context.Fornecedores.SingleAsync();
        Assert.Equal("6201501", stored.CnaePrincipalCodigo);
        Assert.Equal("Desenvolvimento de programas de computador sob encomenda", stored.CnaePrincipalDescricao);
    }

    [Fact]
    public async Task Approve_Should_Update_Only_Approved_Fields_And_Register_Audit()
    {
        await using var context = NewContext();
        var fornecedor = Supplier();
        await context.Fornecedores.AddAsync(fornecedor);
        await context.SaveChangesAsync();

        var useCase = new AprovarEnriquecimentoFornecedorUseCase(new FornecedorRepository(context),
            new FornecedorEnriquecimentoAnaliseRepository(context), new FakeIdentity(_userId));
        await useCase.ExecuteAsync(fornecedor.Id, new(Query(razaoSocial: "Fornecedor Novo", email: "novo@teste.com", nomeFantasia: "Fantasia CNPJ"),
            null, [nameof(Fornecedor.RazaoSocial), nameof(Fornecedor.NomeFantasia)], "BU-A", "SOMA_DESENV", "corr-aprova"));

        var stored = await context.Fornecedores.SingleAsync();
        Assert.Equal("Fornecedor Novo", stored.RazaoSocial);
        Assert.Null(stored.NomeFantasia);
        Assert.Null(stored.Email);
        var audit = await context.FornecedoresEnriquecimentoAnalises.OrderBy(x => x.Campo).ToArrayAsync();
        Assert.Equal(2, audit.Length);
        Assert.All(audit, x => Assert.Equal("Aceito", x.Decisao));
        Assert.All(audit, x => Assert.Equal("corr-aprova", x.CorrelationId));
    }

    [Fact]
    public async Task Reject_Should_Not_Update_Field_And_Register_Audit()
    {
        await using var context = NewContext();
        var fornecedor = Supplier();
        await context.Fornecedores.AddAsync(fornecedor);
        await context.SaveChangesAsync();

        var useCase = new RejeitarEnriquecimentoFornecedorUseCase(new FornecedorRepository(context),
            new FornecedorEnriquecimentoAnaliseRepository(context), new FakeIdentity(_userId));
        await useCase.ExecuteAsync(fornecedor.Id, new(Query(email: "novo@teste.com"), null, [nameof(Fornecedor.Email)], "BU-A", null, "corr-rejeita"));

        var stored = await context.Fornecedores.SingleAsync();
        Assert.Null(stored.Email);
        var audit = await context.FornecedoresEnriquecimentoAnalises.SingleAsync();
        Assert.Equal("Rejeitado", audit.Decisao);
        Assert.Equal("corr-rejeita", audit.CorrelationId);
        Assert.Equal(nameof(Fornecedor.Email), audit.Campo);
    }

    private Fornecedor Supplier() => new(Guid.NewGuid(), "Fornecedor Atual", Cnpj.Create("12345678000195"), null,
        null, null, null, "São Paulo", "SP", "BR", "Ativo", null, _userId, DateTimeOffset.UtcNow, "BU-A", "SOMA_DESENV", "F-1");

    private static ConsultaCnpjResultado Query(string? razaoSocial = null, string? email = null, string? nomeFantasia = null,
        string? cnaePrincipalCodigo = null, string? cnaePrincipalDescricao = null) =>
        ConsultaCnpjResultado.CriarSucesso("12345678000195", "ConsultaTeste", SituacaoCadastralCnpj.Ativa,
            DateTimeOffset.UtcNow, razaoSocial: razaoSocial, nomeFantasia: nomeFantasia, email: email,
            cnaePrincipalCodigo: cnaePrincipalCodigo, cnaePrincipalDescricao: cnaePrincipalDescricao);

    private static BlueprintOSDbContext NewContext() => new(new DbContextOptionsBuilder<BlueprintOSDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private sealed class FakeIdentity(Guid userId) : ICurrentIdentity
    {
        public RequestIdentity GetRequired() => new(userId, "Buyer");
    }
}
