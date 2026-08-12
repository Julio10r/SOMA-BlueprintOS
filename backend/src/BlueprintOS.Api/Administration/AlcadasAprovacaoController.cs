using BlueprintOS.Api.Auth;
using BlueprintOS.Api.Authorization;
using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Domain.Identity;

namespace BlueprintOS.Api.Administration;

public sealed record AlcadaAprovacaoUpsertRequest(
    string? Nome,
    CriterioAlcada Criterio,
    decimal? ValorMinimo,
    decimal? ValorMaximo,
    Guid? CentroCustoMetadadoId,
    int Nivel,
    Guid? AprovadorUsuarioId,
    Guid? AprovadorPerfilId);

public sealed record AlcadaAprovacaoStatusRequest(bool Ativo);

/// <summary>O1.12 — Fundação de Administração de Alçadas de Aprovação (ADR-0020, revisão R1.1). CRUD
/// administrativo por Unidade de Negócio, sem exclusão física. <c>unidadeNegocioId</c> explícito no path,
/// nunca de claim de sessão do cliente — mesmo padrão de <see cref="ConfiguracaoNotificacaoController"/>.
/// Nenhum motor de avaliação/execução de aprovação é acionado por estes endpoints.</summary>
public static class AlcadasAprovacaoController
{
    public static IEndpointRouteBuilder MapAlcadasAprovacao(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup(PerfisController.BaseRoute)
            .WithTags("Administração — Alçadas de Aprovação")
            .RequireAuthorization(RbacPolicies.For(PermissaoCatalogo.AlcadaGerenciar))
            .AddEndpointFilter<CsrfHeaderFilter>()
            .AddEndpointFilter<EscopoUnidadeNegocioPathFilter>();

        group.MapGet("/unidades-negocio/{unidadeNegocioId:guid}/alcadas-aprovacao", Listar);
        group.MapPost("/unidades-negocio/{unidadeNegocioId:guid}/alcadas-aprovacao", Criar);
        group.MapPut("/unidades-negocio/{unidadeNegocioId:guid}/alcadas-aprovacao/{id:guid}", Atualizar);
        group.MapPatch("/unidades-negocio/{unidadeNegocioId:guid}/alcadas-aprovacao/{id:guid}/status", AlterarStatus);

        return endpoints;
    }

    private static async Task<IResult> Listar(Guid unidadeNegocioId, IListarAlcadasAprovacaoUseCase useCase, CancellationToken ct) =>
        Results.Ok(await useCase.ExecuteAsync(unidadeNegocioId, ct));

    private static async Task<IResult> Criar(
        Guid unidadeNegocioId, AlcadaAprovacaoUpsertRequest? request, ICriarAlcadaAprovacaoUseCase useCase, CancellationToken ct)
    {
        var resultado = await useCase.ExecuteAsync(ParaInput(request), unidadeNegocioId, ct);
        return resultado.Sucesso
            ? Results.Created($"{PerfisController.BaseRoute}/unidades-negocio/{unidadeNegocioId}/alcadas-aprovacao/{resultado.Valor!.Id}", resultado.Valor)
            : Traduzir(resultado);
    }

    private static async Task<IResult> Atualizar(
        Guid unidadeNegocioId, Guid id, AlcadaAprovacaoUpsertRequest? request, IAtualizarAlcadaAprovacaoUseCase useCase, CancellationToken ct)
    {
        var resultado = await useCase.ExecuteAsync(id, ParaInput(request), unidadeNegocioId, ct);
        return resultado.Sucesso ? Results.Ok(resultado.Valor) : Traduzir(resultado);
    }

    private static async Task<IResult> AlterarStatus(
        Guid unidadeNegocioId, Guid id, AlcadaAprovacaoStatusRequest? request, IAlterarStatusAlcadaAprovacaoUseCase useCase, CancellationToken ct)
    {
        if (request is null) return Results.BadRequest(new { code = "requisicao_invalida", message = "Informe o status desejado." });

        var resultado = await useCase.ExecuteAsync(id, request.Ativo, unidadeNegocioId, ct);
        return resultado.Sucesso ? Results.Ok(resultado.Valor) : Traduzir(resultado);
    }

    private static AlcadaAprovacaoInput ParaInput(AlcadaAprovacaoUpsertRequest? request) => new(
        request?.Nome ?? string.Empty,
        request?.Criterio ?? CriterioAlcada.Valor,
        request?.ValorMinimo,
        request?.ValorMaximo,
        request?.CentroCustoMetadadoId,
        request?.Nivel ?? 0,
        request?.AprovadorUsuarioId,
        request?.AprovadorPerfilId);

    private static IResult Traduzir<T>(RbacResultado<T> resultado) => resultado.Falha switch
    {
        RbacFalha.UnidadeNegocioNaoEncontrada => Results.NotFound(new { code = "unidade_negocio_nao_encontrada", message = resultado.Mensagem }),
        RbacFalha.AlcadaAprovacaoNaoEncontrada => Results.NotFound(new { code = "alcada_aprovacao_nao_encontrada", message = resultado.Mensagem }),
        RbacFalha.NomeObrigatorio => Results.BadRequest(new { code = "nome_obrigatorio", message = resultado.Mensagem }),
        RbacFalha.NivelInvalido => Results.BadRequest(new { code = "nivel_invalido", message = resultado.Mensagem }),
        RbacFalha.FaixaDeValorInvalida => Results.BadRequest(new { code = "faixa_de_valor_invalida", message = resultado.Mensagem }),
        RbacFalha.AprovadorInvalido => Results.BadRequest(new { code = "aprovador_invalido", message = resultado.Mensagem }),
        RbacFalha.CentroCustoObrigatorio => Results.BadRequest(new { code = "centro_custo_obrigatorio", message = resultado.Mensagem }),
        RbacFalha.CentroCustoInvalidoNaUnidadeDeNegocio => Results.BadRequest(new { code = "centro_custo_invalido", message = resultado.Mensagem }),
        _ => Results.BadRequest(new { code = "requisicao_invalida", message = resultado.Mensagem }),
    };
}
