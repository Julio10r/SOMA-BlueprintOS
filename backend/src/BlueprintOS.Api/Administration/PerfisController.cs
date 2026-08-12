using BlueprintOS.Api.Auth;
using BlueprintOS.Api.Authorization;
using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Domain.Identity;

namespace BlueprintOS.Api.Administration;

public sealed record PerfilUpsertRequest(string? Nome, string? Descricao, IReadOnlyList<string>? Permissoes);

public sealed record PerfilStatusRequest(bool Ativo);

/// <summary>Endpoints reais da Gestão de Perfis (O1.5 — RBAC Real), substituindo o mock de frontend
/// `administration/profiles/services/perfisMockApi.ts`.
///
/// Enforcement: TODO endpoint deste grupo exige a permissão <c>Perfil.Gerenciar</c> via policy do
/// ASP.NET Core Authorization (<see cref="RbacPolicies"/>). Consequências deliberadas:
/// - requisição sem sessão → <c>401</c> (produzido pela <c>FallbackPolicy</c> global, antes deste código);
/// - sessão válida sem a permissão → <c>403</c> (produzido pela policy, antes deste código);
/// - o frontend esconder ou não um botão é irrelevante para o resultado: a barreira é aqui.
///
/// Escopo de dados: por padrão a Unidade de Negócio vem de <see cref="ICurrentIdentity"/> (claim da
/// sessão). Gate Final da Onda 1 — a query string aceita <c>unidadeNegocioId</c> apenas como o mecanismo
/// pelo qual o Administrador Sênior administra outra BU; ver
/// <see cref="EscopoAdministrativoUnidadeNegocio.TryResolverUnidadeNegocio"/>. Um Administrador de BU que
/// informe outra Unidade de Negócio recebe <c>403</c> — nunca lê/altera Perfis de outra BU, mesmo com um
/// Id válido.
///
/// Não há exclusão física: `ComprasFuncional.md` ("Gestão de Perfis") define como ações oficiais apenas
/// Criar, Editar e Ativar/Inativar — a inativação é a revogação de acesso, e preserva a auditabilidade
/// dos vínculos históricos.</summary>
public static class PerfisController
{
    /// <summary>Prefixo `/api` deliberado: as rotas da SPA usam `/administracao/*`, e um proxy de
    /// desenvolvimento que encaminhasse esse prefixo ao backend impediria o React Router de resolver as
    /// telas de Administração (o mesmo cuidado já registrado para `/bootstrap` em `vite.config.ts`).</summary>
    public const string BaseRoute = "/api/administracao";

    public static IEndpointRouteBuilder MapPerfis(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup(BaseRoute)
            .WithTags("Administração — Perfis")
            .RequireAuthorization(RbacPolicies.For(PermissaoCatalogo.PerfilGerenciar))
            // CSRF aplicado no GRUPO, não rota por rota: um endpoint novo acrescentado aqui no futuro
            // nasce protegido, em vez de perder a proteção por esquecimento de uma linha. O filtro é
            // inerte para métodos seguros (GET), então não há custo em generalizá-lo.
            .AddEndpointFilter<CsrfHeaderFilter>();

        group.MapGet("/permissoes", ListarPermissoes);
        group.MapGet("/perfis", ListarPerfis);
        group.MapGet("/perfis/{id:guid}", ObterPerfil);
        group.MapPost("/perfis", CriarPerfil);
        group.MapPut("/perfis/{id:guid}", AtualizarPerfil);
        group.MapPatch("/perfis/{id:guid}/status", AlterarStatus);

        return endpoints;
    }

    private static async Task<IResult> ListarPermissoes(IListarCatalogoPermissoesUseCase useCase, CancellationToken ct) =>
        Results.Ok(await useCase.ExecuteAsync(ct));

    private static async Task<IResult> ListarPerfis(
        Guid? unidadeNegocioId, ICurrentIdentity identity, IListarPerfisUseCase useCase, CancellationToken ct)
    {
        if (!EscopoAdministrativoUnidadeNegocio.TryResolverUnidadeNegocio(identity, unidadeNegocioId, out var resolvido, out var falha)) return falha!;
        return Results.Ok(await useCase.ExecuteAsync(resolvido, ct));
    }

