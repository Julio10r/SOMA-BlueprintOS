using BlueprintOS.Api.Auth;
using BlueprintOS.Api.Authorization;
using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Domain.Identity;

namespace BlueprintOS.Api.Administration;

public sealed record ItemFiscalCriarRequest(string? Codigo, string? Descricao, string? UnidadeMedidaCodigoErp, string? ContaContabilCodigoErp);

public sealed record ItemFiscalAtualizarRequest(string? Descricao, string? UnidadeMedidaCodigoErp, string? ContaContabilCodigoErp);

public sealed record ItemFiscalStatusRequest(bool Ativo);

/// <summary>Endpoints reais do cadastro local de Item Fiscal (B3 — Bloco 3, Discovery homologado). RBAC
/// granular por ação (mesmo padrão de `FornecedoresController`): consultar exige
/// <c>ItemFiscal.Visualizar</c>, cadastrar exige <c>ItemFiscal.Criar</c>, editar exige
/// <c>ItemFiscal.Editar</c>, ativar/inativar exige <c>ItemFiscal.Inativar</c> — nunca presumir que uma
/// permissão cobre as demais ações.
///
/// Bloco 3 é exclusivamente local: sem integração com o Linx (leitura ou escrita) nesta etapa — a
/// sincronização é escopo do Bloco 5, ainda não iniciado.</summary>
public static class ItensFiscaisController
{
    public static IEndpointRouteBuilder MapItensFiscais(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup(PerfisController.BaseRoute)
            .WithTags("Cadastros — Item Fiscal")
            .AddEndpointFilter<CsrfHeaderFilter>();

        group.MapGet("/itens-fiscais", ListarItensFiscais)
            .RequireAuthorization(RbacPolicies.For(PermissaoCatalogo.ItemFiscalVisualizar));
        group.MapGet("/itens-fiscais/{id:guid}", ObterItemFiscal)
            .RequireAuthorization(RbacPolicies.For(PermissaoCatalogo.ItemFiscalVisualizar));
        group.MapPost("/itens-fiscais", CriarItemFiscal)
            .RequireAuthorization(RbacPolicies.For(PermissaoCatalogo.ItemFiscalCriar));
        group.MapPut("/itens-fiscais/{id:guid}", AtualizarItemFiscal)
            .RequireAuthorization(RbacPolicies.For(PermissaoCatalogo.ItemFiscalEditar));
        group.MapPatch("/itens-fiscais/{id:guid}/status", AlterarStatus)
            .RequireAuthorization(RbacPolicies.For(PermissaoCatalogo.ItemFiscalInativar));

        return endpoints;
    }

    private static async Task<IResult> ListarItensFiscais(
        ICurrentIdentity identity, IListarItensFiscaisUseCase useCase, CancellationToken ct)
    {
        if (!TryResolverUnidadeNegocio(identity, out var unidadeNegocioId, out var falha)) return falha!;
        return Results.Ok(await useCase.ExecuteAsync(unidadeNegocioId, ct));
    }

    private static async Task<IResult> ObterItemFiscal(
        Guid id, ICurrentIdentity identity, IObterItemFiscalUseCase useCase, CancellationToken ct)
    {
        if (!TryResolverUnidadeNegocio(identity, out var unidadeNegocioId, out var falha)) return falha!;

        var item = await useCase.ExecuteAsync(id, unidadeNegocioId, ct);
        return item is null
            ? Results.NotFound(new { code = "item_fiscal_nao_encontrado", message = "Item Fiscal não encontrado." })
            : Results.Ok(item);
    }

    private static async Task<IResult> CriarItemFiscal(
        ItemFiscalCriarRequest? request, ICurrentIdentity identity, ICriarItemFiscalUseCase useCase, CancellationToken ct)
    {
        if (!TryResolverUnidadeNegocio(identity, out var unidadeNegocioId, out var falha)) return falha!;

        var input = new ItemFiscalCriarInput(
            request?.Codigo ?? string.Empty,
            request?.Descricao ?? string.Empty,
            request?.UnidadeMedidaCodigoErp ?? string.Empty,
            request?.ContaContabilCodigoErp ?? string.Empty);
        var resultado = await useCase.ExecuteAsync(input, unidadeNegocioId, ct);
        return resultado.Sucesso
            ? Results.Created($"{PerfisController.BaseRoute}/itens-fiscais/{resultado.Valor!.Id}", resultado.Valor)
            : Traduzir(resultado);
    }

    private static async Task<IResult> AtualizarItemFiscal(
        Guid id, ItemFiscalAtualizarRequest? request, ICurrentIdentity identity, IAtualizarItemFiscalUseCase useCase, CancellationToken ct)
    {
        if (!TryResolverUnidadeNegocio(identity, out var unidadeNegocioId, out var falha)) return falha!;

        var input = new ItemFiscalAtualizarInput(
            request?.Descricao ?? string.Empty,
            request?.UnidadeMedidaCodigoErp ?? string.Empty,
            request?.ContaContabilCodigoErp ?? string.Empty);
        var resultado = await useCase.ExecuteAsync(id, input, unidadeNegocioId, ct);
        return resultado.Sucesso ? Results.Ok(resultado.Valor) : Traduzir(resultado);
    }

    private static async Task<IResult> AlterarStatus(
        Guid id, ItemFiscalStatusRequest? request, ICurrentIdentity identity, IAlterarStatusItemFiscalUseCase useCase, CancellationToken ct)
    {
        if (!TryResolverUnidadeNegocio(identity, out var unidadeNegocioId, out var falha)) return falha!;
        if (request is null) return Results.BadRequest(new { code = "invalid_request", message = "Informe o status desejado." });

        var resultado = await useCase.ExecuteAsync(id, request.Ativo, unidadeNegocioId, ct);
        return resultado.Sucesso ? Results.Ok(resultado.Valor) : Traduzir(resultado);
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

    private static IResult Traduzir<T>(RbacResultado<T> resultado) => resultado.Falha switch
    {
        RbacFalha.ItemFiscalNaoEncontrado => Results.NotFound(new { code = "item_fiscal_nao_encontrado", message = resultado.Mensagem }),
        RbacFalha.CodigoDuplicado => Results.Conflict(new { code = "codigo_duplicado", message = resultado.Mensagem }),
        RbacFalha.CodigoObrigatorio => Results.BadRequest(new { code = "codigo_obrigatorio", message = resultado.Mensagem }),
        RbacFalha.DescricaoObrigatoria => Results.BadRequest(new { code = "descricao_obrigatoria", message = resultado.Mensagem }),
        RbacFalha.ContaContabilObrigatoria => Results.BadRequest(new { code = "conta_contabil_obrigatoria", message = resultado.Mensagem }),
        RbacFalha.ContaContabilInvalidaOuInativa => Results.BadRequest(new { code = "conta_contabil_invalida", message = resultado.Mensagem }),
        RbacFalha.UnidadeMedidaObrigatoria => Results.BadRequest(new { code = "unidade_medida_obrigatoria", message = resultado.Mensagem }),
        RbacFalha.UnidadeMedidaInvalidaOuInativa => Results.BadRequest(new { code = "unidade_medida_invalida", message = resultado.Mensagem }),
        _ => Results.BadRequest(new { code = "requisicao_invalida", message = resultado.Mensagem }),
    };
}
