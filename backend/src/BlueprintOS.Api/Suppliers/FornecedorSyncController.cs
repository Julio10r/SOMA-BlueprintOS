using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using BlueprintOS.Application.Procurement.Suppliers.Models;

namespace BlueprintOS.Api.Suppliers;

public static class FornecedorSyncController
{
    public static IEndpointRouteBuilder MapFornecedorSync(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/fornecedores").WithTags("Sincronização de Fornecedores");
        group.MapPost("/sincronizar", Sync);
        group.MapPost("/sincronizar/lote", SyncBatch);
        group.MapGet("/sincronizar-erp", SyncErp);
        group.MapGet("/{fornecedorId:guid}/sincronizacoes", Audit);
        return endpoints;
    }

    private static async Task<IResult> Sync(SincronizarFornecedorRequest? request, ISincronizarFornecedorUseCase useCase, CancellationToken ct)
    {
        if (request is null) return Results.BadRequest(new { code = "invalid_request", message = "Request body is required." });
        try { return Results.Ok(await useCase.ExecuteAsync(request.ToDto(), ct)); }
        catch (ArgumentException ex) { return Results.BadRequest(new { code = "validation_error", message = ex.Message }); }
        catch (InvalidOperationException ex) { return Results.BadRequest(new { code = "adapter_error", message = ex.Message }); }
    }

    private static async Task<IResult> SyncBatch(SincronizarFornecedoresLoteRequest? request, ISincronizarFornecedorUseCase useCase, CancellationToken ct)
    {
        if (request is null) return Results.BadRequest(new { code = "invalid_request", message = "Request body is required." });
        try { return Results.Ok(await useCase.ExecutarLoteAsync(request.ToDto(), ct)); }
        catch (ArgumentException ex) { return Results.BadRequest(new { code = "validation_error", message = ex.Message }); }
    }

    private static async Task<IResult> SyncErp(string? businessUnit, int? limite, string? correlationId,
        ISincronizarFornecedoresErpUseCase useCase, CancellationToken ct)
    {
        try { return Results.Ok(await useCase.ExecuteAsync(new(businessUnit ?? "DEFAULT", limite ?? 100, correlationId), ct)); }
        catch (ArgumentException ex) { return Results.BadRequest(new { code = "validation_error", message = ex.Message }); }
        catch (InvalidOperationException ex) { return Results.BadRequest(new { code = "erp_configuration_error", message = ex.Message }); }
    }

    private static async Task<IResult> Audit(Guid fornecedorId, IFornecedorSincronizacaoRepository repository, CancellationToken ct) =>
        Results.Ok(await repository.ListarPorFornecedorAsync(fornecedorId, ct));
}

public sealed record SincronizarFornecedorRequest(string BusinessUnit, string ErpSistema, string? ErpFornecedorId,
    Guid? FornecedorId, DirecaoSincronizacao Direcao, string? CorrelationId, OperacaoFornecedor Operacao = OperacaoFornecedor.Sincronizar)
{
    public SincronizarFornecedorDto ToDto() => new(BusinessUnit, ErpSistema, ErpFornecedorId, FornecedorId, Direcao, CorrelationId, Operacao);
}

public sealed record SincronizarFornecedoresLoteRequest(string BusinessUnit, string ErpSistema, IReadOnlyList<Guid> FornecedorIds,
    int Limite, string? CorrelationId)
{
    public SincronizarFornecedoresLoteDto ToDto() => new(BusinessUnit, ErpSistema, FornecedorIds, Limite, CorrelationId);
}
