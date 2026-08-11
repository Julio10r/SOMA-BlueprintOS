using BlueprintOS.Api.Auth;
using BlueprintOS.Api.Authorization;
using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Domain.Identity;

namespace BlueprintOS.Api.Administration;

public sealed record ConfiguracaoErpRequest(string? SistemaErp, string? ParametrosConexao);

public sealed record ConfiguracaoErpStatusRequest(bool Ativo);

/// <summary>O1.11 — Configuração de ERP por Unidade de Negócio (relação 1:1). <c>unidadeNegocioId</c>
/// explícito no path, protegido pela permissão corporativa <c>ConfiguracaoErp.Gerenciar</c>. PURAMENTE
/// registro de configuração: nenhuma operação de leitura/escrita real no ERP acontece por meio destes
/// endpoints — os leitores de Filial/Centro de Custo (`Infrastructure/Integrations/ERP`), já existentes,
/// permanecem a única fonte real de integração. Parâmetros de conexão nunca em claro no banco/log/resposta
/// — apenas <c>parametrosConfigurados: bool</c>.</summary>
public static class ConfiguracaoErpController
{
    public static IEndpointRouteBuilder MapConfiguracaoErp(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup(PerfisController.BaseRoute)
            .WithTags("Administração — Configuração de ERP")
            .RequireAuthorization(RbacPolicies.For(PermissaoCatalogo.ConfiguracaoErpGerenciar))
            .AddEndpointFilter<CsrfHeaderFilter>();

        group.MapGet("/unidades-negocio/{unidadeNegocioId:guid}/configuracao-erp", Obter);
        group.MapPut("/unidades-negocio/{unidadeNegocioId:guid}/configuracao-erp", Salvar);
        group.MapPatch("/unidades-negocio/{unidadeNegocioId:guid}/configuracao-erp/status", AlterarStatus);

        return endpoints;
    }

    private static async Task<IResult> Obter(Guid unidadeNegocioId, IObterConfiguracaoErpUseCase useCase, CancellationToken ct)
    {
        var resultado = await useCase.ExecuteAsync(unidadeNegocioId, ct);
        if (!resultado.Sucesso) return Traduzir(resultado);
        return resultado.Valor is null
            ? Results.NotFound(new { code = "configuracao_erp_nao_encontrada", message = "Configuração de ERP não encontrada para esta Unidade de Negócio." })
            : Results.Ok(resultado.Valor);
    }

    private static async Task<IResult> Salvar(
        Guid unidadeNegocioId, ConfiguracaoErpRequest? request, ISalvarConfiguracaoErpUseCase useCase, CancellationToken ct)
    {
        var input = new ConfiguracaoErpInput(request?.SistemaErp ?? string.Empty, request?.ParametrosConexao);
        var resultado = await useCase.ExecuteAsync(unidadeNegocioId, input, ct);
        return resultado.Sucesso ? Results.Ok(resultado.Valor) : Traduzir(resultado);
    }

    private static async Task<IResult> AlterarStatus(
        Guid unidadeNegocioId, ConfiguracaoErpStatusRequest? request, IAlterarStatusConfiguracaoErpUseCase useCase, CancellationToken ct)
    {
        if (request is null) return Results.BadRequest(new { code = "requisicao_invalida", message = "Informe o status desejado." });

        var resultado = await useCase.ExecuteAsync(unidadeNegocioId, request.Ativo, ct);
        return resultado.Sucesso ? Results.Ok(resultado.Valor) : Traduzir(resultado);
    }

    private static IResult Traduzir<T>(RbacResultado<T> resultado) => resultado.Falha switch
    {
        RbacFalha.UnidadeNegocioNaoEncontrada => Results.NotFound(new { code = "unidade_negocio_nao_encontrada", message = resultado.Mensagem }),
        RbacFalha.ConfiguracaoErpNaoEncontrada => Results.NotFound(new { code = "configuracao_erp_nao_encontrada", message = resultado.Mensagem }),
        RbacFalha.SistemaErpObrigatorio => Results.BadRequest(new { code = "sistema_erp_obrigatorio", message = resultado.Mensagem }),
        _ => Results.BadRequest(new { code = "requisicao_invalida", message = resultado.Mensagem }),
    };
}
