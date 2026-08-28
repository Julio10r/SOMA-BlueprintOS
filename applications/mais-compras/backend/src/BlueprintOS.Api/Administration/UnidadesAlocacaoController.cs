using BlueprintOS.Api.Auth;
using BlueprintOS.Api.Authorization;
using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Domain.Identity;

namespace BlueprintOS.Api.Administration;

public sealed record UnidadeAlocacaoUpsertRequest(string? Nome, string? Descricao);

public sealed record UnidadeAlocacaoStatusRequest(bool Ativo);

/// <summary>Endpoints reais da Gestão de Unidades de Alocação (O1.8), substituindo o mock de frontend
/// `administration/allocation-units/services/unidadesAlocacaoMockApi.ts`, seguindo exatamente o mesmo
/// padrão de enforcement e de escopo por Unidade de Negócio de <see cref="UsuariosController"/> (O1.6):
/// - requisição sem sessão → <c>401</c> (FallbackPolicy global);
/// - sessão válida sem <c>UnidadeAlocacao.Gerenciar</c> → <c>403</c> (policy);
/// - Unidade de Negócio sempre da identidade autenticada, nunca do payload;
/// - sem exclusão física — apenas Criar, Editar e Ativar/Inativar;
/// - sem vínculo com Centro de Custo (escopo da O1.9) e sem integração ERP (ADR-0020, item 4).</summary>
public static class UnidadesAlocacaoController
{
    public static IEndpointRouteBuilder MapUnidadesAlocacao(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup(PerfisController.BaseRoute)
            .WithTags("Administração — Unidades de Alocação")
            .RequireAuthorization(RbacPolicies.For(PermissaoCatalogo.UnidadeAlocacaoGerenciar))
            .AddEndpointFilter<CsrfHeaderFilter>();

        group.MapGet("/unidades-alocacao", ListarUnidadesAlocacao);
        group.MapGet("/unidades-alocacao/{id:guid}", ObterUnidadeAlocacao);
        group.MapPost("/unidades-alocacao", CriarUnidadeAlocacao);
        group.MapPut("/unidades-alocacao/{id:guid}", AtualizarUnidadeAlocacao);
        group.MapPatch("/unidades-alocacao/{id:guid}/status", AlterarStatus);

        return endpoints;
    }

    private static async Task<IResult> ListarUnidadesAlocacao(
        ICurrentIdentity identity, IListarUnidadesAlocacaoUseCase useCase, CancellationToken ct)
    {
        if (!TryResolverUnidadeNegocio(identity, out var unidadeNegocioId, out var falha)) return falha!;
        return Results.Ok(await useCase.ExecuteAsync(unidadeNegocioId, ct));
    }

    private static async Task<IResult> ObterUnidadeAlocacao(
        Guid id, ICurrentIdentity identity, IObterUnidadeAlocacaoUseCase useCase, CancellationToken ct)
    {
        if (!TryResolverUnidadeNegocio(identity, out var unidadeNegocioId, out var falha)) return falha!;

        var unidade = await useCase.ExecuteAsync(id, unidadeNegocioId, ct);
        return unidade is null
            ? Results.NotFound(new { code = "unidade_alocacao_nao_encontrada", message = "Unidade de Alocação não encontrada." })
            : Results.Ok(unidade);
    }

    private static async Task<IResult> CriarUnidadeAlocacao(
        UnidadeAlocacaoUpsertRequest? request, ICurrentIdentity identity, ICriarUnidadeAlocacaoUseCase useCase, CancellationToken ct)
    {
        if (!TryResolverUnidadeNegocio(identity, out var unidadeNegocioId, out var falha)) return falha!;

        var resultado = await useCase.ExecuteAsync(ParaInput(request), unidadeNegocioId, ct);
        return resultado.Sucesso
            ? Results.Created($"{PerfisController.BaseRoute}/unidades-alocacao/{resultado.Valor!.Id}", resultado.Valor)
            : Traduzir(resultado);
    }

    private static async Task<IResult> AtualizarUnidadeAlocacao(
        Guid id, UnidadeAlocacaoUpsertRequest? request, ICurrentIdentity identity, IAtualizarUnidadeAlocacaoUseCase useCase, CancellationToken ct)
    {
        if (!TryResolverUnidadeNegocio(identity, out var unidadeNegocioId, out var falha)) return falha!;

        var resultado = await useCase.ExecuteAsync(id, ParaInput(request), unidadeNegocioId, ct);
        return resultado.Sucesso ? Results.Ok(resultado.Valor) : Traduzir(resultado);
    }

    private static async Task<IResult> AlterarStatus(
        Guid id, UnidadeAlocacaoStatusRequest? request, ICurrentIdentity identity, IAlterarStatusUnidadeAlocacaoUseCase useCase, CancellationToken ct)
    {
        if (!TryResolverUnidadeNegocio(identity, out var unidadeNegocioId, out var falha)) return falha!;
        if (request is null) return Results.BadRequest(new { code = "invalid_request", message = "Informe o status desejado." });

        var resultado = await useCase.ExecuteAsync(id, request.Ativo, unidadeNegocioId, ct);
        return resultado.Sucesso ? Results.Ok(resultado.Valor) : Traduzir(resultado);
    }

    private static UnidadeAlocacaoInput ParaInput(UnidadeAlocacaoUpsertRequest? request) => new(
        request?.Nome ?? string.Empty,
        request?.Descricao ?? string.Empty);

    /// <summary>Mesmo cuidado de <see cref="UsuariosController"/>/<see cref="PerfisController"/>: sessão
    /// sem Unidade de Negócio resolvida nunca é tratada como "sem restrição" — é <c>403</c> explícito,
    /// fail-closed.</summary>
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

    private static IResult Traduzir<T>(RbacResultado<T> resultado) => resultado.Falha switch
    {
        RbacFalha.UnidadeAlocacaoNaoEncontrada => Results.NotFound(new { code = "unidade_alocacao_nao_encontrada", message = resultado.Mensagem }),
        RbacFalha.NomeDuplicado => Results.Conflict(new { code = "nome_duplicado", message = resultado.Mensagem }),
        RbacFalha.NomeObrigatorio => Results.BadRequest(new { code = "nome_obrigatorio", message = resultado.Mensagem }),
        _ => Results.BadRequest(new { code = "requisicao_invalida", message = resultado.Mensagem }),
    };
}
