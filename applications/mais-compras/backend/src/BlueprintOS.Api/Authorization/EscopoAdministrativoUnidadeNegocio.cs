using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Domain.Identity;

namespace BlueprintOS.Api.Authorization;

/// <summary>Abstração central do escopo administrativo (Gate Final da Onda 1). RBAC (<see cref="RbacPolicies"/>)
/// responde "o ator pode executar esta operação?"; esta classe responde "em qual Unidade de Negócio?".
/// Nenhum controller deve reimplementar esta regra comparando <c>Perfil.Nome</c> ou confiando isoladamente
/// em <c>unidadeNegocioId</c> vindo de path/body/query — todos devem passar pelo mesmo ponto.</summary>
public static class EscopoAdministrativoUnidadeNegocio
{
    /// <summary>Autoriza o ator a operar sobre <paramref name="unidadeNegocioIdAlvo"/>. Administrador
    /// Sênior (<see cref="EscopoAdministrativo.Produto"/>) atravessa qualquer Unidade de Negócio — a
    /// permissão RBAC do recurso (ex.: <c>Alcada.Gerenciar</c>) já foi validada pela policy antes deste
    /// código executar. Administrador de BU (<see cref="EscopoAdministrativo.Negocio"/>) só é autorizado
    /// quando o alvo coincide com a própria Unidade de Negócio da sessão — mesmo que conheça o Id de
    /// outra BU.</summary>
    public static bool Autoriza(RequestIdentity identity, Guid unidadeNegocioIdAlvo) =>
        identity.EscopoAdministrativo == EscopoAdministrativo.Produto
        || identity.UnidadeNegocioId == unidadeNegocioIdAlvo;

    public static IResult Negado() => Results.Json(
        new { code = "escopo_administrativo_negado", message = "Administração de outra Unidade de Negócio não é permitida para este ator." },
        statusCode: StatusCodes.Status403Forbidden);

    /// <summary>Resolve a Unidade de Negócio administrada pelos controllers cujo padrão histórico é "BU
    /// da sessão" (Perfis, Usuários, Filiais, Centros de Custo, Unidades de Alocação, Monitoramento
    /// Operacional). <paramref name="overrideUnidadeNegocioId"/> é o parâmetro opcional de query que só o
    /// Administrador Sênior pode usar para administrar outra BU — um Administrador de BU que o informe
    /// (mesmo com o Id correto de outra BU) recebe <c>403</c>, nunca é silenciosamente ignorado nem
    /// redirecionado à própria BU. Sessão sem Unidade de Negócio resolvida (esquema de Development) é
    /// <c>403</c> fail-closed, nunca "sem restrição".</summary>
    public static bool TryResolverUnidadeNegocio(
        ICurrentIdentity identity, Guid? overrideUnidadeNegocioId, out Guid unidadeNegocioId, out IResult? falha)
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

        if (overrideUnidadeNegocioId is null || overrideUnidadeNegocioId == Guid.Empty)
        {
            unidadeNegocioId = atual.UnidadeNegocioId.Value;
            falha = null;
            return true;
        }

        if (!Autoriza(atual, overrideUnidadeNegocioId.Value))
        {
            unidadeNegocioId = Guid.Empty;
            falha = Negado();
            return false;
        }

        unidadeNegocioId = overrideUnidadeNegocioId.Value;
        falha = null;
        return true;
    }
}

/// <summary>Endpoint filter reutilizável para os controllers cujo recurso administrado recebe
/// <c>unidadeNegocioId</c> explícito no path (Alçadas, Regras de Workflow, Regras Orçamentárias,
/// Identity Providers, Configuração de Notificações). Aplicado no nível do <c>MapGroup</c> — um endpoint
/// novo acrescentado ao grupo nasce protegido, no mesmo espírito de <c>CsrfHeaderFilter</c>. Substitui o
/// comportamento anterior de "confiar em qualquer unidadeNegocioId do path desde que a permissão RBAC do
/// recurso esteja presente", que permitia um Administrador de BU atravessar BUs (achado do Gate).</summary>
public sealed class EscopoUnidadeNegocioPathFilter(ICurrentIdentity currentIdentity) : IEndpointFilter
{
    public const string RouteValueName = "unidadeNegocioId";

    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var routeValue = context.HttpContext.GetRouteValue(RouteValueName)?.ToString();
        if (!Guid.TryParse(routeValue, out var unidadeNegocioIdAlvo))
        {
            // Sem valor de rota reconhecível: nada a validar aqui — o endpoint decide o próprio 400/404.
            return next(context);
        }

        var identity = currentIdentity.GetRequired();
        if (!EscopoAdministrativoUnidadeNegocio.Autoriza(identity, unidadeNegocioIdAlvo))
        {
            return ValueTask.FromResult<object?>(EscopoAdministrativoUnidadeNegocio.Negado());
        }

        return next(context);
    }
}
