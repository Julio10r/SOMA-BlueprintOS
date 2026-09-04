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

/// <summary>B3 — Bloco 5A.9: modelo "1 CNPJ/CPF = 1 Fornecedor +Compras, N vínculos Linx" (GAPs KALUNGA —
/// sincronização travada — e PLATINUM — colisão de CNPJ). Cobre domínio (FornecedorLinxVinculo), o
/// algoritmo de fonte cadastral/Principal do sincronizador, o backfill de migração e a recuperação
/// administrativa governada de execução abandonada.</summary>
public sealed class FornecedorLinxVinculoModelTests
{
    private static readonly DateTimeOffset T1 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset T2 = new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid Bu = Guid.NewGuid();

    // ===================== A/B — Domínio: modelo e regra de atividade =====================

    [Theory]
    [InlineData(false, false, true)]   // ambos ativos -> vínculo ativo
    [InlineData(true, false, false)]   // CADASTRO_CLI_FOR inativo (master) -> vínculo inativo
    [InlineData(false, true, false)]   // FORNECEDORES inativo -> vínculo inativo
    [InlineData(true, true, false)]    // ambos inativos -> vínculo inativo
    public void Vinculo_Ativo_Deve_Exigir_Ambas_As_Tabelas_Concordarem(bool inativoCadastroCliFor, bool inativoFornecedores, bool esperadoAtivo)
    {
        var vinculo = new FornecedorLinxVinculo(Guid.NewGuid(), Bu, "SOMA_DESENV", "001", "FORNECEDOR X",
            inativoFornecedores, inativoCadastroCliFor, T1, principal: false, agora: T1);

        Assert.Equal(esperadoAtivo, vinculo.Ativo);
    }

    [Fact]
    public void Vinculo_Inativo_Nao_Pode_Ser_Definido_Como_Principal()
    {
        var vinculo = new FornecedorLinxVinculo(Guid.NewGuid(), Bu, "SOMA_DESENV", "001", "FORNECEDOR X",
            inativoFornecedores: true, inativoCadastroCliFor: false, T1, principal: false, agora: T1);

        Assert.Throws<InvalidOperationException>(() => vinculo.DefinirComoPrincipal(T2));
    }

    [Fact]
    public void Vinculo_Nao_Pode_Nascer_Principal_Se_Inativo()
    {
        Assert.Throws<ArgumentException>(() => new FornecedorLinxVinculo(Guid.NewGuid(), Bu, "SOMA_DESENV", "001", "FORNECEDOR X",
            inativoFornecedores: true, inativoCadastroCliFor: false, T1, principal: true, agora: T1));
    }

    // ===================== C/D — Sincronização: fonte cadastral, Principal, empate =====================

    [Fact]
    public async Task Sincronizacao_Deve_Preservar_Ambos_Vinculos_Do_Mesmo_Cnpj_Sem_Criar_Segundo_Fornecedor()
    {
        await using var context = NewContext();
        var identity = new FakeIdentity();
        var reader = new FakeReader(
            new("001", "SOMA_DESENV", Canonical("Fornecedor A", "Fantasia A", "12345678000195"), T1, false),
            new("002", "SOMA_DESENV", Canonical("Fornecedor A", "Fantasia A", "12345678000195"), T1, false));

        var result = await Create(context, identity, reader).ExecuteAsync(new("BU-A", 100, null));

        Assert.Equal(1, await context.Fornecedores.CountAsync());
        Assert.Equal(2, await context.FornecedorLinxVinculos.CountAsync());
        Assert.Equal(1, result.Incluidos);
    }

