using BlueprintOS.Api.Auth;
using BlueprintOS.Api.Authorization;
using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using BlueprintOS.Application.Procurement.Suppliers.Models;
using BlueprintOS.Domain.Identity;

namespace BlueprintOS.Api.Administration;

/// <summary>O1.13 — Administração Operacional e Monitoramento. Leitura das execuções em lote de
/// sincronização de fornecedores já persistidas por B2.1.3 (Monitor de Integrações e Monitor de
/// Filas/Reprocessamentos). Nenhum motor novo de sincronização é criado; apenas consulta sobre dados reais,
/// protegida pela mesma permissão corporativa <c>Sistema.Gerenciar</c> usada pelas demais telas de
/// Administração do Sistema (ver <see cref="FeatureFlagsController"/>).</summary>
public static class MonitoramentoOperacionalController
{
    private const string BaseRoute = "/api/administracao/monitoramento";

    public static IEndpointRouteBuilder MapMonitoramentoOperacional(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup(BaseRoute)
            .WithTags("Administração — Monitoramento Operacional")
            .RequireAuthorization(RbacPolicies.For(PermissaoCatalogo.SistemaGerenciar))
            .AddEndpointFilter<CsrfHeaderFilter>();

        group.MapGet("/sincronizacoes-fornecedores", Listar);
        group.MapGet("/sincronizacoes-fornecedores/{id:guid}", ObterDetalhe);

        return endpoints;
    }

    private static async Task<IResult> Listar(
        string? status, string? businessUnit, int? pagina, int? tamanhoPagina,
        IListarSincronizacoesFornecedoresUseCase useCase, CancellationToken ct)
    {
        var filtro = new ListarSincronizacoesFornecedoresFiltro(status, businessUnit, pagina ?? 1, tamanhoPagina ?? 20);
        return Results.Ok(await useCase.ExecuteAsync(filtro, ct));
    }

    private static async Task<IResult> ObterDetalhe(Guid id, IObterSincronizacaoFornecedorUseCase useCase, CancellationToken ct) =>
        await useCase.ExecuteAsync(id, ct) is { } detalhe
            ? Results.Ok(detalhe)
            : Results.NotFound(new { code = "sincronizacao_nao_encontrada", message = "Execução de sincronização não encontrada." });
}
