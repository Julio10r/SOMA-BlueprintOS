using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Application.Procurement.Suppliers;
using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using BlueprintOS.Domain.Procurement.Suppliers;
using BlueprintOS.Infrastructure.Persistence;
using BlueprintOS.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BlueprintOS.UnitTests.Application.Procurement.Suppliers;

public sealed class GarantirFornecedorNoErpUseCaseTests
{
    [Fact]
    public async Task Should_Register_Erp_Link_And_Sync_Status_When_Adapter_Succeeds()
    {
        await using var context = NewContext();
        var user = new FakeIdentity();
        var fornecedor = new Fornecedor(Guid.NewGuid(), "Fornecedor Teste", Cnpj.Create("12345678000195"), null, null, null, null, "São Paulo", "SP", "BR", "Ativo", null, DateTimeOffset.UtcNow, user.UnidadeNegocioId);
        await new FornecedorRepository(context).AdicionarAsync(fornecedor);
        var adapter = new FakeAdapter { Resultado = new(OperacaoGarantirFornecedorErp.Criado, "123456", "BU-A", "SOMA_DESENV", DateTimeOffset.UtcNow, "corr-1") };
        var useCase = Create(context, user, adapter);

        var resultado = await useCase.ExecuteAsync(fornecedor.Id, "BU-A", new GarantirFornecedorNoErpDto("corr-1"));

        Assert.NotNull(resultado);
        Assert.Equal(OperacaoGarantirFornecedorErp.Criado, resultado!.Operacao);
        var persistido = await context.Fornecedores.SingleAsync();
        Assert.Equal("123456", persistido.ErpFornecedorId);
        Assert.Equal("SOMA_DESENV", persistido.ErpSistema);
        Assert.Equal("BU-A", persistido.BusinessUnit);
        Assert.Equal("Sincronizado", persistido.StatusSincronizacao);
        Assert.Equal(1, adapter.Chamadas);
    }

    /// <summary>Retest do Gate de Fornecedores (2026-09-01): a causa raiz do falso sucesso relatado era o
    /// use case nunca repassar os campos de endereço ao Adapter — o "Enviar ao ERP" convergia com sucesso
    /// (rowcount &gt; 0) sem sequer tentar gravar Cep/Logradouro/Numero/Complemento/Bairro. Este teste prova
    /// que o request enviado ao Adapter carrega exatamente os dados de endereço atuais do Fornecedor.</summary>
    [Fact]
    public async Task Should_Forward_Address_Fields_To_Erp_Adapter()
    {
        await using var context = NewContext();
        var user = new FakeIdentity();
        var fornecedor = new Fornecedor(Guid.NewGuid(), "Fornecedor Endereco", DocumentoFiscal.Create("12345678000195"), null, null, null, null, null,
            "São Paulo", "SP", "BR", "Ativo", null, DateTimeOffset.UtcNow, user.UnidadeNegocioId,
            cep: "01310-100", logradouro: "Avenida Paulista", numero: "1000", complemento: "Sala QA", bairro: "Bela Vista");
        await new FornecedorRepository(context).AdicionarAsync(fornecedor);
        var adapter = new FakeAdapter { Resultado = new(OperacaoGarantirFornecedorErp.Atualizado, "315525", "BU-A", "SOMA_DESENV", DateTimeOffset.UtcNow, "corr-1") };
        var useCase = Create(context, user, adapter);

        await useCase.ExecuteAsync(fornecedor.Id, "BU-A", new GarantirFornecedorNoErpDto("corr-1"));

        var enviado = adapter.UltimaRequisicao!;
        Assert.Equal("01310-100", enviado.Cep);
        Assert.Equal("Avenida Paulista", enviado.Logradouro);
        Assert.Equal("1000", enviado.Numero);
        Assert.Equal("Sala QA", enviado.Complemento);
        Assert.Equal("Bela Vista", enviado.Bairro);
    }

    /// <summary>REGRA FUNDAMENTAL do Retest do Gate: se o Adapter não conseguir confirmar a gravação real
    /// (aqui simulado por uma falha de persistência do Adapter, o mesmo caminho que a verificação pós-escrita
    /// do <c>SomaGarantirFornecedorErpAdapter</c> aciona quando o Linx não confirma um campo), o Fornecedor
    /// NUNCA deve ficar marcado como "Sincronizado" — o estado de sincronização anterior (ou "Pendente") deve
    /// ser preservado, nunca um sucesso mentiroso.</summary>
    [Fact]
    public async Task Should_Not_Mark_Sincronizado_When_Adapter_Fails_To_Confirm_Write()
    {
        await using var context = NewContext();
        var user = new FakeIdentity();
        var fornecedor = new Fornecedor(Guid.NewGuid(), "Fornecedor Falha ERP", Cnpj.Create("12345678000195"), null, null, null, null, "São Paulo", "SP", "BR", "Ativo", null, DateTimeOffset.UtcNow, user.UnidadeNegocioId);
        await new FornecedorRepository(context).AdicionarAsync(fornecedor);
        var statusAntes = fornecedor.StatusSincronizacao;
        var adapter = new FakeAdapter { FalharCom = new ErpFornecedorEscritaException(ErpFornecedorErro.Persistencia, "O ERP não confirmou a gravação do campo 'Bairro' do fornecedor — a operação foi revertida.") };
        var useCase = Create(context, user, adapter);

        await Assert.ThrowsAsync<ErpFornecedorEscritaException>(() => useCase.ExecuteAsync(fornecedor.Id, "BU-A", new GarantirFornecedorNoErpDto("corr-1")));

        var persistido = await context.Fornecedores.SingleAsync();
        Assert.Equal(statusAntes, persistido.StatusSincronizacao);
        Assert.NotEqual("Sincronizado", persistido.StatusSincronizacao);
    }

