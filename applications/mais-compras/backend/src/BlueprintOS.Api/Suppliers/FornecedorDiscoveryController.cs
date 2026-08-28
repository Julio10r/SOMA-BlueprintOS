using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using BlueprintOS.Application.Procurement.Suppliers.Models;

namespace BlueprintOS.Api.Suppliers;

/// <summary>Endpoints da descoberta inteligente de fornecedores no ERP.</summary>
public static class FornecedorDiscoveryController
{
    public static IEndpointRouteBuilder MapFornecedorDiscovery(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/fornecedores").WithTags("Descoberta de Fornecedores");
        group.MapPost("/descobrir", Discover);
        group.MapGet("/descobertas", List);
        group.MapGet("/descobertas/{id:guid}", GetById);
        return endpoints;
    }

    private static async Task<IResult> Discover(DescobrirFornecedoresRequest? request, IDescobrirFornecedoresUseCase useCase, CancellationToken ct)
    {
        if (request is null) return Results.BadRequest(new { code = "invalid_request", message = "Request body is required." });
        try { return Results.Ok(await useCase.ExecuteAsync(request.ToDto(), ct)); }
        catch (ArgumentException ex) { return Results.BadRequest(new { code = "validation_error", message = ex.Message }); }
        catch (IdentityUnavailableException) { return Results.StatusCode(StatusCodes.Status503ServiceUnavailable); }
    }

    private static async Task<IResult> List(IListarDescobertasUseCase useCase, CancellationToken ct) => Results.Ok(await useCase.ExecuteAsync(ct));

    private static async Task<IResult> GetById(Guid id, IListarDescobertasUseCase useCase, CancellationToken ct) =>
        await useCase.ExecuteAsync(id, ct) is { } value ? Results.Ok(value) : Results.NotFound();
}

public sealed record DescobrirFornecedoresRequest(string CodigoItem, string? Descricao, string? Categoria)
{
    public DescobrirFornecedoresDto ToDto() => new(CodigoItem, Descricao, Categoria);
}