    [Fact]
    public async Task Fonte_Cadastral_Deve_Ser_O_Vinculo_Ativo_Mais_Recente_Mesmo_Quando_Nao_E_Principal()
    {
        // Exemplo homologado (§4-5): CLIFOR 001 é Principal com dado mais antigo; CLIFOR 002 chega depois,
        // não-Principal, com DATA_PARA_TRANSFERENCIA mais recente — os dados cadastrais devem vir de 002,
        // mas 001 continua Principal.
        await using var context = NewContext();
        var identity = new FakeIdentity();
        var reader1 = new FakeReader(new FornecedorErpIntegracaoDto("001", "SOMA_DESENV", Canonical("Fornecedor Antigo", "Fantasia Antiga", "12345678000195"), T1, false));
        await Create(context, identity, reader1).ExecuteAsync(new("BU-A", 100, null));

        var fornecedorId = (await context.Fornecedores.SingleAsync()).Id;
        Assert.True((await context.FornecedorLinxVinculos.SingleAsync(v => v.CodigoErp == "001")).Principal);

        var reader2 = new FakeReader(new FornecedorErpIntegracaoDto("002", "SOMA_DESENV", Canonical("Fornecedor Novo", "Fantasia Nova", "12345678000195"), T2, false));
        await Create(context, identity, reader2).ExecuteAsync(new("BU-A", 100, null));

        var stored = await context.Fornecedores.SingleAsync(x => x.Id == fornecedorId);
        var vinculo001 = await context.FornecedorLinxVinculos.SingleAsync(v => v.CodigoErp == "001");
        var vinculo002 = await context.FornecedorLinxVinculos.SingleAsync(v => v.CodigoErp == "002");

        Assert.Equal("Fornecedor Novo", stored.RazaoSocial);
        Assert.True(vinculo001.Principal);
        Assert.False(vinculo002.Principal);
    }

    [Fact]
    public async Task Vinculo_Inativo_Mais_Recente_Nao_Deve_Fornecer_Dados_Cadastrais()
    {
        await using var context = NewContext();
        var identity = new FakeIdentity();
        var reader1 = new FakeReader(new FornecedorErpIntegracaoDto("001", "SOMA_DESENV", Canonical("Fornecedor Ativo", "Fantasia Ativa", "12345678000195"), T1, false));
        await Create(context, identity, reader1).ExecuteAsync(new("BU-A", 100, null));

        // CLIFOR 002, mesmo CNPJ, timestamp mais recente, porém INATIVO — não deve virar fonte cadastral.
        var inativo = Canonical("Fornecedor Inativo Mais Recente", "Fantasia Inativa", "12345678000195") with { Ativo = false };
        var reader2 = new FakeReader(new FornecedorErpIntegracaoDto("002", "SOMA_DESENV", inativo, T2, false));
        await Create(context, identity, reader2).ExecuteAsync(new("BU-A", 100, null));

        var stored = await context.Fornecedores.SingleAsync();
        Assert.Equal("Fornecedor Ativo", stored.RazaoSocial);
        Assert.Equal("Ativo", stored.Status);
    }

    [Fact]
    public async Task Todos_Vinculos_Ficando_Inativos_Deve_Inativar_Fornecedor()
    {
        await using var context = NewContext();
        var identity = new FakeIdentity();
        // Padding: 3 fornecedores Ativos que nunca mudam, para a inativação de 1 (1/4 = 25%) ficar abaixo
        // do limiar de 30% da guarda de inativação em massa (testada isoladamente em
        // SincronizarFornecedoresErpUseCaseTests.Execute_Should_Abort_Inactivations_When_Percentage_Is_Abnormal).
        var reader1 = new FakeReader(
            new FornecedorErpIntegracaoDto("001", "SOMA_DESENV", Canonical("Fornecedor Unico", "Fantasia", "12345678000195"), T1, false),
            new FornecedorErpIntegracaoDto("P1", "SOMA_DESENV", Canonical("Padding 1", "Padding 1", "11222333000181"), T1, false),
            new FornecedorErpIntegracaoDto("P2", "SOMA_DESENV", Canonical("Padding 2", "Padding 2", "99888777000100"), T1, false),
            new FornecedorErpIntegracaoDto("P3", "SOMA_DESENV", Canonical("Padding 3", "Padding 3", "22333444000181"), T1, false));
        await Create(context, identity, reader1).ExecuteAsync(new("BU-A", 100, null));
        Assert.Equal("Ativo", (await context.Fornecedores.SingleAsync(f => f.Cnpj_Cpf == DocumentoFiscal.Create("12345678000195").Value)).Status);

        var inativo = Canonical("Fornecedor Unico", "Fantasia", "12345678000195") with { Ativo = false };
        var reader2 = new FakeReader(new FornecedorErpIntegracaoDto("001", "SOMA_DESENV", inativo, T2, false));
        var result = await Create(context, identity, reader2).ExecuteAsync(new("BU-A", 100, null));

        Assert.Equal("Inativo", (await context.Fornecedores.SingleAsync(f => f.Cnpj_Cpf == DocumentoFiscal.Create("12345678000195").Value)).Status);
        Assert.Equal(1, result.Atualizados);
    }

