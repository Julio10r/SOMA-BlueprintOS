using BlueprintOS.Domain.Procurement.Suppliers;
using BlueprintOS.Infrastructure.Persistence;
using BlueprintOS.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BlueprintOS.UnitTests.Infrastructure.Persistence.Repositories;

public sealed class FornecedorCnpjConsultaHistoricoRepositoryTests
{
    private static readonly DateTimeOffset Referencia = new(2026, 8, 12, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ExpurgarPayloadBrutoExpiradoAsync_Should_Preserve_Payload_At_179_Days()
    {
        await using var context = NewContext();
        var registro = CriarRegistro(idadeDias: 179, payload: "{\"razao_social\":\"X\"}");
        context.Add(registro);
        await context.SaveChangesAsync();

        var quantidade = await new FornecedorCnpjConsultaHistoricoRepository(context)
            .ExpurgarPayloadBrutoExpiradoAsync(Referencia);

        Assert.Equal(0, quantidade);
        var recarregado = await context.FornecedoresCnpjConsultas.SingleAsync();
        Assert.NotNull(recarregado.PayloadBrutoJson);
    }

    [Fact]
    public async Task ExpurgarPayloadBrutoExpiradoAsync_Should_Preserve_Payload_Exactly_At_180_Days_Boundary()
    {
        await using var context = NewContext();
        var registro = CriarRegistro(idadeDias: 180, payload: "{\"razao_social\":\"X\"}");
        context.Add(registro);
        await context.SaveChangesAsync();

        var quantidade = await new FornecedorCnpjConsultaHistoricoRepository(context)
            .ExpurgarPayloadBrutoExpiradoAsync(Referencia);

        Assert.Equal(0, quantidade);
        var recarregado = await context.FornecedoresCnpjConsultas.SingleAsync();
        Assert.NotNull(recarregado.PayloadBrutoJson);
    }

    [Fact]
    public async Task ExpurgarPayloadBrutoExpiradoAsync_Should_Null_Payload_At_181_Days()
    {
        await using var context = NewContext();
        var registro = CriarRegistro(idadeDias: 181, payload: "{\"razao_social\":\"X\"}");
        context.Add(registro);
        await context.SaveChangesAsync();

        var quantidade = await new FornecedorCnpjConsultaHistoricoRepository(context)
            .ExpurgarPayloadBrutoExpiradoAsync(Referencia);

        Assert.Equal(1, quantidade);
        var recarregado = await context.FornecedoresCnpjConsultas.SingleAsync();
        Assert.Null(recarregado.PayloadBrutoJson);
    }

    [Fact]
    public async Task ExpurgarPayloadBrutoExpiradoAsync_Should_Keep_Structural_Fields_Untouched()
    {
        await using var context = NewContext();
        var registro = CriarRegistro(idadeDias: 181, payload: "{\"razao_social\":\"X\"}");
        context.Add(registro);
        await context.SaveChangesAsync();

        await new FornecedorCnpjConsultaHistoricoRepository(context).ExpurgarPayloadBrutoExpiradoAsync(Referencia);

        var recarregado = await context.FornecedoresCnpjConsultas.SingleAsync();
        Assert.Equal("12345678000195", recarregado.Cnpj_Cpf);
        Assert.Equal("BrasilAPI", recarregado.FonteConsulta);
        Assert.Equal(TipoErroConsultaCnpjHistorico.NaoEncontrado, recarregado.TipoErro);
        Assert.Equal("Falha", recarregado.Status);
        Assert.Equal(Referencia.AddDays(-181), recarregado.DataConsulta);
    }

    [Fact]
    public async Task ExpurgarPayloadBrutoExpiradoAsync_Should_Be_Idempotent_When_Run_Twice()
    {
        await using var context = NewContext();
        var registro = CriarRegistro(idadeDias: 200, payload: "{\"razao_social\":\"X\"}");
        context.Add(registro);
        await context.SaveChangesAsync();
        var repository = new FornecedorCnpjConsultaHistoricoRepository(context);

        var primeiraExecucao = await repository.ExpurgarPayloadBrutoExpiradoAsync(Referencia);
        var segundaExecucao = await repository.ExpurgarPayloadBrutoExpiradoAsync(Referencia);

        Assert.Equal(1, primeiraExecucao);
        Assert.Equal(0, segundaExecucao);
    }

    [Fact]
    public async Task ExpurgarPayloadBrutoExpiradoAsync_Should_Not_Touch_Records_Whose_Payload_Is_Already_Null()
    {
        await using var context = NewContext();
        var registro = CriarRegistro(idadeDias: 300, payload: null);
        context.Add(registro);
        await context.SaveChangesAsync();

        var quantidade = await new FornecedorCnpjConsultaHistoricoRepository(context)
            .ExpurgarPayloadBrutoExpiradoAsync(Referencia);

        Assert.Equal(0, quantidade);
    }

    [Fact]
    public async Task ExpurgarPayloadBrutoExpiradoAsync_Should_Only_Modify_Eligible_Records()
    {
        await using var context = NewContext();
        var recente = CriarRegistro(idadeDias: 10, payload: "{\"a\":1}");
        var expirado1 = CriarRegistro(idadeDias: 200, payload: "{\"a\":2}");
        var expirado2 = CriarRegistro(idadeDias: 400, payload: "{\"a\":3}");
        context.AddRange(recente, expirado1, expirado2);
        await context.SaveChangesAsync();

        var quantidade = await new FornecedorCnpjConsultaHistoricoRepository(context)
            .ExpurgarPayloadBrutoExpiradoAsync(Referencia);

        Assert.Equal(2, quantidade);
        var registros = await context.FornecedoresCnpjConsultas.ToListAsync();
        Assert.NotNull(registros.Single(r => r.Id == recente.Id).PayloadBrutoJson);
        Assert.Null(registros.Single(r => r.Id == expirado1.Id).PayloadBrutoJson);
        Assert.Null(registros.Single(r => r.Id == expirado2.Id).PayloadBrutoJson);
    }

    [Fact]
    public async Task ExpurgarPayloadBrutoExpiradoAsync_Should_Never_Delete_Historico_Rows_Or_Fornecedores()
    {
        await using var context = NewContext();
        var registro = CriarRegistro(idadeDias: 400, payload: "{\"a\":1}");
        context.Add(registro);
        await context.SaveChangesAsync();

        await new FornecedorCnpjConsultaHistoricoRepository(context).ExpurgarPayloadBrutoExpiradoAsync(Referencia);

        Assert.Equal(1, await context.FornecedoresCnpjConsultas.CountAsync());
        Assert.Empty(context.Fornecedores);
    }

    private static FornecedorCnpjConsultaHistorico CriarRegistro(int idadeDias, string? payload) =>
        new(Guid.NewGuid(), "12345678000195", "BrasilAPI", Referencia.AddDays(-idadeDias), Guid.NewGuid(),
            "Falha", "N/A", "CNPJ não encontrado.", $"corr-{Guid.NewGuid():N}", "BU-A", null,
            TipoErroConsultaCnpjHistorico.NaoEncontrado, payload);

    private static BlueprintOSDbContext NewContext() => new(new DbContextOptionsBuilder<BlueprintOSDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