    private static async Task<IResult> ObterPerfil(
        Guid id, Guid? unidadeNegocioId, ICurrentIdentity identity, IObterPerfilUseCase useCase, CancellationToken ct)
    {
        if (!EscopoAdministrativoUnidadeNegocio.TryResolverUnidadeNegocio(identity, unidadeNegocioId, out var resolvido, out var falha)) return falha!;

        var perfil = await useCase.ExecuteAsync(id, resolvido, ct);
        return perfil is null
            ? Results.NotFound(new { code = "perfil_nao_encontrado", message = "Perfil não encontrado." })
            : Results.Ok(perfil);
    }

    private static async Task<IResult> CriarPerfil(
        PerfilUpsertRequest? request, Guid? unidadeNegocioId, ICurrentIdentity identity, ICriarPerfilUseCase useCase, CancellationToken ct)
    {
        if (!EscopoAdministrativoUnidadeNegocio.TryResolverUnidadeNegocio(identity, unidadeNegocioId, out var resolvido, out var falha)) return falha!;

        // As permissões do ator vêm das claims resolvidas no backend — nunca do payload. Alimentam a regra
        // de não-escalonamento: ninguém concede uma permissão que não possui.
        var resultado = await useCase.ExecuteAsync(
            ParaInput(request), resolvido, PermissoesDoAtor(identity), ct);
        return resultado.Sucesso ? Results.Created($"{BaseRoute}/perfis/{resultado.Valor!.Id}", resultado.Valor) : Traduzir(resultado);
    }

    private static async Task<IResult> AtualizarPerfil(
        Guid id, PerfilUpsertRequest? request, Guid? unidadeNegocioId, ICurrentIdentity identity, IAtualizarPerfilUseCase useCase, CancellationToken ct)
    {
        if (!EscopoAdministrativoUnidadeNegocio.TryResolverUnidadeNegocio(identity, unidadeNegocioId, out var resolvido, out var falha)) return falha!;

        var resultado = await useCase.ExecuteAsync(
            id, ParaInput(request), resolvido, PermissoesDoAtor(identity), ct);
        return resultado.Sucesso ? Results.Ok(resultado.Valor) : Traduzir(resultado);
    }

    private static async Task<IResult> AlterarStatus(
        Guid id, PerfilStatusRequest? request, Guid? unidadeNegocioId, ICurrentIdentity identity, IAlterarStatusPerfilUseCase useCase, CancellationToken ct)
    {
        if (!EscopoAdministrativoUnidadeNegocio.TryResolverUnidadeNegocio(identity, unidadeNegocioId, out var resolvido, out var falha)) return falha!;
        if (request is null) return Results.BadRequest(new { code = "invalid_request", message = "Informe o status desejado." });

        var resultado = await useCase.ExecuteAsync(id, request.Ativo, resolvido, ct);
        return resultado.Sucesso ? Results.Ok(resultado.Valor) : Traduzir(resultado);
    }

    private static IReadOnlyList<string> PermissoesDoAtor(ICurrentIdentity identity) =>
        identity.GetRequired().Permissoes ?? [];

    private static PerfilInput ParaInput(PerfilUpsertRequest? request) => new(
        request?.Nome ?? string.Empty,
        request?.Descricao ?? string.Empty,
        request?.Permissoes ?? []);

    private static IResult Traduzir<T>(RbacResultado<T> resultado) => resultado.Falha switch
    {
        RbacFalha.PerfilNaoEncontrado => Results.NotFound(new { code = "perfil_nao_encontrado", message = resultado.Mensagem }),
        RbacFalha.NomeDuplicado => Results.Conflict(new { code = "nome_duplicado", message = resultado.Mensagem }),
        RbacFalha.UltimoPerfilAdministrativo => Results.Conflict(new { code = "ultimo_perfil_administrativo", message = resultado.Mensagem }),
        RbacFalha.PermissaoDesconhecida => Results.BadRequest(new { code = "permissao_desconhecida", message = resultado.Mensagem }),
        RbacFalha.NomeReservado => Results.Json(
            new { code = "nome_reservado", message = resultado.Mensagem },
            statusCode: StatusCodes.Status403Forbidden),
        RbacFalha.EscalonamentoDePrivilegio => Results.Json(
            new { code = "escalonamento_de_privilegio", message = resultado.Mensagem },
            statusCode: StatusCodes.Status403Forbidden),
        _ => Results.BadRequest(new { code = "requisicao_invalida", message = resultado.Mensagem }),
    };
}
