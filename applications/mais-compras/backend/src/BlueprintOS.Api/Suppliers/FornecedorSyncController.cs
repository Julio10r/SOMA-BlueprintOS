using BlueprintOS.Api.Authorization;
using BlueprintOS.Application.Procurement.Suppliers;
using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using BlueprintOS.Application.Procurement.Suppliers.Models;
using BlueprintOS.Domain.Identity;

namespace BlueprintOS.Api.Suppliers;

/// <summary>O1.13 — Todas as rotas são ações operacionais/administrativas (disparar sincronização em
/// lote/seletiva, consultar histórico de sincronização por fornecedor), mesma natureza das telas de
/// Monitor de Integrações. Protegidas por <c>Sistema.Gerenciar</c>, quitando a dívida técnica de B2.1.3
/// que nunca teve RBAC.</summary>
public static class FornecedorSyncController
{
    public static IEndpointRouteBuilder MapFornecedorSync(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/fornecedores")
            .WithTags("Sincronização de Fornecedores")
            .RequireAuthorization(RbacPolicies.For(PermissaoCatalogo.SistemaGerenciar));
        group.MapPost("/sincronizar", Sync);
        group.MapPost("/sincronizar/lote", SyncBatch);
        group.MapGet("/sincronizar-erp", SyncErp);
        group.MapGet("/{fornecedorId:guid}/sincronizacoes", Audit);

        // B2.9 — operação explícita de negócio ("garantir/atualizar fornecedor no ERP"), distinta das
        // rotas administrativas acima: exige apenas a permissão de edição de Fornecedor, nunca disparada
        // implicitamente por consulta de CNPJ (B2.6).
        endpoints.MapGroup("/api/fornecedores")
            .WithTags("Sincronização de Fornecedores")
            .RequireAuthorization(RbacPolicies.For(PermissaoCatalogo.FornecedorEditar))
            .MapPost("/{fornecedorId:guid}/garantir-erp", GarantirErp);

        return endpoints;
    }

    private static async Task<IResult> GarantirErp(Guid fornecedorId, GarantirFornecedorErpRequestBody? body,
        IGarantirFornecedorNoErpUseCase useCase, CancellationToken ct)
    {
        if (body is null || string.IsNullOrWhiteSpace(body.BusinessUnit))
            return Results.BadRequest(new { code = "validation_error", message = "BusinessUnit é obrigatória." });
        try
        {
            var resultado = await useCase.ExecuteAsync(fornecedorId, body.BusinessUnit, new GarantirFornecedorNoErpDto(body.CorrelationId), ct);
            return resultado is null ? Results.NotFound() : Results.Ok(resultado);
        }
        catch (ErpFornecedorEscritaException ex)
        {
            return ex.Tipo switch
            {
                ErpFornecedorErro.Validacao => Results.BadRequest(new { code = "validation_error", message = ex.Message }),
                ErpFornecedorErro.ConflitoRecuperavel => Results.Conflict(new { code = "conflict_retryable", message = ex.Message }),
                ErpFornecedorErro.Timeout => Results.StatusCode(StatusCodes.Status504GatewayTimeout),
                ErpFornecedorErro.Conectividade => Results.StatusCode(StatusCodes.Status502BadGateway),
                _ => Results.StatusCode(StatusCodes.Status500InternalServerError)
            };
        }
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

    private static async Task<IResult> SyncErp(string? businessUnit, int? limite, string? correlationId, bool? dryRun,
        ISincronizarFornecedoresErpUseCase useCase, CancellationToken ct)
    {
        // Requisito 3 — o default de "limite" deixou de ser 100: quando não informado (ou <= 0), o use
        // case pagina até a fonte esgotar naturalmente, sem teto artificial. "limite" continua aceito
        // como teto explícito quando o chamador realmente quiser um.
        try { return Results.Ok(await useCase.ExecuteAsync(new(businessUnit ?? "DEFAULT", limite ?? 0, correlationId, dryRun ?? false), ct)); }
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

public sealed record GarantirFornecedorErpRequestBody(string BusinessUnit, string? CorrelationId);