    [Fact]
    public async Task Should_Return_Null_When_Fornecedor_Does_Not_Exist()
    {
        await using var context = NewContext();
        var user = new FakeIdentity();
        var adapter = new FakeAdapter();
        var useCase = Create(context, user, adapter);

        var resultado = await useCase.ExecuteAsync(Guid.NewGuid(), "BU-A", new GarantirFornecedorNoErpDto(null));

        Assert.Null(resultado);
        Assert.Equal(0, adapter.Chamadas);
    }

    [Fact]
    public async Task Should_Reject_Empty_BusinessUnit()
    {
        await using var context = NewContext();
        var useCase = Create(context, new FakeIdentity(), new FakeAdapter());
        var ex = await Assert.ThrowsAsync<ErpFornecedorEscritaException>(() => useCase.ExecuteAsync(Guid.NewGuid(), " ", new GarantirFornecedorNoErpDto(null)));
        Assert.Equal(ErpFornecedorErro.Validacao, ex.Tipo);
    }

    [Fact]
    public async Task Should_Generate_CorrelationId_When_Not_Provided()
    {
        await using var context = NewContext();
        var user = new FakeIdentity();
        var fornecedor = new Fornecedor(Guid.NewGuid(), "Fornecedor Sem Correlation", Cnpj.Create("12345678000195"), null, null, null, null, "São Paulo", "SP", "BR", "Ativo", null, DateTimeOffset.UtcNow, user.UnidadeNegocioId);
        await new FornecedorRepository(context).AdicionarAsync(fornecedor);
        var adapter = new FakeAdapter();
        var useCase = Create(context, user, adapter);

        await useCase.ExecuteAsync(fornecedor.Id, "BU-A", new GarantirFornecedorNoErpDto(null));

        Assert.False(string.IsNullOrWhiteSpace(adapter.UltimaRequisicao!.CorrelationId));
    }

    [Fact]
    public async Task Should_Propagate_Same_Request_Idempotently_On_Repeated_Calls()
    {
        await using var context = NewContext();
        var user = new FakeIdentity();
        var fornecedor = new Fornecedor(Guid.NewGuid(), "Fornecedor Idempotente", Cnpj.Create("12345678000195"), null, null, null, null, "São Paulo", "SP", "BR", "Ativo", null, DateTimeOffset.UtcNow, user.UnidadeNegocioId);
        await new FornecedorRepository(context).AdicionarAsync(fornecedor);
        var adapter = new FakeAdapter { Resultado = new(OperacaoGarantirFornecedorErp.Criado, "654321", "BU-A", "SOMA_DESENV", DateTimeOffset.UtcNow, "corr-1") };
        var useCase = Create(context, user, adapter);

        await useCase.ExecuteAsync(fornecedor.Id, "BU-A", new GarantirFornecedorNoErpDto("corr-1"));
        adapter.Resultado = adapter.Resultado! with { Operacao = OperacaoGarantirFornecedorErp.Atualizado };
        var segunda = await useCase.ExecuteAsync(fornecedor.Id, "BU-A", new GarantirFornecedorNoErpDto("corr-1"));

        Assert.Equal(OperacaoGarantirFornecedorErp.Atualizado, segunda!.Operacao);
        Assert.Equal(2, adapter.Chamadas);
        Assert.Single(await context.Fornecedores.ToListAsync());
    }

    private static GarantirFornecedorNoErpUseCase Create(BlueprintOSDbContext context, FakeIdentity identity, FakeAdapter adapter) =>
        new(new FornecedorRepository(context), new FakeResolver(adapter));

    private static BlueprintOSDbContext NewContext() => new(new DbContextOptionsBuilder<BlueprintOSDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private sealed class FakeIdentity : ICurrentIdentity
    {
        public Guid UserId { get; } = Guid.NewGuid();
        public Guid UnidadeNegocioId { get; } = Guid.NewGuid();
        public RequestIdentity GetRequired() => new(UserId, "Buyer", UnidadeNegocioId);
    }

    private sealed class FakeResolver(FakeAdapter adapter) : IGarantirFornecedorErpAdapterResolver
    { public IGarantirFornecedorErpAdapter Resolver(string businessUnit) => adapter; }

    private sealed class FakeAdapter : IGarantirFornecedorErpAdapter
    {
        public string ErpSistema => "SOMA_DESENV";
        public GarantirFornecedorErpResultado? Resultado { get; set; }
        public ErpFornecedorEscritaException? FalharCom { get; set; }
        public GarantirFornecedorErpRequest? UltimaRequisicao { get; private set; }
        public int Chamadas { get; private set; }

        public Task<GarantirFornecedorErpResultado> GarantirAsync(GarantirFornecedorErpRequest request, CancellationToken cancellationToken = default)
        {
            Chamadas++;
            UltimaRequisicao = request;
            if (FalharCom is not null) throw FalharCom;
            var resultado = Resultado ?? new GarantirFornecedorErpResultado(OperacaoGarantirFornecedorErp.Criado, "000001", request.BusinessUnit, ErpSistema, DateTimeOffset.UtcNow, request.CorrelationId);
            return Task.FromResult(resultado);
        }
    }
}
