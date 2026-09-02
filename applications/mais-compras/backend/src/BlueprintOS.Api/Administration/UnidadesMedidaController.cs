using BlueprintOS.Api.Auth;
using BlueprintOS.Api.Authorization;
using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Domain.Identity;

namespace BlueprintOS.Api.Administration;

public sealed record UnidadeMedidaMetadadoRequest(string? DescricaoMaisCompras, bool? AtivoNoMaisCompras);

/// <summary>Endpoints reais da Gestão de Unidades de Medida (B3 — Bloco 2, Discovery homologado). Mesmo
/// padrão de enforcement/escopo de <see cref="ContasContabeisController"/>/<see cref="FiliaisController"/>.
///
/// Unidade de Medida é cadastro de apoio originado do ERP (`SOMA_DESENV`, `UNIDADES`) — imutável no
/// +Compras: não há endpoint de criação nem de exclusão física, apenas leitura (combinada com metadados
/// locais) e atualização dos metadados locais permitidos (Descrição +Compras, Ativo/Inativo no
/// +Compras).</summary>
public static class UnidadesMedidaController
{
    public static IEndpointRouteBuilder MapUnidadesMedida(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup(PerfisController.BaseRoute)
            .WithTags("Administração — Unidades de Medida")
            .RequireAuthorization(RbacPolicies.For(PermissaoCatalogo.UnidadeMedidaGerenciar))
            .AddEndpointFilter<CsrfHeaderFilter>();

        group.MapGet("/unidades-medida", ListarUnidades);
        group.MapPut("/unidades-medida/{codigoErp}", AtualizarMetadado);

        return endpoints;
    }

    private static async Task<IResult> ListarUnidades(
        ICurrentIdentity identity, IListarUnidadesMedidaUseCase useCase, CancellationToken ct)
    {
        if (!TryResolverUnidadeNegocio(identity, out var unidadeNegocioId, out var falha)) return falha!;
        return Results.Ok(await useCase.ExecuteAsync(unidadeNegocioId, ct));
    }

    private static async Task<IResult> AtualizarMetadado(
        string codigoErp, UnidadeMedidaMetadadoRequest? request, ICurrentIdentity identity,
        IAtualizarMetadadoUnidadeMedidaUseCase useCase, CancellationToken ct)
    {
        if (!TryResolverUnidadeNegocio(identity, out var unidadeNegocioId, out var falha)) return falha!;

        var input = new UnidadeMedidaMetadadoInput(request?.DescricaoMaisCompras, request?.AtivoNoMaisCompras ?? true);
        var resultado = await useCase.ExecuteAsync(codigoErp, input, unidadeNegocioId, ct);
        return resultado.Sucesso
            ? Results.Ok(resultado.Valor)
            : Results.NotFound(new { code = "unidade_medida_nao_encontrada", message = resultado.Mensagem });
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
