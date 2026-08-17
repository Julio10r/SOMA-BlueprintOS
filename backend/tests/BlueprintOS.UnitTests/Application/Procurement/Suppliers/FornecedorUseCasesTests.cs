using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Application.Procurement.Suppliers;
using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using BlueprintOS.Application.Procurement.Suppliers.Models;
using BlueprintOS.Domain.Procurement.Suppliers;
using BlueprintOS.Domain.Identity;
using Microsoft.Extensions.Logging.Abstractions;

namespace BlueprintOS.UnitTests.Application.Procurement.Suppliers;

public sealed class FornecedorUseCasesTests
{
    [Fact]
    public async Task Cadastrar_Should_Create_Supplier_With_Current_Temporary_User()
    {
        var identity = new FakeIdentity(); var repository = new FakeRepository();
        var result = await CreateUseCase(repository, identity).ExecuteAsync(CreateDto());
        Assert.Equal(identity.UserId, result.TemporaryUserId);
        Assert.Equal("12345678000195", result.Cnpj_Cpf);
    }

    [Fact]
    public async Task Cadastrar_Should_Persist_Cnae_Principal_When_Informed()
    {
        // B2.8: CNAE principal so e persistido na operacao explicita de cadastro (nunca por
        // consulta isolada). Codigo normalizado para digitos puros mesmo se vier mascarado.
        var result = await CreateUseCase(new FakeRepository(), new FakeIdentity())
            .ExecuteAsync(CreateDto() with { CnaePrincipalCodigo = "62.01-5/01", CnaePrincipalDescricao = "Desenvolvimento de programas de computador sob encomenda" });

        Assert.Equal("6201501", result.CnaePrincipalCodigo);
        Assert.Equal("Desenvolvimento de programas de computador sob encomenda", result.CnaePrincipalDescricao);
    }

    [Fact]
    public async Task Cadastrar_Should_Succeed_Without_Cnae_Principal()
    {
        // Cadastro manual apos falha do Provider (B2.6): CNAE ausente nunca impede o cadastro.
        var result = await CreateUseCase(new FakeRepository(), new FakeIdentity()).ExecuteAsync(CreateDto());

        Assert.Null(result.CnaePrincipalCodigo);
        Assert.Null(result.CnaePrincipalDescricao);
    }

