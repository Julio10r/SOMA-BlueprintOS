using BlueprintOS.Api.Auth;
using BlueprintOS.Api.Authorization;
using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Domain.Identity;

namespace BlueprintOS.Api.Administration;

public sealed record UsuarioUpsertRequest(
    string? Nome,
    string? Email,
    IReadOnlyList<Guid>? Perfis,
    IReadOnlyList<string>? CentrosCusto,
    bool? TodosCentrosCusto);

public sealed record UsuarioStatusRequest(bool Ativo);

/// <summary>Endpoints reais da Gestão de Usuários (O1.6), substituindo o mock de frontend
/// `administration/users/services/usuariosMockApi.ts`, seguindo exatamente o mesmo padrão de enforcement e
/// de escopo por Unidade de Negócio de <see cref="PerfisController"/> (O1.5):
/// - requisição sem sessão → <c>401</c> (FallbackPolicy global);
/// - sessão válida sem <c>Usuario.Gerenciar</c> → <c>403</c> (policy);
/// - Unidade de Negócio, por padrão, da identidade autenticada; a query <c>unidadeNegocioId</c> só é
///   honrada quando o ator é Administrador Sênior (Gate Final da Onda 1 — <see cref="EscopoAdministrativoUnidadeNegocio"/>);
/// - sem exclusão física — apenas Criar, Editar e Ativar/Inativar (Work Order O1.6, "Fora de escopo").</summary>
public static class UsuariosController
{
    public static IEndpointRouteBuilder MapUsuarios(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup(PerfisController.BaseRoute)
            .WithTags("Administração — Usuários")
            .RequireAuthorization(RbacPolicies.For(PermissaoCatalogo.UsuarioGerenciar))
            .AddEndpointFilter<CsrfHeaderFilter>();

        group.MapGet("/usuarios", ListarUsuarios);
        group.MapGet("/usuarios/{id:guid}", ObterUsuario);
        group.MapPost("/usuarios", CriarUsuario);
        group.MapPut("/usuarios/{id:guid}", AtualizarUsuario);
        group.MapPatch("/usuarios/{id:guid}/status", AlterarStatus);

        return endpoints;
    }

    private static async Task<IResult> ListarUsuarios(
        Guid? unidadeNegocioId, ICurrentIdentity identity, IListarUsuariosUseCase useCase, CancellationToken ct)
    {
        if (!EscopoAdministrativoUnidadeNegocio.TryResolverUnidadeNegocio(identity, unidadeNegocioId, out var resolvido, out var falha)) return falha!;
        return Results.Ok(await useCase.ExecuteAsync(resolvido, ct));
    }

    private static async Task<IResult> ObterUsuario(
        Guid id, Guid? unidadeNegocioId, ICurrentIdentity identity, IObterUsuarioUseCase useCase, CancellationToken ct)
    {
        if (!EscopoAdministrativoUnidadeNegocio.TryResolverUnidadeNegocio(identity, unidadeNegocioId, out var resolvido, out var falha)) return falha!;

        var usuario = await useCase.ExecuteAsync(id, resolvido, ct);
        return usuario is null
            ? Results.NotFound(new { code = "usuario_nao_encontrado", message = "Usuário não encontrado." })
            : Results.Ok(usuario);
    }

    private static async Task<IResult> CriarUsuario(
        UsuarioUpsertRequest? request, Guid? unidadeNegocioId, ICurrentIdentity identity, ICriarUsuarioUseCase useCase, CancellationToken ct)
    {
        if (!EscopoAdministrativoUnidadeNegocio.TryResolverUnidadeNegocio(identity, unidadeNegocioId, out var resolvido, out var falha)) return falha!;

        var resultado = await useCase.ExecuteAsync(ParaInput(request), resolvido, PermissoesDoAtor(identity), ct);
        return resultado.Sucesso ? Results.Created($"{PerfisController.BaseRoute}/usuarios/{resultado.Valor!.Id}", resultado.Valor) : Traduzir(resultado);
    }

    private static async Task<IResult> AtualizarUsuario(
        Guid id, UsuarioUpsertRequest? request, Guid? unidadeNegocioId, ICurrentIdentity identity, IAtualizarUsuarioUseCase useCase, CancellationToken ct)
    {
        if (!EscopoAdministrativoUnidadeNegocio.TryResolverUnidadeNegocio(identity, unidadeNegocioId, out var resolvido, out var falha)) return falha!;

        var resultado = await useCase.ExecuteAsync(id, ParaInput(request), resolvido, PermissoesDoAtor(identity), ct);
        return resultado.Sucesso ? Results.Ok(resultado.Valor) : Traduzir(resultado);
    }

    private static async Task<IResult> AlterarStatus(
        Guid id, UsuarioStatusRequest? request, Guid? unidadeNegocioId, ICurrentIdentity identity, IAlterarStatusUsuarioUseCase useCase, CancellationToken ct)
    {
        if (!EscopoAdministrativoUnidadeNegocio.TryResolverUnidadeNegocio(identity, unidadeNegocioId, out var resolvido, out var falha)) return falha!;
        if (request is null) return Results.BadRequest(new { code = "invalid_request", message = "Informe o status desejado." });

        var resultado = await useCase.ExecuteAsync(id, request.Ativo, resolvido, ct);
        return resultado.Sucesso ? Results.Ok(resultado.Valor) : Traduzir(resultado);
    }

    private static IReadOnlyList<string> PermissoesDoAtor(ICurrentIdentity identity) =>
        identity.GetRequired().Permissoes ?? [];

    private static UsuarioInput ParaInput(UsuarioUpsertRequest? request) => new(
        request?.Nome ?? string.Empty,
        request?.Email ?? string.Empty,
        request?.Perfis ?? [],
        request?.CentrosCusto ?? [],
        request?.TodosCentrosCusto ?? false);

    private static IResult Traduzir<T>(RbacResultado<T> resultado) => resultado.Falha switch
    {
        RbacFalha.UsuarioNaoEncontrado => Results.NotFound(new { code = "usuario_nao_encontrado", message = resultado.Mensagem }),
        RbacFalha.EmailDuplicado => Results.Conflict(new { code = "email_duplicado", message = resultado.Mensagem }),
        RbacFalha.UltimoAdministradorSeniorAtivo => Results.Conflict(new { code = "ultimo_administrador_senior_ativo", message = resultado.Mensagem }),
        RbacFalha.PerfilInvalido => Results.BadRequest(new { code = "perfil_invalido", message = resultado.Mensagem }),
        RbacFalha.CentroCustoInvalido => Results.BadRequest(new { code = "centro_custo_invalido", message = resultado.Mensagem }),
        RbacFalha.EscalonamentoDePrivilegio => Results.Json(
            new { code = "escalonamento_de_privilegio", message = resultado.Mensagem },
            statusCode: StatusCodes.Status403Forbidden),
        _ => Results.BadRequest(new { code = "requisicao_invalida", message = resultado.Mensagem }),
    };
}
