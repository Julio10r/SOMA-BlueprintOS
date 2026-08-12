using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Application.Procurement.Suppliers;
using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using BlueprintOS.Application.Procurement.Suppliers.Models;
using BlueprintOS.Domain.Procurement.Suppliers;

namespace BlueprintOS.UnitTests.Application.Procurement.Suppliers;

public sealed class FornecedorUseCasesTests
{
    [Fact]
    public async Task Cadastrar_Should_Create_Supplier_With_Current_Temporary_User()
    {
        var identity = new FakeIdentity(); var repository = new FakeRepository();
        var result = await new CadastrarFornecedorUseCase(repository, identity).ExecuteAsync(CreateDto());
        Assert.Equal(identity.UserId, result.TemporaryUserId);
        Assert.Equal("12345678000195", result.Cnpj_Cpf);
    }

    [Fact]
    public async Task Cadastrar_Should_Persist_Cnae_Principal_When_Informed()
    {
        // B2.8: CNAE principal so e persistido na operacao explicita de cadastro (nunca por
        // consulta isolada). Codigo normalizado para digitos puros mesmo se vier mascarado.
        var result = await new CadastrarFornecedorUseCase(new FakeRepository(), new FakeIdentity())
            .ExecuteAsync(CreateDto() with { CnaePrincipalCodigo = "62.01-5/01", CnaePrincipalDescricao = "Desenvolvimento de programas de computador sob encomenda" });

        Assert.Equal("6201501", result.CnaePrincipalCodigo);
        Assert.Equal("Desenvolvimento de programas de computador sob encomenda", result.CnaePrincipalDescricao);
    }

    [Fact]
    public async Task Cadastrar_Should_Succeed_Without_Cnae_Principal()
    {
        // Cadastro manual apos falha do Provider (B2.6): CNAE ausente nunca impede o cadastro.
        var result = await new CadastrarFornecedorUseCase(new FakeRepository(), new FakeIdentity()).ExecuteAsync(CreateDto());

        Assert.Null(result.CnaePrincipalCodigo);
        Assert.Null(result.CnaePrincipalDescricao);
    }

    [Fact]
    public async Task Cadastrar_Should_Reject_Duplicate_Cnpj()
    {
        var repository = new FakeRepository { ExistingCnpj = "12345678000195" };
        await Assert.ThrowsAsync<InvalidOperationException>(() => new CadastrarFornecedorUseCase(repository, new FakeIdentity()).ExecuteAsync(CreateDto()));
    }

    [Theory]
    [InlineData("12345678909", "PF")]
    [InlineData("123.456.789-09", "PF")]
    [InlineData("12345678000195", "PJ")]
    [InlineData("12.345.678/0001-95", "PJ")]
    public async Task Cadastrar_Should_Accept_Valid_Cpf_Or_Cnpj_Masked_Or_Unmasked(string documento, string tipoPessoa)
    {
        var result = await new CadastrarFornecedorUseCase(new FakeRepository(), new FakeIdentity())
            .ExecuteAsync(CreateDto() with { Cnpj_Cpf = documento, TipoPessoa = tipoPessoa });

        var esperado = new string(documento.Where(char.IsDigit).ToArray());
        Assert.Equal(esperado, result.Cnpj_Cpf);
        Assert.Equal(tipoPessoa, result.TipoPessoa);
    }

    [Fact]
    public async Task Cadastrar_Should_Reject_Documento_Fiscal_Above_14_Characters()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => new CadastrarFornecedorUseCase(new FakeRepository(), new FakeIdentity())
            .ExecuteAsync(CreateDto() with { Cnpj_Cpf = "123456789012345" }));
    }

    [Fact]
    public async Task Cadastrar_Should_Reject_Alphanumeric_Documento_Fiscal()
    {
        // BUG-4 (ADR-0023): documento alfanumérico não é mais aceito pelo domínio +Compras — só CPF/CNPJ
        // com dígito verificador válido. Compatibilidade com códigos legados alfanuméricos do Linx
        // pertence exclusivamente a um futuro Adapter Linx (B2.9), nunca ao Value Object canônico.
        await Assert.ThrowsAsync<ArgumentException>(() => new CadastrarFornecedorUseCase(new FakeRepository(), new FakeIdentity())
            .ExecuteAsync(CreateDto() with { Cnpj_Cpf = "AB12345678901", TipoPessoa = "PF" }));
    }

    [Fact]
    public async Task Cadastrar_Should_Reject_Cnpj_With_Invalid_Check_Digit()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => new CadastrarFornecedorUseCase(new FakeRepository(), new FakeIdentity())
            .ExecuteAsync(CreateDto() with { Cnpj_Cpf = "12345678000190" }));
    }

    [Fact]
    public async Task Update_Should_Not_Expose_Supplier_From_Another_User()
    {
        var repository = new FakeRepository();
        var supplier = new Fornecedor(Guid.NewGuid(), "Empresa", Cnpj.Create("12345678000195"), null, null, null, null, null, null, null, "Ativo", null, Guid.NewGuid(), DateTimeOffset.UtcNow);
        repository.Items.Add(supplier);
        var result = await new AtualizarFornecedorUseCase(repository, new FakeIdentity()).ExecuteAsync(supplier.Id, new AtualizarFornecedorDto("Novo", null, null, null, null, null, null, null, null, null));
        Assert.Null(result);
    }

    private static CadastrarFornecedorDto CreateDto() => new("Empresa Ltda", "12.345.678/0001-95", null, null, null, null, null, null, null, "Ativo", null);
    private sealed class FakeIdentity : ICurrentIdentity { public Guid UserId { get; } = Guid.NewGuid(); public RequestIdentity GetRequired() => new(UserId, "Buyer"); }
    private sealed class FakeRepository : IFornecedorRepository
    {
        public List<Fornecedor> Items { get; } = []; public string? ExistingCnpj { get; set; }
        public Task AdicionarAsync(Fornecedor f, CancellationToken ct = default) { Items.Add(f); return Task.CompletedTask; }
        public Task AtualizarAsync(Fornecedor f, CancellationToken ct = default) => Task.CompletedTask;
        public Task ExcluirAsync(Fornecedor f, CancellationToken ct = default) { Items.Remove(f); return Task.CompletedTask; }
        public Task<Fornecedor?> ObterPorIdAsync(Guid id, Guid user, CancellationToken ct = default) => Task.FromResult(Items.SingleOrDefault(x => x.Id == id && x.TemporaryUserId == user));
        public Task<Fornecedor?> ObterPorCnpjAsync(string cnpj, Guid user, CancellationToken ct = default) => Task.FromResult(Items.SingleOrDefault(x => x.Cnpj_Cpf == cnpj && x.TemporaryUserId == user));
        public Task<IReadOnlyList<Fornecedor>> PesquisarAsync(string term, Guid user, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Fornecedor>>(Items.Where(x => x.TemporaryUserId == user).ToArray());
        public Task<IReadOnlyList<Fornecedor>> ListarAsync(Guid user, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Fornecedor>>(Items.Where(x => x.TemporaryUserId == user).ToArray());
        public Task<bool> ExisteAsync(string cnpj, CancellationToken ct = default) => Task.FromResult(ExistingCnpj == cnpj || Items.Any(x => x.Cnpj_Cpf == cnpj));
    }
}
