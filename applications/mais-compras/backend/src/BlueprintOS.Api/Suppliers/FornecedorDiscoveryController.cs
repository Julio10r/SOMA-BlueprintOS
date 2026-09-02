using BlueprintOS.Api.Authorization;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using BlueprintOS.Application.Procurement.Suppliers.Models;
using BlueprintOS.Domain.Identity;

namespace BlueprintOS.Api.Suppliers;

/// <summary>Endpoints da descoberta inteligente de fornecedores no ERP (busca fornecedores candidatos por
/// item/categoria — não é a mesma coisa que consultar CNPJ de um fornecedor já identificado).
///
/// RBAC (Retest do Gate de Fornecedores, 2026-09-01, item 3 — quitando dívida técnica: os 3 endpoints
/// abaixo tinham apenas autenticação, nenhuma permissão granular). Mapeamento de consumidores feito antes
/// desta correção: nenhum consumidor de frontend (nenhuma tela chama `/descobrir` ou `/descobertas`) e
/// nenhum consumidor de backend além do próprio controller (`grep` por `IDescobrirFornecedoresUseCase`/
/// `IListarDescobertasUseCase` só retorna a própria implementação, o DI e este controller). Os três
/// endpoints existem exclusivamente para servir o mesmo fluxo de descoberta ponta a ponta (buscar →
/// listar → ver um resultado) — não há uso legítimo divergente que justifique permissões diferentes entre
/// eles. `Fornecedor.Criar` é a policy correta pelo mesmo raciocínio já aplicado às demais rotas de apoio
/// ao cadastro (consulta-cnpj, consulta-cep, municipios, categorias, todas em FornecedoresController):
/// descoberta de fornecedores candidatos é parte do funil de trazer um novo fornecedor para o +Compras.</summary>
public static class FornecedorDiscoveryController
{
    public static IEndpointRouteBuilder MapFornecedorDiscovery(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/fornecedores")
            .WithTags("Descoberta de Fornecedores")
            .RequireAuthorization(RbacPolicies.For(PermissaoCatalogo.FornecedorCriar));
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
