using BlueprintOS.Api.Auth;
using BlueprintOS.Api.Authorization;
using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Domain.Identity;

namespace BlueprintOS.Api.Administration;

public sealed record ConfiguracaoNotificacaoRequest(bool EmailAtivado, string? EmailRemetente, string? NomeRemetente);

/// <summary>O1.11, item #24 — Configuração de Notificações por Unidade de Negócio (relação 1:1). ESCOPO
/// MÍNIMO DE FUNDAÇÃO aprovado pelo Product Owner: apenas registro de configuração administrativa do canal
/// e-mail (ativado/inativado, remetente, nome do remetente). Nenhum envio real de e-mail/SMTP/fila/worker
/// acontece por meio destes endpoints. <c>unidadeNegocioId</c> explícito no path (recurso administrado),
/// protegido pela permissão corporativa <c>Sistema.Gerenciar</c> (mesma usada por Identity
/// Providers/Parâmetros/Feature Flags) — nunca confia em um <c>UnidadeNegocioId</c> de sessão do
/// cliente.</summary>
public static class ConfiguracaoNotificacaoController
{
    public static IEndpointRouteBuilder MapConfiguracaoNotificacao(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup(PerfisController.BaseRoute)
            .WithTags("Administração — Configuração de Notificações")
            .RequireAuthorization(RbacPolicies.For(PermissaoCatalogo.SistemaGerenciar))
            .AddEndpointFilter<CsrfHeaderFilter>();

        group.MapGet("/unidades-negocio/{unidadeNegocioId:guid}/configuracao-notificacao", Obter);
        group.MapPut("/unidades-negocio/{unidadeNegocioId:guid}/configuracao-notificacao", Salvar);

        return endpoints;
    }

    private static async Task<IResult> Obter(Guid unidadeNegocioId, IObterConfiguracaoNotificacaoUseCase useCase, CancellationToken ct)
    {
        var resultado = await useCase.ExecuteAsync(unidadeNegocioId, ct);
        if (!resultado.Sucesso) return Traduzir(resultado);
        return resultado.Valor is null
            ? Results.NotFound(new { code = "configuracao_notificacao_nao_encontrada", message = "Configuração de Notificações não encontrada para esta Unidade de Negócio." })
            : Results.Ok(resultado.Valor);
    }

    private static async Task<IResult> Salvar(
        Guid unidadeNegocioId, ConfiguracaoNotificacaoRequest? request, ISalvarConfiguracaoNotificacaoUseCase useCase, CancellationToken ct)
    {
        var input = new ConfiguracaoNotificacaoInput(request?.EmailAtivado ?? false, request?.EmailRemetente, request?.NomeRemetente);
        var resultado = await useCase.ExecuteAsync(unidadeNegocioId, input, ct);
        return resultado.Sucesso ? Results.Ok(resultado.Valor) : Traduzir(resultado);
    }

    private static IResult Traduzir<T>(RbacResultado<T> resultado) => resultado.Falha switch
    {
        RbacFalha.UnidadeNegocioNaoEncontrada => Results.NotFound(new { code = "unidade_negocio_nao_encontrada", message = resultado.Mensagem }),
        RbacFalha.ConfiguracaoNotificacaoNaoEncontrada => Results.NotFound(new { code = "configuracao_notificacao_nao_encontrada", message = resultado.Mensagem }),
        RbacFalha.EmailRemetenteInvalido => Results.BadRequest(new { code = "email_remetente_invalido", message = resultado.Mensagem }),
        _ => Results.BadRequest(new { code = "requisicao_invalida", message = resultado.Mensagem }),
    };
}