    [Fact]
    public async Task Novo_Vinculo_Nao_Deve_Substituir_Principal_Ja_Existente()
    {
        await using var context = NewContext();
        var identity = new FakeIdentity();
        var reader1 = new FakeReader(new FornecedorErpIntegracaoDto("001", "SOMA_DESENV", Canonical("Fornecedor", "Fantasia", "12345678000195"), T1, false));
        await Create(context, identity, reader1).ExecuteAsync(new("BU-A", 100, null));

        var reader2 = new FakeReader(new FornecedorErpIntegracaoDto("002", "SOMA_DESENV", Canonical("Fornecedor", "Fantasia", "12345678000195"), T2, false));
        await Create(context, identity, reader2).ExecuteAsync(new("BU-A", 100, null));

        Assert.True((await context.FornecedorLinxVinculos.SingleAsync(v => v.CodigoErp == "001")).Principal);
        Assert.False((await context.FornecedorLinxVinculos.SingleAsync(v => v.CodigoErp == "002")).Principal);
    }

    [Fact]
    public async Task Empate_Na_Definicao_Automatica_De_Principal_Nao_Deve_Inventar_Desempate()
    {
        // Um Fornecedor já com 2 vínculos ATIVOS empatados no maior DATA_PARA_TRANSFERENCIA e NENHUM
        // Principal (ex.: resultado de um `RemoverComoPrincipal` anterior) — a atribuição automática nunca
        // é reavaliada por um algoritmo per-row-sequencial na primeira vez que cada vínculo é criado (o
        // primeiro sempre vira Principal trivialmente, sozinho); um empate real só existe quando os dois
        // candidatos JÁ coexistem sem Principal no momento em que uma nova linha do Linx é processada.
        await using var context = NewContext();
        var identity = new FakeIdentity();
        var fornecedor = new Fornecedor(Guid.NewGuid(), "Fornecedor A", DocumentoFiscal.Create("12345678000195"), "PJ", null, null, null,
            null, null, null, null, "Ativo", null, T1, identity.UnidadeNegocioId);
        await new FornecedorRepository(context).AdicionarAsync(fornecedor);
        var vinculoRepo = new FornecedorLinxVinculoRepository(context);
        await vinculoRepo.AdicionarAsync(new FornecedorLinxVinculo(fornecedor.Id, identity.UnidadeNegocioId, "SOMA_DESENV", "001", "FORNECEDOR A", false, false, T1, principal: false, agora: T1));
        await vinculoRepo.AdicionarAsync(new FornecedorLinxVinculo(fornecedor.Id, identity.UnidadeNegocioId, "SOMA_DESENV", "002", "FORNECEDOR A", false, false, T1, principal: false, agora: T1));
        await vinculoRepo.SalvarAlteracoesAsync();

        // Uma nova sincronização processa o vínculo "001" (sem alteração de dados) — o caso de uso
        // reavalia a definição de Principal (nenhum existe) e encontra os 2 candidatos ativos empatados.
        var reader = new FakeReader(new FornecedorErpIntegracaoDto("001", "SOMA_DESENV", Canonical("Fornecedor A", "FORNECEDOR A", "12345678000195") with { DataUltimaAlteracao = T1 }, T1, false));
        var result = await Create(context, identity, reader).ExecuteAsync(new("BU-A", 100, null));

        Assert.DoesNotContain(await context.FornecedorLinxVinculos.ToListAsync(), v => v.Principal);
        Assert.NotEmpty(result.OcorrenciasVinculos ?? []);
    }

    // ===================== GAP KALUNGA — falha fatal nunca deixa execução presa =====================

    [Fact]
    public async Task Falha_Fatal_Fora_Do_Tratamento_Por_Registro_Deve_Finalizar_Execucao_Com_Status_Terminal()
    {
        await using var context = NewContext();
        var identity = new FakeIdentity();
        var reader = new ThrowingReader();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Create(context, identity, reader).ExecuteAsync(new("BU-A", 100, null)));