    [Fact]
    public async Task Cadastrar_Should_Reject_Duplicate_Cnpj()
    {
        var repository = new FakeRepository { ExistingCnpj = "12345678000195" };
        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateUseCase(repository, new FakeIdentity()).ExecuteAsync(CreateDto()));
    }

    [Theory]
    [InlineData("12345678909", "PF")]
    [InlineData("123.456.789-09", "PF")]
    [InlineData("12345678000195", "PJ")]
    [InlineData("12.345.678/0001-95", "PJ")]
    public async Task Cadastrar_Should_Accept_Valid_Cpf_Or_Cnpj_Masked_Or_Unmasked(string documento, string tipoPessoa)
    {
        var result = await CreateUseCase(new FakeRepository(), new FakeIdentity())
            .ExecuteAsync(CreateDto() with { Cnpj_Cpf = documento, TipoPessoa = tipoPessoa });

        var esperado = new string(documento.Where(char.IsDigit).ToArray());
        Assert.Equal(esperado, result.Cnpj_Cpf);
        Assert.Equal(tipoPessoa, result.TipoPessoa);
    }

    [Fact]
    public async Task Cadastrar_Should_Reject_Documento_Fiscal_Above_14_Characters()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => CreateUseCase(new FakeRepository(), new FakeIdentity())
            .ExecuteAsync(CreateDto() with { Cnpj_Cpf = "123456789012345" }));
    }

    [Fact]
    public async Task Cadastrar_Should_Reject_Alphanumeric_Documento_Fiscal()
    {
        // BUG-4 (ADR-0023): documento alfanumérico não é mais aceito pelo domínio +Compras — só CPF/CNPJ
        // com dígito verificador válido. Compatibilidade com códigos legados alfanuméricos do Linx
        // pertence exclusivamente a um futuro Adapter Linx (B2.9), nunca ao Value Object canônico.
        await Assert.ThrowsAsync<ArgumentException>(() => CreateUseCase(new FakeRepository(), new FakeIdentity())
            .ExecuteAsync(CreateDto() with { Cnpj_Cpf = "AB12345678901", TipoPessoa = "PF" }));
    }

    [Fact]
    public async Task Cadastrar_Should_Reject_Cnpj_With_Invalid_Check_Digit()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => CreateUseCase(new FakeRepository(), new FakeIdentity())
            .ExecuteAsync(CreateDto() with { Cnpj_Cpf = "12345678000190" }));
    }

    [Fact]
    public async Task Update_Should_Not_Expose_Supplier_From_Another_User()
    {
        var repository = new FakeRepository();
        var supplier = new Fornecedor(Guid.NewGuid(), "Empresa", Cnpj.Create("12345678000195"), null, null, null, null, null, null, null, "Ativo", null, Guid.NewGuid(), DateTimeOffset.UtcNow);
        repository.Items.Add(supplier);
        var result = await CreateAtualizarUseCase(repository, new FakeIdentity()).ExecuteAsync(supplier.Id, new AtualizarFornecedorDto("Novo", null, null, null, null, null, null, null, null, null));
        Assert.Null(result);
    }

    [Fact]
    public async Task Cadastrar_Should_Persist_Reviewed_Address_And_NomeFantasia_Values()
    {
        // Secao 5 da rodada B2.9: o valor persistido deve ser o valor revisado pelo usuario, nunca o
        // valor original sugerido pelo provider — aqui simulado por um cadastro manual completo.
        var result = await CreateUseCase(new FakeRepository(), new FakeIdentity()).ExecuteAsync(CreateDto() with
        {
            NomeFantasia = "Fantasia Revisada", Cep = "01310-100", Logradouro = "Av. Paulista", Numero = "1000",
            Complemento = "Sala 10", Bairro = "Bela Vista"
        });

        Assert.Equal("Fantasia Revisada", result.NomeFantasia);
        Assert.Equal("01310-100", result.Cep);
        Assert.Equal("Av. Paulista", result.Logradouro);
        Assert.Equal("1000", result.Numero);
        Assert.Equal("Sala 10", result.Complemento);
        Assert.Equal("Bela Vista", result.Bairro);
    }

    [Fact]
    public async Task Cadastrar_Should_Call_Garantir_No_Erp_With_Default_BusinessUnit_When_Session_Has_No_Bu()
    {
        // BU nunca vem do frontend/request (secao 4 da rodada B2.9) — aqui a sessao (Development) nao
        // carrega a claim de BU, e o backend cai no unico ERP configurado ("DEFAULT"), nunca em um
        // valor arbitrario vindo de fora.
        var garantir = new FakeGarantirNoErpUseCase();
        var result = await CreateUseCase(new FakeRepository(), new FakeIdentity(), garantir).ExecuteAsync(CreateDto());

        Assert.Single(garantir.Chamadas);
        Assert.Equal(result.Id, garantir.Chamadas[0].FornecedorId);
        Assert.Equal("DEFAULT", garantir.Chamadas[0].BusinessUnit);
    }

    [Fact]
    public async Task Cadastrar_Should_Not_Fail_When_Erp_Adapter_Fails_And_Should_Keep_Pendente()
    {
        // Comando funcional unico (secao 3): o cadastro em +Compras nao pode falhar so porque o ERP
        // falhou. O Fornecedor permanece salvo localmente com StatusSincronizacao="Pendente" para
        // convergir numa proxima tentativa (idempotente), sem fingir atomicidade distribuida (secao 7).
        var repository = new FakeRepository();
        var garantir = new FakeGarantirNoErpUseCase { Falha = new ErpFornecedorEscritaException(ErpFornecedorErro.Conectividade, "Falha de conectividade com o ERP.") };
        var result = await CreateUseCase(repository, new FakeIdentity(), garantir).ExecuteAsync(CreateDto());

        Assert.NotNull(result);
        var persistido = Assert.Single(repository.Items);
        Assert.Equal("Pendente", persistido.StatusSincronizacao);
        Assert.NotNull(persistido.MensagemErroSincronizacao);
    }

    [Fact]
    public async Task Atualizar_Should_Retry_Garantir_No_Erp_And_Keep_Pendente_On_Failure()
    {
        // Reabertura por CNPJ (secao 6): confirmar a Review de um Fornecedor existente deve retentar a
        // integracao ERP (convergencia), e uma falha aqui tambem nao pode bloquear o UPDATE local.
        var repository = new FakeRepository();
        var supplier = new Fornecedor(Guid.NewGuid(), "Empresa", Cnpj.Create("12345678000195"), null, null, null, null, null, null, null, "Ativo", null,
            Guid.NewGuid(), DateTimeOffset.UtcNow);
        var identity = new FakeIdentity();
        repository.Items.Add(supplier);
        var supplierWithOwner = new Fornecedor(supplier.Id, "Empresa", Cnpj.Create("12345678000195"), null, null, null, null, null, null, null, "Ativo", null,
            identity.UserId, DateTimeOffset.UtcNow);
        repository.Items.Clear(); repository.Items.Add(supplierWithOwner);

        var garantir = new FakeGarantirNoErpUseCase { Falha = new ErpFornecedorEscritaException(ErpFornecedorErro.Timeout, "Timeout ao integrar com o ERP.") };
        var result = await CreateAtualizarUseCase(repository, identity, garantir)
            .ExecuteAsync(supplierWithOwner.Id, new AtualizarFornecedorDto("Empresa Atualizada", null, null, null, null, null, null, null, "Ativo", null));

        Assert.NotNull(result);
        Assert.Single(garantir.Chamadas);
        Assert.Equal("Pendente", supplierWithOwner.StatusSincronizacao);
    }

    [Fact]
    public async Task Inativar_Should_Mark_Status_Inativo_Instead_Of_Removing_Supplier()
    {
        // DR-18 (Design Review Pos-Onda 1, P1, BLOQUEIA GATE): "excluir" Fornecedor via
        // DELETE /fornecedores/{id} e semantica de inativacao (AlterarStatus), nunca remocao fisica —
        // nem +Compras nem ERP executam DELETE fisico como operacao funcional.
        var identity = new FakeIdentity(); var repository = new FakeRepository();
        var supplier = new Fornecedor(Guid.NewGuid(), "Empresa", Cnpj.Create("12345678000195"), null, null, null, null, null, null, null, "Ativo", null,
            identity.UserId, DateTimeOffset.UtcNow);
        repository.Items.Add(supplier);

        var result = await new InativarFornecedorUseCase(repository, identity).ExecuteAsync(supplier.Id);

        Assert.True(result);
        Assert.Contains(supplier, repository.Items);
        Assert.Equal("Inativo", supplier.Status);
    }

    [Fact]
    public async Task Inativar_Should_Return_False_When_Supplier_Not_Found()
    {
        var identity = new FakeIdentity(); var repository = new FakeRepository();
        var result = await new InativarFornecedorUseCase(repository, identity).ExecuteAsync(Guid.NewGuid());
        Assert.False(result);
    }

    internal static CadastrarFornecedorUseCase CreateUseCase(IFornecedorRepository repository, ICurrentIdentity identity, IGarantirFornecedorNoErpUseCase? garantirNoErp = null) =>
        new(repository, identity, garantirNoErp ?? new FakeGarantirNoErpUseCase(), new ResolvedorBusinessUnit(new FakeUnidadeNegocioRepository()), NullLogger<CadastrarFornecedorUseCase>.Instance);

    internal static AtualizarFornecedorUseCase CreateAtualizarUseCase(IFornecedorRepository repository, ICurrentIdentity identity, IGarantirFornecedorNoErpUseCase? garantirNoErp = null) =>
        new(repository, identity, garantirNoErp ?? new FakeGarantirNoErpUseCase(), new ResolvedorBusinessUnit(new FakeUnidadeNegocioRepository()), NullLogger<AtualizarFornecedorUseCase>.Instance);

    private static CadastrarFornecedorDto CreateDto() => new("Empresa Ltda", "12.345.678/0001-95", null, null, null, null, null, null, null, "Ativo", null);
    internal sealed class FakeIdentity : ICurrentIdentity { public Guid UserId { get; } = Guid.NewGuid(); public RequestIdentity GetRequired() => new(UserId, "Buyer"); }
    internal sealed class FakeUnidadeNegocioRepository : IUnidadeNegocioRepository
    {
        public Task<UnidadeNegocio?> ObterPorIdAsync(Guid id, CancellationToken ct) => Task.FromResult<UnidadeNegocio?>(null);
        public Task<bool> PossuiAdministradorSeniorAtivoAsync(Guid unidadeNegocioId, CancellationToken ct) => Task.FromResult(false);
        public Task AdicionarAsync(UnidadeNegocio unidadeNegocio, CancellationToken ct) => Task.CompletedTask;
        public Task<IReadOnlyList<UnidadeNegocio>> ListarTodasAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<UnidadeNegocio>>([]);
        public Task<bool> ExisteComSlugAsync(string slug, Guid? excluirId, CancellationToken ct) => Task.FromResult(false);
        public Task SalvarAlteracoesAsync(CancellationToken ct) => Task.CompletedTask;
    }
    internal sealed class FakeGarantirNoErpUseCase : IGarantirFornecedorNoErpUseCase
    {
        public List<(Guid FornecedorId, string BusinessUnit)> Chamadas { get; } = [];
        public Func<GarantirFornecedorErpResultado?>? Resultado { get; set; }
        public ErpFornecedorEscritaException? Falha { get; set; }
        public Task<GarantirFornecedorErpResultado?> ExecuteAsync(Guid fornecedorId, string businessUnit, GarantirFornecedorNoErpDto dto, CancellationToken cancellationToken = default)
        {
            Chamadas.Add((fornecedorId, businessUnit));
            if (Falha is not null) throw Falha;
            return Task.FromResult(Resultado?.Invoke());
        }
    }
    private sealed class FakeRepository : IFornecedorRepository
    {
        public List<Fornecedor> Items { get; } = []; public string? ExistingCnpj { get; set; }
        public Task AdicionarAsync(Fornecedor f, CancellationToken ct = default) { Items.Add(f); return Task.CompletedTask; }
        public Task AtualizarAsync(Fornecedor f, CancellationToken ct = default) => Task.CompletedTask;
        public Task<Fornecedor?> ObterPorIdAsync(Guid id, Guid user, CancellationToken ct = default) => Task.FromResult(Items.SingleOrDefault(x => x.Id == id && x.TemporaryUserId == user));
        public Task<Fornecedor?> ObterPorCnpjAsync(string cnpj, Guid user, CancellationToken ct = default) => Task.FromResult(Items.SingleOrDefault(x => x.Cnpj_Cpf == cnpj && x.TemporaryUserId == user));
        public Task<IReadOnlyList<Fornecedor>> PesquisarAsync(string term, Guid user, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Fornecedor>>(Items.Where(x => x.TemporaryUserId == user).ToArray());
        public Task<IReadOnlyList<Fornecedor>> ListarAsync(Guid user, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Fornecedor>>(Items.Where(x => x.TemporaryUserId == user).ToArray());
        public Task<bool> ExisteAsync(string cnpj, CancellationToken ct = default) => Task.FromResult(ExistingCnpj == cnpj || Items.Any(x => x.Cnpj_Cpf == cnpj));
    }
}
