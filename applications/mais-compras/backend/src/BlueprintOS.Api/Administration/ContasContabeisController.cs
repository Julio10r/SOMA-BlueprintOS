using BlueprintOS.Api.Auth;
using BlueprintOS.Api.Authorization;
using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Domain.Identity;

namespace BlueprintOS.Api.Administration;

public sealed record ContaContabilMetadadoRequest(string? DescricaoMaisCompras, bool? AtivoNoMaisCompras);

/// <summary>Endpoints reais da Gestão de Contas Contábeis (B3 — Bloco 1, Discovery homologado). Mesmo
/// padrão de enforcement/escopo de <see cref="FiliaisController"/>/<see cref="CentrosCustoController"/>.
///
/// Conta Contábil é cadastro de apoio originado do ERP (`SOMA_DESENV`, `CTB_CONTA_PLANO`) — imutável no
/// +Compras: não há endpoint de criação nem de exclusão física, apenas leitura (combinada com metadados
/// locais) e atualização dos metadados locais permitidos (Descrição +Compras, Ativo/Inativo no
/// +Compras — sempre subordinado ao status real do Linx, `ADR-0024`).</summary>
public static class ContasContabeisController
{
    public static IEndpointRouteBuilder MapContasContabeis(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup(PerfisController.BaseRoute)
            .WithTags("Administração — Contas Contábeis")
            .RequireAuthorization(RbacPolicies.For(PermissaoCatalogo.ContaContabilGerenciar))
            .AddEndpointFilter<CsrfHeaderFilter>();

        group.MapGet("/contas-contabeis", ListarContasContabeis);
        group.MapPut("/contas-contabeis/{codigoErp}", AtualizarMetadado);

        return endpoints;
    }

    private static async Task<IResult> ListarContasContabeis(
        ICurrentIdentity identity, IListarContasContabeisUseCase useCase, CancellationToken ct)
    {
        if (!TryResolverUnidadeNegocio(identity, out var unidadeNegocioId, out var falha)) return falha!;
        return Results.Ok(await useCase.ExecuteAsync(unidadeNegocioId, ct));
    }

    private static async Task<IResult> AtualizarMetadado(
        string codigoErp, ContaContabilMetadadoRequest? request, ICurrentIdentity identity,
        IAtualizarMetadadoContaContabilUseCase useCase, CancellationToken ct)
    {
        if (!TryResolverUnidadeNegocio(identity, out var unidadeNegocioId, out var falha)) return falha!;

        var input = new ContaContabilMetadadoInput(request?.DescricaoMaisCompras, request?.AtivoNoMaisCompras ?? true);
        var resultado = await useCase.ExecuteAsync(codigoErp, input, unidadeNegocioId, ct);
        return resultado.Sucesso
            ? Results.Ok(resultado.Valor)
            : Results.NotFound(new { code = "conta_contabil_nao_encontrada", message = resultado.Mensagem });
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