        var execucao = await context.SincronizacoesFornecedores.SingleAsync();
        Assert.Equal("AbortadoPorFalhaFatal", execucao.Status);
        Assert.NotNull(execucao.DataFim);
        Assert.Contains("falha simulada", execucao.JustificativaEncerramento, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Execucao_Travada_Deve_Bloquear_Nova_Execucao_Ate_Recuperacao_Administrativa()
    {
        await using var context = NewContext();
        var identity = new FakeIdentity();
        var travada = new SincronizacaoFornecedor(Guid.NewGuid(), "SOMA_DESENV", "BU-A", DateTimeOffset.UtcNow, identity.UnidadeNegocioId);
        travada.MarcarEmAndamento();
        await context.SincronizacoesFornecedores.AddAsync(travada);
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Create(context, identity, new FakeReader()).ExecuteAsync(new("BU-A", 100, null)));

        var recuperar = new RecuperarSincronizacaoFornecedorAbandonadaUseCase(context, identity, NullLogger<RecuperarSincronizacaoFornecedorAbandonadaUseCase>.Instance);
        var resumo = await recuperar.ExecuteAsync(new RecuperarSincronizacaoFornecedorAbandonadaDto(travada.Id, "Recuperação administrativa — investigação B3/5A confirmou abandono."));

        Assert.Equal("EmAndamento", resumo.StatusAnterior);
        Assert.Equal("AbortadoRecuperacaoAdministrativa", resumo.StatusFinal);

        var reader = new FakeReader(new FornecedorErpIntegracaoDto("001", "SOMA_DESENV", Canonical("Fornecedor", "Fantasia", "12345678000195"), T1, false));
        var result = await Create(context, identity, reader).ExecuteAsync(new("BU-A", 100, null));
        Assert.Equal("Sucesso", result.Status);
    }

    [Fact]
    public async Task Recuperacao_Administrativa_Deve_Exigir_Justificativa_E_Execucao_Realmente_Travada()
    {
        await using var context = NewContext();
        var identity = new FakeIdentity();
        var recuperar = new RecuperarSincronizacaoFornecedorAbandonadaUseCase(context, identity, NullLogger<RecuperarSincronizacaoFornecedorAbandonadaUseCase>.Instance);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            recuperar.ExecuteAsync(new RecuperarSincronizacaoFornecedorAbandonadaDto(Guid.NewGuid(), "")));

