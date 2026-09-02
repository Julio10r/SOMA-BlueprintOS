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

    /// <summary>Retest do Gate de Fornecedores (2026-09-01), item 4: a mensagem de duplicidade LOCAL (CNPJ
    /// já existe no próprio +Compras) chega ao usuário como está — o controller devolve
    /// <c>ex.Message</c> diretamente no corpo 409 (<c>duplicate_cnpj</c>), sem tradução em outra camada.
    /// Antes desta correção a mensagem era o texto técnico em inglês "Documento fiscal already exists." —
    /// nunca mais expor exception/message técnico do backend diretamente ao usuário.</summary>
    /// <summary>Retest do Gate de Fornecedores (2026-09-01), item 6/12 — prova de que o bypass de frontend
    /// relatado na validação técnica foi fechado: uma chamada direta ao use case (equivalente a chamar
    /// <c>POST /fornecedores</c> sem passar pelo formulário) sem cada campo de endereço/categoria,
    /// individualmente, é rejeitada — nenhuma persistência local, nenhuma tentativa de integração com o
    /// Linx (a checagem acontece antes de qualquer I/O).</summary>
    [Theory]
    [InlineData(null, "01310-100", "Avenida Paulista", "1000", "Bela Vista", "São Paulo", "SP", "BRASIL")] // sem Categoria
    [InlineData("Serviços Gerais", null, "Avenida Paulista", "1000", "Bela Vista", "São Paulo", "SP", "BRASIL")] // sem CEP
    [InlineData("Serviços Gerais", "01310-100", null, "1000", "Bela Vista", "São Paulo", "SP", "BRASIL")] // sem Logradouro
    [InlineData("Serviços Gerais", "01310-100", "Avenida Paulista", null, "Bela Vista", "São Paulo", "SP", "BRASIL")] // sem Número
    [InlineData("Serviços Gerais", "01310-100", "Avenida Paulista", "1000", null, "São Paulo", "SP", "BRASIL")] // sem Bairro
    [InlineData("Serviços Gerais", "01310-100", "Avenida Paulista", "1000", "Bela Vista", null, "SP", "BRASIL")] // sem Cidade
    [InlineData("Serviços Gerais", "01310-100", "Avenida Paulista", "1000", "Bela Vista", "São Paulo", null, "BRASIL")] // sem UF
    [InlineData("Serviços Gerais", "01310-100", "Avenida Paulista", "1000", "Bela Vista", "São Paulo", "SP", null)] // sem País
    public async Task Cadastrar_Should_Reject_Direct_Api_Call_Missing_Required_Address_Field(
        string? categoria, string? cep, string? logradouro, string? numero, string? bairro, string? cidade, string? estado, string? pais)
    {
        var repository = new FakeRepository();
        var garantir = new FakeGarantirNoErpUseCase();
        var dto = CreateDto() with { Categoria = categoria, Cep = cep, Logradouro = logradouro, Numero = numero, Bairro = bairro, Cidade = cidade, Estado = estado, Pais = pais };

        await Assert.ThrowsAsync<ArgumentException>(() => CreateUseCase(repository, new FakeIdentity(), garantir).ExecuteAsync(dto));

        Assert.Empty(repository.Items);
        Assert.Empty(garantir.Chamadas);
    }

    /// <summary>Reteste do Gate de Fornecedores (2026-09-01) — decisão do PO revertida: E-mail e
    /// Telefone agora são obrigatórios no cadastro manual, mesmo bypassando o formulário e chamando o
    /// use case (equivalente a <c>POST /fornecedores</c>) diretamente.</summary>
    [Theory]
    [InlineData(null, "+55 11999998888")] // sem e-mail
    [InlineData("nao-e-um-email", "+55 11999998888")] // e-mail inválido
    [InlineData("contato@empresa.com.br", null)] // sem telefone
    [InlineData("contato@empresa.com.br", "abc")] // telefone inválido
    public async Task Cadastrar_Should_Reject_Direct_Api_Call_Missing_Or_Invalid_Email_Ou_Telefone(string? email, string? telefone)
    {
        var repository = new FakeRepository();
        var garantir = new FakeGarantirNoErpUseCase();
        var dto = CreateDto() with { Email = email, Telefone = telefone };

        await Assert.ThrowsAsync<ArgumentException>(() => CreateUseCase(repository, new FakeIdentity(), garantir).ExecuteAsync(dto));

        Assert.Empty(repository.Items);
        Assert.Empty(garantir.Chamadas);
    }

    [Fact]
    public async Task Cadastrar_Should_Accept_Valid_Email_E_Telefone()
    {
        var repository = new FakeRepository();
        var garantir = new FakeGarantirNoErpUseCase();

        var result = await CreateUseCase(repository, new FakeIdentity(), garantir).ExecuteAsync(CreateDto());

        Assert.Single(repository.Items);
        Assert.Equal("contato@empresa.com.br", result.Email);
    }

    [Fact]
    public async Task Cadastrar_Should_Reject_Duplicate_Cnpj_With_A_Clear_PtBr_Message()
    {
        var repository = new FakeRepository { ExistingCnpj = "12345678000195" };
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => CreateUseCase(repository, new FakeIdentity()).ExecuteAsync(CreateDto()));
        Assert.Equal("Já existe um fornecedor cadastrado com este CNPJ/CPF.", ex.Message);
        Assert.DoesNotContain("already exists", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // Gate de homologação de Fornecedores (2026-09-01): verificação de existência no Linx antes do
    // cadastro — 3 cenários reais documentados (decisão validada do PO,
    // linx-idempotencia-convergencia-create-update-fornecedor).

    [Fact]
    public async Task Cadastrar_Should_Proceed_Normally_When_Cnpj_Nao_Existe_No_Linx()
    {
        var verificar = new FakeVerificarFornecedorNoErpUseCase { Resultado = new(EstadoFornecedorErp.NaoExiste, null) };
        var sincronizar = new FakeSincronizarFornecedorUseCase();

        var result = await CreateUseCase(new FakeRepository(), new FakeIdentity(), verificarNoErp: verificar, sincronizar: sincronizar).ExecuteAsync(CreateDto());

        Assert.Equal("12345678000195", result.Cnpj_Cpf);
        Assert.Single(verificar.Chamadas);
        Assert.Empty(sincronizar.Chamadas); // nunca importa quando não existe no Linx
    }

    [Fact]
    public async Task Cadastrar_Should_Proceed_Normally_When_Cnpj_Existe_Sem_Papel_Fornecedor()
    {
        // CADASTRO_CLI_FOR existe (ex.: já é Cliente/Filial) mas sem o papel Fornecedor — não
        // bloqueia o cadastro local; GarantirNoErpSemFalharCadastroAsync (fluxo já existente) é
        // quem adiciona o papel Fornecedor ao cadastro-base, preservando os papéis existentes.
        var verificar = new FakeVerificarFornecedorNoErpUseCase { Resultado = new(EstadoFornecedorErp.ExisteSemPapelFornecedor, "001234") };
        var sincronizar = new FakeSincronizarFornecedorUseCase();

        var result = await CreateUseCase(new FakeRepository(), new FakeIdentity(), verificarNoErp: verificar, sincronizar: sincronizar).ExecuteAsync(CreateDto());

        Assert.Equal("12345678000195", result.Cnpj_Cpf);
        Assert.Empty(sincronizar.Chamadas); // não importa — o cadastro local prossegue normalmente
    }

    [Fact]
    public async Task Cadastrar_Should_Not_Duplicate_When_Cnpj_Ja_Existe_Como_Fornecedor_No_Linx()
    {
        // Já existe com o papel Fornecedor confirmado: nunca cria um novo registro local — importa
        // o existente do ERP (mesma engine de "Atualizar do ERP") e sinaliza ao chamador via exceção
        // tipada, nunca lançando um Fornecedor duplicado.
        var verificar = new FakeVerificarFornecedorNoErpUseCase { Resultado = new(EstadoFornecedorErp.ExisteComPapelFornecedor, "005678") };
        var fornecedorIdImportado = Guid.NewGuid();
        var sincronizar = new FakeSincronizarFornecedorUseCase
        {
            Resultado = dto => new SincronizacaoFornecedorResultado(fornecedorIdImportado, dto.BusinessUnit, dto.ErpSistema, dto.ErpFornecedorId, "Sincronizado", "corr", DateTimeOffset.UtcNow, null)
        };
        var repository = new FakeRepository();

        var ex = await Assert.ThrowsAsync<FornecedorJaExisteNoErpException>(
            () => CreateUseCase(repository, new FakeIdentity(), verificarNoErp: verificar, sincronizar: sincronizar).ExecuteAsync(CreateDto()));

        Assert.Equal(fornecedorIdImportado, ex.FornecedorId);
        Assert.Single(sincronizar.Chamadas);
        Assert.Equal(DirecaoSincronizacao.ErpParaMaisCompras, sincronizar.Chamadas[0].Direcao);
        Assert.Equal("005678", sincronizar.Chamadas[0].ErpFornecedorId);
        Assert.Empty(repository.Items); // nunca cria um Fornecedor local duplicado neste caminho
    }

    [Fact]
    public async Task Cadastrar_Should_Converge_When_Concurrent_Request_Created_Same_Cnpj()
    {
        // Concorrência real (índice único de Cnpj_Cpf): a segunda requisição, ao colidir no INSERT,
        // converge para o registro já criado pela primeira em vez de falhar com 500 (decisão do PO).
        var identity = new FakeIdentity();
        var jaCriadoPelaOutraRequisicao = new Fornecedor(Guid.NewGuid(), "Empresa Ltda", Cnpj.Create("12345678000195"), null, null, null, null, null,
            null, null, "Ativo", null, identity.UserId, DateTimeOffset.UtcNow);
        var repository = new ConcurrentDuplicateFakeRepository(jaCriadoPelaOutraRequisicao);

        var result = await CreateUseCase(repository, identity).ExecuteAsync(CreateDto());

        Assert.Equal("12345678000195", result.Cnpj_Cpf);
        Assert.Equal(jaCriadoPelaOutraRequisicao.Id, result.Id);
    }

    /// <summary>Simula a corrida real: <see cref="IFornecedorRepository.AdicionarAsync"/> lança
    /// <see cref="DuplicateRecordException"/> (mesma tradução que <c>FornecedorRepository</c> faz a
    /// partir de uma violação de índice único do SQL Server), e a reconsulta subsequente encontra o
    /// registro já criado pela requisição concorrente.</summary>
    private sealed class ConcurrentDuplicateFakeRepository(Fornecedor concorrente) : IFornecedorRepository
    {
        public Task AdicionarAsync(Fornecedor fornecedor, CancellationToken ct = default) =>
            throw new DuplicateRecordException("Documento fiscal já foi cadastrado por outra requisição concorrente.");
        public Task AtualizarAsync(Fornecedor fornecedor, CancellationToken ct = default) => Task.CompletedTask;
        public Task<Fornecedor?> ObterPorIdAsync(Guid id, Guid user, CancellationToken ct = default) => Task.FromResult<Fornecedor?>(null);
        public Task<Fornecedor?> ObterPorCnpjAsync(string cnpj, Guid user, CancellationToken ct = default) =>
            Task.FromResult(cnpj == concorrente.Cnpj_Cpf && user == concorrente.TemporaryUserId ? concorrente : null);
        public Task<IReadOnlyList<Fornecedor>> PesquisarAsync(string termo, Guid user, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Fornecedor>>([]);
        public Task<IReadOnlyList<Fornecedor>> ListarAsync(Guid user, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Fornecedor>>([]);
        public Task<bool> ExisteAsync(string cnpj, CancellationToken ct = default) => Task.FromResult(false);
        public Task<Fornecedor?> ObterPorCnpjSemRastreamentoAsync(string cnpj, Guid user, CancellationToken ct = default) => ObterPorCnpjAsync(cnpj, user, ct);
        public Task<int> ContarAtivosAsync(Guid user, CancellationToken ct = default) => Task.FromResult(0);
        public Task<FornecedorPesquisaPaginadaResultado> PesquisarPaginadoAsync(Guid temporaryUserId, string? termo,
            FornecedorStatusFiltro status, FornecedorOrdenacaoCampo ordenarPor, bool ordenarDescendente,
            int page, int pageSize, CancellationToken ct = default) =>
            Task.FromResult(new FornecedorPesquisaPaginadaResultado([], 0, page, pageSize));
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

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Cadastrar_Should_Reject_Missing_NomeFantasia(string? nomeFantasia)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => CreateUseCase(new FakeRepository(), new FakeIdentity())
            .ExecuteAsync(CreateDto() with { NomeFantasia = nomeFantasia }));
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
        //
        // Regra de inativação (2026-09-02, decisão do PO — assimétrica, BY DESIGN): +Compras NÃO tem
        // autoridade para inativar fornecedor no Linx. InativarFornecedorUseCase, por isso, nunca recebe
        // um adapter/use case de ERP como dependência — é estruturalmente incapaz de propagar ao ERP, não
        // apenas "não propaga por enquanto". O sentido inverso (Linx → +Compras) é coberto por
        // Import_Should_Reflect_Erp_Inactivation_Onto_Local_Fornecedor em SincronizarFornecedorUseCaseTests.
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

    [Fact]
    public async Task AlterarStatus_Should_Activate_And_Inactivate_Without_Removing_Supplier()
    {
        // Rota semantica PATCH /fornecedores/{id}/status: precisa funcionar nos dois sentidos
        // (ativar e inativar), sempre via AlterarStatus, nunca removendo a linha (DR-18).
        var identity = new FakeIdentity(); var repository = new FakeRepository();
        var supplier = new Fornecedor(Guid.NewGuid(), "Empresa", Cnpj.Create("12345678000195"), null, null, null, null, null, null, null, "Ativo", null,
            identity.UserId, DateTimeOffset.UtcNow);
        repository.Items.Add(supplier);
        var useCase = new AlterarStatusFornecedorUseCase(repository, identity);

        var inativado = await useCase.ExecuteAsync(supplier.Id, ativo: false);
        Assert.NotNull(inativado);
        Assert.Equal("Inativo", inativado!.Status);
        Assert.Contains(supplier, repository.Items);

        var reativado = await useCase.ExecuteAsync(supplier.Id, ativo: true);
        Assert.NotNull(reativado);
        Assert.Equal("Ativo", reativado!.Status);
        Assert.Contains(supplier, repository.Items);
    }

    [Fact]
    public async Task AlterarStatus_Should_Return_Null_When_Supplier_Not_Found()
    {
        var identity = new FakeIdentity(); var repository = new FakeRepository();
        var result = await new AlterarStatusFornecedorUseCase(repository, identity).ExecuteAsync(Guid.NewGuid(), true);
        Assert.Null(result);
    }

    [Fact]
    public async Task PesquisarPaginado_Should_Return_Correct_Total_And_Page()
    {
        var identity = new FakeIdentity(); var repository = new FakeRepository();
        var cnpjsValidos = new[] { "12345678000195", "11444777000161" };
        for (var i = 0; i < 25; i++)
            repository.Items.Add(new Fornecedor(Guid.NewGuid(), $"Empresa {i:00}", Cnpj.Create(cnpjsValidos[i % cnpjsValidos.Length]),
                null, null, null, null, null, null, null, "Ativo", null, identity.UserId, DateTimeOffset.UtcNow));
        var useCase = new PesquisarFornecedorPaginadoUseCase(repository, identity);

        var pagina1 = await useCase.ExecuteAsync(new PesquisarFornecedorPaginadoParametros(null, null, null, Page: 1, PageSize: 10));
        Assert.Equal(25, pagina1.TotalCount);
        Assert.Equal(10, pagina1.Items.Count);
        Assert.Equal(1, pagina1.Page);

        var pagina3 = await useCase.ExecuteAsync(new PesquisarFornecedorPaginadoParametros(null, null, null, Page: 3, PageSize: 10));
        Assert.Equal(5, pagina3.Items.Count);
    }

    [Fact]
    public async Task PesquisarPaginado_Should_Filter_By_Status()
    {
        var identity = new FakeIdentity(); var repository = new FakeRepository();
        var ativo = new Fornecedor(Guid.NewGuid(), "Ativa Ltda", Cnpj.Create("12345678000195"), null, null, null, null, null, null, null, "Ativo", null, identity.UserId, DateTimeOffset.UtcNow);
        var inativo = new Fornecedor(Guid.NewGuid(), "Inativa Ltda", Cnpj.Create("11444777000161"), null, null, null, null, null, null, null, "Ativo", null, identity.UserId, DateTimeOffset.UtcNow);
        inativo.AlterarStatus(false, DateTimeOffset.UtcNow, "MaisCompras");
        repository.Items.Add(ativo); repository.Items.Add(inativo);
        var useCase = new PesquisarFornecedorPaginadoUseCase(repository, identity);

        var somenteAtivos = await useCase.ExecuteAsync(new PesquisarFornecedorPaginadoParametros(null, "Ativo", null));
        Assert.Single(somenteAtivos.Items);
        Assert.Equal("Ativa Ltda", somenteAtivos.Items[0].Nome);

        var somenteInativos = await useCase.ExecuteAsync(new PesquisarFornecedorPaginadoParametros(null, "Inativo", null));
        Assert.Single(somenteInativos.Items);
        Assert.Equal("Inativa Ltda", somenteInativos.Items[0].Nome);

        var todos = await useCase.ExecuteAsync(new PesquisarFornecedorPaginadoParametros(null, "Todos", null));
        Assert.Equal(2, todos.TotalCount);
    }

    [Fact]
    public async Task PesquisarPaginado_Should_Match_Partial_Name()
    {
        var identity = new FakeIdentity(); var repository = new FakeRepository();
        repository.Items.Add(new Fornecedor(Guid.NewGuid(), "Alpha Suprimentos", Cnpj.Create("12345678000195"), null, null, null, null, null, null, null, "Ativo", null, identity.UserId, DateTimeOffset.UtcNow));
        repository.Items.Add(new Fornecedor(Guid.NewGuid(), "Beta Comercio", Cnpj.Create("11444777000161"), null, null, null, null, null, null, null, "Ativo", null, identity.UserId, DateTimeOffset.UtcNow));
        var useCase = new PesquisarFornecedorPaginadoUseCase(repository, identity);

        var resultado = await useCase.ExecuteAsync(new PesquisarFornecedorPaginadoParametros("Alpha", null, null));
        Assert.Single(resultado.Items);
        Assert.Equal("Alpha Suprimentos", resultado.Items[0].Nome);
    }

    [Fact]
    public async Task PesquisarPaginado_Should_Expose_Real_StatusSincronizacao_Not_Inferred()
    {
        // O frontend nao deve mais inferir "Sincronizado"/"Nao sincronizado" a partir de
        // ErpFornecedorId != null — o DTO paginado precisa expor o StatusSincronizacao real do
        // Fornecedor (Pendente/Sincronizado/Falhou), incluindo UltimaSincronizacaoEm e
        // MensagemErroSincronizacao.
        var identity = new FakeIdentity(); var repository = new FakeRepository();
        var fornecedor = new Fornecedor(Guid.NewGuid(), "Gama Distribuidora", Cnpj.Create("12345678000195"), null, null, null,
            null, null, null, null, "Ativo", null, identity.UserId, DateTimeOffset.UtcNow);
        var agora = DateTimeOffset.UtcNow;
        fornecedor.RegistrarSincronizacao("Falhou", agora, "Timeout ao chamar o ERP.");
        repository.Items.Add(fornecedor);
        var useCase = new PesquisarFornecedorPaginadoUseCase(repository, identity);

        var resultado = await useCase.ExecuteAsync(new PesquisarFornecedorPaginadoParametros(null, null, null));

        var dto = Assert.Single(resultado.Items);
        Assert.Equal("Falhou", dto.StatusSincronizacao);
        Assert.Equal(agora, dto.UltimaSincronizacaoEm);
        Assert.Equal("Timeout ao chamar o ERP.", dto.MensagemErroSincronizacao);
    }

    [Fact]
    public async Task PesquisarPaginado_Should_Return_Zero_Results_When_No_Match()
    {
        var identity = new FakeIdentity(); var repository = new FakeRepository();
        repository.Items.Add(new Fornecedor(Guid.NewGuid(), "Alpha Suprimentos", Cnpj.Create("12345678000195"), null, null, null, null, null, null, null, "Ativo", null, identity.UserId, DateTimeOffset.UtcNow));
        var useCase = new PesquisarFornecedorPaginadoUseCase(repository, identity);

        var resultado = await useCase.ExecuteAsync(new PesquisarFornecedorPaginadoParametros("Inexistente", null, null));
        Assert.Empty(resultado.Items);
        Assert.Equal(0, resultado.TotalCount);
    }

    internal static CadastrarFornecedorUseCase CreateUseCase(IFornecedorRepository repository, ICurrentIdentity identity, IGarantirFornecedorNoErpUseCase? garantirNoErp = null,
        IVerificarFornecedorNoErpUseCase? verificarNoErp = null, ISincronizarFornecedorUseCase? sincronizar = null) =>
        new(repository, identity, garantirNoErp ?? new FakeGarantirNoErpUseCase(), verificarNoErp ?? new FakeVerificarFornecedorNoErpUseCase(),
            sincronizar ?? new FakeSincronizarFornecedorUseCase(), new ResolvedorBusinessUnit(new FakeUnidadeNegocioRepository()), NullLogger<CadastrarFornecedorUseCase>.Instance);

    internal static AtualizarFornecedorUseCase CreateAtualizarUseCase(IFornecedorRepository repository, ICurrentIdentity identity, IGarantirFornecedorNoErpUseCase? garantirNoErp = null) =>
        new(repository, identity, garantirNoErp ?? new FakeGarantirNoErpUseCase(), new ResolvedorBusinessUnit(new FakeUnidadeNegocioRepository()), NullLogger<AtualizarFornecedorUseCase>.Instance);

    // Retest do Gate de Fornecedores (2026-09-01), item 6: Categoria/CEP/Logradouro/Número/Bairro/
    // Cidade/UF/País agora são obrigatórios em CadastrarFornecedorUseCase — todo teste que não foca
    // especificamente nesses campos precisa de um DTO já válido para não quebrar por um motivo
    // não relacionado ao que está sendo testado.
    private static CadastrarFornecedorDto CreateDto() => new("Empresa Ltda", "12.345.678/0001-95", "Serviços Gerais", "contato@empresa.com.br", "+55 11999998888", null, "São Paulo", "SP", "BRASIL", "Ativo", null,
        NomeFantasia: "Empresa Fantasia", Cep: "01310-100", Logradouro: "Avenida Paulista", Numero: "1000", Bairro: "Bela Vista");
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
    /// <summary>Por padrão simula "não existe no Linx" — o comportamento anterior à existência
    /// desta verificação (nenhum teste que não a exercite explicitamente é afetado).</summary>
    internal sealed class FakeVerificarFornecedorNoErpUseCase : IVerificarFornecedorNoErpUseCase
    {
        public List<(string BusinessUnit, string DocumentoFiscal)> Chamadas { get; } = [];
        public VerificacaoFornecedorErpResultado Resultado { get; set; } = new(EstadoFornecedorErp.NaoExiste, null);
        public Task<VerificacaoFornecedorErpResultado> ExecuteAsync(string businessUnit, string documentoFiscal, CancellationToken cancellationToken = default)
        {
            Chamadas.Add((businessUnit, documentoFiscal));
            return Task.FromResult(Resultado);
        }
    }
    internal sealed class FakeSincronizarFornecedorUseCase : ISincronizarFornecedorUseCase
    {
        public List<SincronizarFornecedorDto> Chamadas { get; } = [];
        public Func<SincronizarFornecedorDto, SincronizacaoFornecedorResultado>? Resultado { get; set; }
        public Task<SincronizacaoFornecedorResultado> ExecuteAsync(SincronizarFornecedorDto dto, CancellationToken cancellationToken = default)
        {
            Chamadas.Add(dto);
            var resultado = Resultado?.Invoke(dto) ?? new SincronizacaoFornecedorResultado(Guid.NewGuid(), dto.BusinessUnit, dto.ErpSistema, dto.ErpFornecedorId, "Sincronizado", "corr", DateTimeOffset.UtcNow, null);
            return Task.FromResult(resultado);
        }
        public Task<IReadOnlyList<SincronizacaoFornecedorResultado>> ExecutarLoteAsync(SincronizarFornecedoresLoteDto dto, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SincronizacaoFornecedorResultado>>([]);
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
        public Task<Fornecedor?> ObterPorCnpjSemRastreamentoAsync(string cnpj, Guid user, CancellationToken ct = default) => ObterPorCnpjAsync(cnpj, user, ct);
        public Task<int> ContarAtivosAsync(Guid user, CancellationToken ct = default) => Task.FromResult(Items.Count(x => x.TemporaryUserId == user && x.Status == "Ativo"));
        public Task<FornecedorPesquisaPaginadaResultado> PesquisarPaginadoAsync(Guid temporaryUserId, string? termo,
            FornecedorStatusFiltro status, FornecedorOrdenacaoCampo ordenarPor, bool ordenarDescendente,
            int page, int pageSize, CancellationToken ct = default)
        {
            IEnumerable<Fornecedor> query = Items.Where(x => x.TemporaryUserId == temporaryUserId);
            if (!string.IsNullOrWhiteSpace(termo))
                query = query.Where(x => x.RazaoSocial.Contains(termo, StringComparison.OrdinalIgnoreCase) || x.Cnpj_Cpf.Contains(termo));
            query = status switch
            {
                FornecedorStatusFiltro.Ativo => query.Where(x => x.Status == "Ativo"),
                FornecedorStatusFiltro.Inativo => query.Where(x => x.Status == "Inativo"),
                _ => query,
            };
            query = ordenarPor switch
            {
                FornecedorOrdenacaoCampo.Cnpj => ordenarDescendente ? query.OrderByDescending(x => x.Cnpj_Cpf) : query.OrderBy(x => x.Cnpj_Cpf),
                FornecedorOrdenacaoCampo.Status => ordenarDescendente ? query.OrderByDescending(x => x.Status) : query.OrderBy(x => x.Status),
                FornecedorOrdenacaoCampo.CreatedAt => ordenarDescendente ? query.OrderByDescending(x => x.CreatedAt) : query.OrderBy(x => x.CreatedAt),
                _ => ordenarDescendente ? query.OrderByDescending(x => x.RazaoSocial) : query.OrderBy(x => x.RazaoSocial),
            };
            var list = query.ToList();
            var items = list.Skip((page - 1) * pageSize).Take(pageSize).ToArray();
            return Task.FromResult(new FornecedorPesquisaPaginadaResultado(items, list.Count, page, pageSize));
        }
    }
}
