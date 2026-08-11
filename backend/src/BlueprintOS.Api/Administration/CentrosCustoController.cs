using BlueprintOS.Api.Auth;
using BlueprintOS.Api.Authorization;
using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Domain.Identity;

namespace BlueprintOS.Api.Administration;

public sealed record CentroCustoMetadadoRequest(string? DescricaoMaisCompras, bool? AtivoNoMaisCompras);

/// <summary>Endpoints reais da Gestão de Centros de Custo (O1.7), substituindo o mock de frontend
/// `administration/cost-centers/services/centrosCustoMockApi.ts`, mesmo padrão de enforcement/escopo de
/// <see cref="PerfisController"/>/<see cref="UsuariosController"/>.
///
/// Centro de Custo é dado mestre integrado do ERP (`SOMA_DESENV`) — imutável no +Compras (ADR-0020,
/// item 3): não há endpoint de criação nem de exclusão física, apenas leitura (combinada com metadados
/// locais) e atualização dos metadados locais permitidos (Descrição +Compras, Ativo/Inativo no +Compras).
/// Relacionamento N:N com Unidade de Alocação é fora de escopo (O1.9).</summary>
public static class CentrosCustoController
{
    public static IEndpointRouteBuilder MapCentrosCusto(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup(PerfisController.BaseRoute)
            .WithTags("Administração — Centros de Custo")
            .RequireAuthorization(RbacPolicies.For(PermissaoCatalogo.CentroCustoGerenciar))
            .AddEndpointFilter<CsrfHeaderFilter>();

        group.MapGet("/centros-custo", ListarCentrosCusto);
        group.MapPut("/centros-custo/{codigoErp}", AtualizarMetadado);

        return endpoints;
    }

    private static async Task<IResult> ListarCentrosCusto(
        ICurrentIdentity identity, IListarCentrosCustoUseCase useCase, CancellationToken ct)
    {
        if (!TryResolverUnidadeNegocio(identity, out var unidadeNegocioId, out var falha)) return falha!;
        return Results.Ok(await useCase.ExecuteAsync(unidadeNegocioId, ct));
    }

    private static async Task<IResult> AtualizarMetadado(
        string codigoErp, CentroCustoMetadadoRequest? request, ICurrentIdentity identity,
        IAtualizarMetadadoCentroCustoUseCase useCase, CancellationToken ct)
    {
        if (!TryResolverUnidadeNegocio(identity, out var unidadeNegocioId, out var falha)) return falha!;

        var input = new CentroCustoMetadadoInput(request?.DescricaoMaisCompras, request?.AtivoNoMaisCompras ?? true);
        var resultado = await useCase.ExecuteAsync(codigoErp, input, unidadeNegocioId, ct);
        if (resultado.Sucesso) return Results.Ok(resultado.Valor);

        return resultado.Falha switch
        {
            ErpMetadadoFalha.AncoradoPorOutraUnidadeDeNegocio => Results.Conflict(
                new { code = "centro_custo_ancorado_outra_unidade", message = resultado.Mensagem }),
            _ => Results.NotFound(new { code = "centro_custo_nao_encontrado", message = resultado.Mensagem })
        };
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
