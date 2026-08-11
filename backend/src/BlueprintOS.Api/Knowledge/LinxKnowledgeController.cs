using BlueprintOS.Api.Auth;
using BlueprintOS.Api.Authorization;
using BlueprintOS.Api.Administration;
using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Application.Knowledge.Linx.Contracts;
using BlueprintOS.Application.Knowledge.Linx.Models;
using BlueprintOS.Domain.Identity;
using BlueprintOS.Domain.Knowledge.Linx;

namespace BlueprintOS.Api.Knowledge;

public sealed record RegistrarConhecimentoRequest(
    LinxEspecialista? Especialista,
    LinxConhecimentoCategoria? Categoria,
    string? Assunto,
    string? Conteudo,
    LinxConhecimentoProveniencia? Proveniencia,
    string? Fonte,
    Guid? UnidadeNegocioId,
    IReadOnlyList<string>? Tags,
    Guid? VersaoRaizId);

/// <summary>Base de conhecimento persistente dos Agents Especialistas Linx (Work Order O1.13.5). A busca
/// (GET) exige apenas autenticação — qualquer usuário autenticado pode consultar conhecimento já
/// persistido, incluindo Agents. Registrar/validar exige <c>ConhecimentoLinx.Gerenciar</c>. A promoção
/// final e sensível a "Aprovado" exige a permissão DEDICADA <c>ConhecimentoLinx.Aprovar</c> (Work Order,
/// seção 18) — nunca a mesma permissão de quem registrou/inferiu.</summary>
public static class LinxKnowledgeController
{
    public static IEndpointRouteBuilder MapLinxKnowledge(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup($"{PerfisController.BaseRoute}/conhecimento-linx")
            .WithTags("Administração — Conhecimento Linx")
            .RequireAuthorization()
            .AddEndpointFilter<CsrfHeaderFilter>();

        group.MapGet("/", Buscar);
        group.MapGet("/{versaoRaizId:guid}/historico", ObterHistorico);

        group.MapPost("/", Registrar)
            .RequireAuthorization(RbacPolicies.For(PermissaoCatalogo.ConhecimentoLinxGerenciar));

        group.MapPost("/{id:guid}/validar", Validar)
            .RequireAuthorization(RbacPolicies.For(PermissaoCatalogo.ConhecimentoLinxGerenciar));

        group.MapPost("/{id:guid}/aprovar", Aprovar)
            .RequireAuthorization(RbacPolicies.For(PermissaoCatalogo.ConhecimentoLinxAprovar));

        return endpoints;
    }

    private static async Task<IResult> Buscar(
        string? texto, LinxEspecialista? especialista, LinxConhecimentoCategoria? categoria,
        LinxConhecimentoProveniencia? provenienciaMinima, string? tags,
        ICurrentIdentity identity, IBuscarConhecimentoUseCase useCase, CancellationToken ct)
    {
        var filtro = new LinxKnowledgeFiltro(
            texto, especialista, categoria, provenienciaMinima,
            identity.GetRequired().UnidadeNegocioId,
            tags?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        return Results.Ok(await useCase.ExecuteAsync(filtro, ct));
    }

    private static async Task<IResult> ObterHistorico(
        Guid versaoRaizId, IObterHistoricoConhecimentoUseCase useCase, CancellationToken ct)
    {
        var resultado = await useCase.ExecuteAsync(versaoRaizId, ct);
        return resultado.Sucesso
            ? Results.Ok(resultado.Valor)
            : Results.NotFound(new { code = "conhecimento_nao_encontrado", message = resultado.Mensagem });
    }

    private static async Task<IResult> Registrar(
        RegistrarConhecimentoRequest? request, ICurrentIdentity identity,
        IRegistrarConhecimentoUseCase useCase, CancellationToken ct)
    {
        if (request is null || request.Especialista is null || request.Categoria is null || request.Proveniencia is null)
        {
            return Results.BadRequest(new { code = "requisicao_invalida", message = "Especialista, categoria e proveniência são obrigatórios." });
        }

        var input = new RegistrarConhecimentoInput(
            request.Especialista.Value, request.Categoria.Value, request.Assunto ?? string.Empty,
            request.Conteudo ?? string.Empty, request.Proveniencia.Value, request.Fonte ?? string.Empty,
            request.UnidadeNegocioId, request.Tags, request.VersaoRaizId);

        var ator = identity.GetRequired().UserId.ToString();
        var resultado = await useCase.ExecuteAsync(input, ator, ct);
        return resultado.Sucesso ? Results.Created($"{PerfisController.BaseRoute}/conhecimento-linx/{resultado.Valor!.Id}", resultado.Valor) : Traduzir(resultado);
    }

    private static async Task<IResult> Validar(Guid id, ICurrentIdentity identity, IPromoverConhecimentoUseCase useCase, CancellationToken ct) =>
        await Promover(id, LinxConhecimentoProveniencia.Validado, identity, useCase, ct);

    private static async Task<IResult> Aprovar(Guid id, ICurrentIdentity identity, IPromoverConhecimentoUseCase useCase, CancellationToken ct) =>
        await Promover(id, LinxConhecimentoProveniencia.Aprovado, identity, useCase, ct);

    private static async Task<IResult> Promover(
        Guid id, LinxConhecimentoProveniencia novaProveniencia, ICurrentIdentity identity, IPromoverConhecimentoUseCase useCase, CancellationToken ct)
    {
        var ator = identity.GetRequired().UserId.ToString();
        var resultado = await useCase.ExecuteAsync(id, novaProveniencia, ator, ct);
        return resultado.Sucesso ? Results.Ok(resultado.Valor) : Traduzir(resultado);
    }

    private static IResult Traduzir<T>(RbacResultado<T> resultado) => resultado.Falha switch
    {
        RbacFalha.ConhecimentoLinxNaoEncontrado => Results.NotFound(new { code = "conhecimento_nao_encontrado", message = resultado.Mensagem }),
        RbacFalha.AssuntoObrigatorio => Results.BadRequest(new { code = "assunto_obrigatorio", message = resultado.Mensagem }),
        RbacFalha.ConteudoObrigatorio => Results.BadRequest(new { code = "conteudo_obrigatorio", message = resultado.Mensagem }),
        RbacFalha.FonteObrigatoria => Results.BadRequest(new { code = "fonte_obrigatoria", message = resultado.Mensagem }),
        RbacFalha.TransicaoProvenienciaInvalida => Results.Conflict(new { code = "transicao_invalida", message = resultado.Mensagem }),
        RbacFalha.ConflitoDeConhecimentoDetectado => Results.Conflict(new { code = "conflito_de_conhecimento", message = resultado.Mensagem }),
        _ => Results.BadRequest(new { code = "requisicao_invalida", message = resultado.Mensagem }),
    };
}
