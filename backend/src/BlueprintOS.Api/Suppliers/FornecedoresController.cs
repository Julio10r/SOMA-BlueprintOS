using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Application.Procurement.Suppliers.Contracts;

namespace BlueprintOS.Api.Suppliers;

/// <summary>Endpoints REST do cadastro persistente de fornecedores.</summary>
public static class FornecedoresController
{
    public static IEndpointRouteBuilder MapFornecedores(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/fornecedores").WithTags("Fornecedores");
        group.MapPost("", Create);
        group.MapGet("", Search);
        group.MapGet("/{id:guid}", GetById);
        group.MapPut("/{id:guid}", Update);
        group.MapDelete("/{id:guid}", Delete);
        group.MapPost("/{id:guid}/enriquecimento-cnpj", AnalyzeCnpjEnrichment);
        group.MapPost("/{id:guid}/enriquecimento-cnpj/aprovar", ApproveCnpjEnrichment);
        group.MapPost("/{id:guid}/enriquecimento-cnpj/rejeitar", RejectCnpjEnrichment);
        return endpoints;
    }

    private static async Task<IResult> Create(FornecedorRequest? request, ICadastrarFornecedorUseCase useCase, CancellationToken ct)
    {
        if (request is null) return Results.BadRequest(new { code = "invalid_request", message = "Request body is required." });
        try
        {
            var supplier = await useCase.ExecuteAsync(request.ToCreateDto(), ct);
            return Results.Created($"/fornecedores/{supplier.Id}", supplier);
        }
        catch (ArgumentException ex) { return Results.BadRequest(new { code = "validation_error", message = ex.Message }); }
        catch (InvalidOperationException ex) { return Results.Conflict(new { code = "duplicate_cnpj", message = ex.Message }); }
        catch (IdentityUnavailableException) { return Results.StatusCode(StatusCodes.Status503ServiceUnavailable); }
    }

    private static async Task<IResult> Search(string? q, IPesquisarFornecedorUseCase useCase, CancellationToken ct) => Results.Ok(await useCase.ExecuteAsync(q, ct));
    private static async Task<IResult> GetById(Guid id, IObterFornecedorUseCase useCase, CancellationToken ct) =>
        await useCase.ExecuteAsync(id, ct) is { } supplier ? Results.Ok(supplier) : Results.NotFound();
    private static async Task<IResult> Update(Guid id, AtualizarFornecedorRequest? request, IAtualizarFornecedorUseCase useCase, CancellationToken ct)
    {
        if (request is null) return Results.BadRequest(new { code = "invalid_request", message = "Request body is required." });
        try { return await useCase.ExecuteAsync(id, request.ToDto(), ct) is { } supplier ? Results.Ok(supplier) : Results.NotFound(); }
        catch (ArgumentException ex) { return Results.BadRequest(new { code = "validation_error", message = ex.Message }); }
    }
    private static async Task<IResult> Delete(Guid id, IExcluirFornecedorUseCase useCase, CancellationToken ct) =>
        await useCase.ExecuteAsync(id, ct) ? Results.NoContent() : Results.NotFound();

    private static async Task<IResult> AnalyzeCnpjEnrichment(Guid id, FornecedorEnriquecimentoRequest? request,
        IAnalisarEnriquecimentoFornecedorUseCase useCase, CancellationToken ct)
    {
        if (request is null) return Results.BadRequest(new { code = "invalid_request", message = "Request body is required." });
        try { return await useCase.ExecuteAsync(id, request.ToDto(), ct) is { } result ? Results.Ok(result) : Results.NotFound(); }
        catch (ArgumentException ex) { return Results.BadRequest(new { code = "validation_error", message = ex.Message }); }
    }

    private static async Task<IResult> ApproveCnpjEnrichment(Guid id, FornecedorEnriquecimentoDecisaoRequest? request,
        IAprovarEnriquecimentoFornecedorUseCase useCase, CancellationToken ct)
    {
        if (request is null) return Results.BadRequest(new { code = "invalid_request", message = "Request body is required." });
        try { return await useCase.ExecuteAsync(id, request.ToDto(), ct) is { } result ? Results.Ok(result) : Results.NotFound(); }
        catch (ArgumentException ex) { return Results.BadRequest(new { code = "validation_error", message = ex.Message }); }
    }

    private static async Task<IResult> RejectCnpjEnrichment(Guid id, FornecedorEnriquecimentoDecisaoRequest? request,
        IRejeitarEnriquecimentoFornecedorUseCase useCase, CancellationToken ct)
    {
        if (request is null) return Results.BadRequest(new { code = "invalid_request", message = "Request body is required." });
        try { return await useCase.ExecuteAsync(id, request.ToDto(), ct) is { } result ? Results.Ok(result) : Results.NotFound(); }
        catch (ArgumentException ex) { return Results.BadRequest(new { code = "validation_error", message = ex.Message }); }
    }
}
