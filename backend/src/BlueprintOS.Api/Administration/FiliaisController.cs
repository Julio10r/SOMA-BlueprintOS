using BlueprintOS.Api.Auth;
using BlueprintOS.Api.Authorization;
using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Domain.Identity;

namespace BlueprintOS.Api.Administration;

public sealed record FilialMetadadoRequest(string? DescricaoMaisCompras, bool? AtivoNoMaisCompras);

/// <summary>Endpoints reais da Gestão de Filiais (O1.7), substituindo o mock de frontend
/// `administration/branches/services/filiaisMockApi.ts`, mesmo padrão de enforcement/escopo de
/// <see cref="PerfisController"/>/<see cref="UsuariosController"/>.
///
/// Filial é dado mestre integrado do ERP (`SOMA_DESENV`) — imutável no +Compras (ADR-0020, item 3): não há
/// endpoint de criação nem de exclusão física, apenas leitura (combinada com metadados locais) e
/// atualização dos metadados locais permitidos (Descrição +Compras, Ativo/Inativo no +Compras).</summary>
public static class FiliaisController
{
    public static IEndpointRouteBuilder MapFiliais(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup(PerfisController.BaseRoute)
            .WithTags("Administração — Filiais")
            .RequireAuthorization(RbacPolicies.For(PermissaoCatalogo.FilialGerenciar))
            .AddEndpointFilter<CsrfHeaderFilter>();

        group.MapGet("/filiais", ListarFiliais);
        group.MapPut("/filiais/{codigoCliFor}", AtualizarMetadado);

        return endpoints;
    }

    private static async Task<IResult> ListarFiliais(
        ICurrentIdentity identity, IListarFiliaisUseCase useCase, CancellationToken ct)
    {
        if (!TryResolverUnidadeNegocio(identity, out var unidadeNegocioId, out var falha)) return falha!;
        return Results.Ok(await useCase.ExecuteAsync(unidadeNegocioId, ct));
    }

    private static async Task<IResult> AtualizarMetadado(
        string codigoCliFor, FilialMetadadoRequest? request, ICurrentIdentity identity,
        IAtualizarMetadadoFilialUseCase useCase, CancellationToken ct)
    {
        if (!TryResolverUnidadeNegocio(identity, out var unidadeNegocioId, out var falha)) return falha!;

        var input = new FilialMetadadoInput(request?.DescricaoMaisCompras, request?.AtivoNoMaisCompras ?? true);
        var resultado = await useCase.ExecuteAsync(codigoCliFor, input, unidadeNegocioId, ct);
        return resultado.Sucesso
            ? Results.Ok(resultado.Valor)
            : Results.NotFound(new { code = "filial_nao_encontrada", message = resultado.Mensagem });
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
