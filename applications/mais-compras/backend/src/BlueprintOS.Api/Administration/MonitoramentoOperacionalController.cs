using BlueprintOS.Api.Auth;
using BlueprintOS.Api.Authorization;
using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using BlueprintOS.Application.Procurement.Suppliers.Models;
using BlueprintOS.Domain.Identity;

namespace BlueprintOS.Api.Administration;

/// <summary>O1.13 — Administração Operacional e Monitoramento. Leitura das execuções em lote de
/// sincronização de fornecedores já persistidas por B2.1.3 (Monitor de Integrações e Monitor de
/// Filas/Reprocessamentos). Nenhum motor novo de sincronização é criado; apenas consulta sobre dados reais,
/// protegida pela mesma permissão corporativa <c>Sistema.Gerenciar</c> usada pelas demais telas de
/// Administração do Sistema (ver <see cref="FeatureFlagsController"/>).
///
/// DEB-03 (Gate Final da Onda 1, 11/08/2026) — <c>Sistema.Gerenciar</c> é uma permissão administrativa
/// transversal, mas a auditoria concluiu que isso não deve implicar visibilidade cross-BU: cada
/// listagem/detalhe é escopado pela Unidade de Negócio resolvida da sessão (<see cref="ICurrentIdentity"/>),
/// nunca pelo <c>businessUnit</c> de texto livre informado pelo chamador — mesmo padrão de
/// <see cref="CentrosCustoController"/>/<see cref="FiliaisController"/>.</summary>
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
        ICurrentIdentity identity, IListarSincronizacoesFornecedoresUseCase useCase, CancellationToken ct)
    {
        if (!TryResolverUnidadeNegocio(identity, out var unidadeNegocioId, out var falha)) return falha!;
        var filtro = new ListarSincronizacoesFornecedoresFiltro(status, businessUnit, pagina ?? 1, tamanhoPagina ?? 20);
        return Results.Ok(await useCase.ExecuteAsync(unidadeNegocioId, filtro, ct));
    }

    private static async Task<IResult> ObterDetalhe(
        Guid id, ICurrentIdentity identity, IObterSincronizacaoFornecedorUseCase useCase, CancellationToken ct)
    {
        if (!TryResolverUnidadeNegocio(identity, out var unidadeNegocioId, out var falha)) return falha!;
        return await useCase.ExecuteAsync(unidadeNegocioId, id, ct) is { } detalhe
            ? Results.Ok(detalhe)
            : Results.NotFound(new { code = "sincronizacao_nao_encontrada", message = "Execução de sincronização não encontrada." });
    }

    private static bool TryResolverUnidadeNegocio(ICurrentIdentity identity, out Guid unidadeNegocioId, out IResult? falha)
    {
        var atual = identity.GetRequired();
        if (atual.UnidadeNegocioId is null || atual.UnidadeNegocioId == Guid.Empty)
        {
            unidadeNegocioId = Guid.Empty;
            falha = Results.Json(
                new { code = "unidade_negocio_ausente", message = "A sessão atual não possui Unidade de Negócio resolvida." },
                statusCode: StatusCodes.Status403Forbidden);
            return false;
        }
        unidadeNegocioId = atual.UnidadeNegocioId.Value;
        falha = null;
        return true;
    }
}
