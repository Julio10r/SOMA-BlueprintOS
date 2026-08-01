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
}

public sealed record SincronizarFornecedorRequest(string BusinessUnit, string ErpSistema, string? ErpFornecedorId,
    Guid? FornecedorId, DirecaoSincronizacao Direcao, string? CorrelationId)
{
    public SincronizarFornecedorDto ToDto() => new(BusinessUnit, ErpSistema, ErpFornecedorId, FornecedorId, Direcao, CorrelationId);
}

public sealed record SincronizarFornecedoresLoteRequest(string BusinessUnit, string ErpSistema, IReadOnlyList<Guid> FornecedorIds,
    int Limite, string? CorrelationId)
{
    public SincronizarFornecedoresLoteDto ToDto() => new(BusinessUnit, ErpSistema, FornecedorIds, Limite, CorrelationId);
}
