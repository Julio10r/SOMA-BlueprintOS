using BlueprintOS.Api.Authorization;
using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Domain.Identity;

namespace BlueprintOS.Api.Administration;

/// <summary>B3 — Bloco 5A: rotas operacionais/administrativas de sincronização Linx -> +Compras de Item
/// Fiscal e Referências por Fornecedor (`docs/audits/B3-Bloco5A-*.md`). Mesma natureza das rotas de
/// <c>FornecedorSyncController</c> — protegidas por `Sistema.Gerenciar`, nunca por uma permissão de
/// cadastro comum (disparar sincronização em lote é ação administrativa, não CRUD de um recurso).</summary>
public static class ItemFiscalSyncController
{
    public static IEndpointRouteBuilder MapItemFiscalSync(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup(PerfisController.BaseRoute)
            .WithTags("Sincronização de Item Fiscal (ERP)")
            .RequireAuthorization(RbacPolicies.For(PermissaoCatalogo.SistemaGerenciar));

        group.MapPost("/itens-fiscais/sincronizar-erp", SincronizarItensFiscais);
        group.MapPost("/itens-fiscais/referencias-fornecedor/sincronizar-erp", SincronizarReferenciasFornecedor);

        return endpoints;
    }

    private static async Task<IResult> SincronizarItensFiscais(
        int? limite, string? correlationId, bool? dryRun, ISincronizarItensFiscaisErpUseCase useCase, CancellationToken ct)
    {
        try
        {
            var resumo = await useCase.ExecuteAsync(new SincronizarItensFiscaisErpDto(limite ?? 0, correlationId, dryRun ?? false), ct);
            return Results.Ok(resumo);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { code = "unidade_negocio_ausente", message = ex.Message });
        }
    }

    private static async Task<IResult> SincronizarReferenciasFornecedor(
        int? limite, string? correlationId, bool? dryRun, ISincronizarItemFiscalReferenciasFornecedorErpUseCase useCase, CancellationToken ct)
    {
        try
        {
            var resumo = await useCase.ExecuteAsync(new SincronizarItemFiscalReferenciasFornecedorErpDto(limite ?? 0, correlationId, dryRun ?? false), ct);
            return Results.Ok(resumo);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { code = "unidade_negocio_ausente", message = ex.Message });
        }
    }
}