        var concluida = new SincronizacaoFornecedor(Guid.NewGuid(), "SOMA_DESENV", "BU-A", DateTimeOffset.UtcNow, identity.UnidadeNegocioId);
        concluida.Finalizar(DateTimeOffset.UtcNow);
        await context.SincronizacoesFornecedores.AddAsync(concluida);
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            recuperar.ExecuteAsync(new RecuperarSincronizacaoFornecedorAbandonadaDto(concluida.Id, "Justificativa válida")));
    }

    // ===================== E — Backfill de migração =====================

    [Fact]
    public async Task Backfill_DryRun_Deve_Classificar_Sem_Persistir()
    {
        await using var context = NewContext();
        var fornecedor = new Fornecedor(Guid.NewGuid(), "Fornecedor Legado", DocumentoFiscal.Create("12345678000195"), "PJ", null, null, null,
            null, null, null, null, "Ativo", null, DateTimeOffset.UtcNow, Bu, businessUnit: null, erpSistema: "SOMA_DESENV", erpFornecedorId: "ERP-LEGADO");
        await new FornecedorRepository(context).AdicionarAsync(fornecedor);

        var useCase = new BackfillFornecedorLinxVinculosUseCase(context, NullLogger<BackfillFornecedorLinxVinculosUseCase>.Instance);
        var resumo = await useCase.ExecuteAsync(new BackfillFornecedorLinxVinculosDto(DryRun: true));

        Assert.Equal("DryRunConcluido", resumo.Status);
        Assert.Equal(1, resumo.FornecedoresComIdentidadeErpLegada);
        Assert.Equal(1, resumo.VinculosCriados);
        Assert.Equal(0, await context.FornecedorLinxVinculos.CountAsync());
    }

    [Fact]
    public async Task Backfill_Real_Deve_Criar_Vinculo_Principal_Preservando_Identidade_Legada()
    {
        await using var context = NewContext();
        var ativo = new Fornecedor(Guid.NewGuid(), "Fornecedor Ativo", DocumentoFiscal.Create("12345678000195"), "PJ", null, null, null,
            null, null, null, null, "Ativo", null, DateTimeOffset.UtcNow, Bu, businessUnit: null, erpSistema: "SOMA_DESENV", erpFornecedorId: "ERP-ATIVO");
        var inativo = new Fornecedor(Guid.NewGuid(), "Fornecedor Inativo", DocumentoFiscal.Create("11222333000181"), "PJ", null, null, null,
            null, null, null, null, "Inativo", null, DateTimeOffset.UtcNow, Bu, businessUnit: null, erpSistema: "SOMA_DESENV", erpFornecedorId: "ERP-INATIVO");
        await new FornecedorRepository(context).AdicionarAsync(ativo);
        await new FornecedorRepository(context).AdicionarAsync(inativo);

        var useCase = new BackfillFornecedorLinxVinculosUseCase(context, NullLogger<BackfillFornecedorLinxVinculosUseCase>.Instance);
        var resumo = await useCase.ExecuteAsync(new BackfillFornecedorLinxVinculosDto(DryRun: false));

        Assert.Equal("Concluido", resumo.Status);
        Assert.Equal(2, resumo.VinculosCriados);
        var vinculoAtivo = await context.FornecedorLinxVinculos.SingleAsync(v => v.CodigoErp == "ERP-ATIVO");
        var vinculoInativo = await context.FornecedorLinxVinculos.SingleAsync(v => v.CodigoErp == "ERP-INATIVO");
        Assert.True(vinculoAtivo.Principal);
        Assert.False(vinculoInativo.Principal);

        // Idempotente: rodar de novo não duplica nem re-seleciona Principal.
        var segundo = await useCase.ExecuteAsync(new BackfillFornecedorLinxVinculosDto(DryRun: false));
        Assert.Equal(0, segundo.VinculosCriados);
        Assert.Equal(2, segundo.VinculosJaExistentes);
    }

    // ===================== Helpers =====================

    private static SincronizarFornecedoresErpUseCase Create(BlueprintOSDbContext context, FakeIdentity identity, IFornecedorErpReader reader) =>
        new(reader, new FornecedorRepository(context), new FornecedorLinxVinculoRepository(context), new SincronizacaoFornecedorMonitorRepository(context),
            identity, context, NullLogger<SincronizarFornecedoresErpUseCase>.Instance);

    private static BlueprintOSDbContext NewContext() =>
        new(new DbContextOptionsBuilder<BlueprintOSDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static FornecedorCanonico Canonical(string razaoSocial, string nomeFantasia, string documento) =>
        new(razaoSocial, nomeFantasia, documento, "PJ", "BR", null, null, "01001000", "Rua ERP", "100", null, "Centro",
            "Sao Paulo", "SP", null, "11", "999999999", "erp@example.invalid", "fiscal@example.invalid", null, null, null,
            null, "001", "Fornecedor", null, null, "Normal", false, null, true, false, true, false, false, false, true,
            DateTimeOffset.UtcNow, Guid.NewGuid().ToString("N"));

    private sealed class FakeIdentity : ICurrentIdentity
    {
        public Guid UserId { get; } = Guid.NewGuid();
        public Guid UnidadeNegocioId { get; } = Guid.NewGuid();
        public RequestIdentity GetRequired() => new(UserId, "Buyer", UnidadeNegocioId);
    }

    private sealed class FakeReader(params FornecedorErpIntegracaoDto[] fornecedores) : IFornecedorErpReader
    {
        public Task<IReadOnlyList<FornecedorErpIntegracaoDto>> BuscarFornecedoresAsync(int limite, CancellationToken cancellationToken = default) =>
            BuscarFornecedoresAsync(0, limite, cancellationToken);

        public Task<IReadOnlyList<FornecedorErpIntegracaoDto>> BuscarFornecedoresAsync(int skip, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<FornecedorErpIntegracaoDto>>(fornecedores.Skip(skip).Take(take).ToList());
    }

    /// <summary>Simula uma falha fora do laço por registro (ex.: conexão perdida a meio da paginação) —
    /// GAP KALUNGA (Bloco 5A.9).</summary>
    private sealed class ThrowingReader : IFornecedorErpReader
    {
        public Task<IReadOnlyList<FornecedorErpIntegracaoDto>> BuscarFornecedoresAsync(int limite, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Falha simulada de conexão com o ERP.");

        public Task<IReadOnlyList<FornecedorErpIntegracaoDto>> BuscarFornecedoresAsync(int skip, int take, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Falha simulada de conexão com o ERP.");
    }
}
