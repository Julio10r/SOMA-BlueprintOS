using BlueprintOS.Api.Auth;
using BlueprintOS.Api.Authorization;
using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Domain.Identity;

namespace BlueprintOS.Api.Administration;

public sealed record ItemFiscalReferenciaFornecedorCriarRequest(Guid FornecedorId, string? CodigoItemFornecedor);

public sealed record ItemFiscalReferenciaFornecedorAtualizarRequest(string? CodigoItemFornecedor);

/// <summary>Endpoints reais das Referências de Item Fiscal por Fornecedor (B3 — Bloco 4, Discovery
/// homologado, espelho local de <c>ITEM_FISCAL_REF_FORNECEDOR</c>). Sub-recurso de
/// <c>ItensFiscaisController</c> — sempre sob <c>/itens-fiscais/{itemFiscalId}</c>.
///
/// RBAC: reaproveita as permissões já existentes do Item Fiscal (nenhuma nova permissão criada) — consultar
/// exige <c>ItemFiscal.Visualizar</c>, incluir/editar/remover exigem <c>ItemFiscal.Editar</c> (gerenciar as
/// referências de um Item Fiscal é parte de editá-lo, não uma ação distinta o bastante para justificar
/// permissão própria).
///
/// Bloco 4 é exclusivamente local: sem integração com o Linx (leitura ou escrita) nesta etapa — a
/// sincronização é escopo do Bloco 5A/5B, ainda não iniciado. Remoção é FÍSICA (não inativação lógica),
/// espelhando a ausência de coluna de status comprovada em <c>ITEM_FISCAL_REF_FORNECEDOR</c>.</summary>
public static class ItemFiscalReferenciasFornecedorController
{
    public static IEndpointRouteBuilder MapItemFiscalReferenciasFornecedor(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup(PerfisController.BaseRoute)
            .WithTags("Cadastros — Item Fiscal — Referências por Fornecedor")
            .AddEndpointFilter<CsrfHeaderFilter>();

        group.MapGet("/itens-fiscais/{itemFiscalId:guid}/referencias-fornecedor", Listar)
            .RequireAuthorization(RbacPolicies.For(PermissaoCatalogo.ItemFiscalVisualizar));
        group.MapPost("/itens-fiscais/{itemFiscalId:guid}/referencias-fornecedor", Incluir)
            .RequireAuthorization(RbacPolicies.For(PermissaoCatalogo.ItemFiscalEditar));
        group.MapPut("/itens-fiscais/{itemFiscalId:guid}/referencias-fornecedor/{id:guid}", Atualizar)
            .RequireAuthorization(RbacPolicies.For(PermissaoCatalogo.ItemFiscalEditar));
        group.MapDelete("/itens-fiscais/{itemFiscalId:guid}/referencias-fornecedor/{id:guid}", Remover)
            .RequireAuthorization(RbacPolicies.For(PermissaoCatalogo.ItemFiscalEditar));

        return endpoints;
    }

    private static async Task<IResult> Listar(
        Guid itemFiscalId, ICurrentIdentity identity, IListarReferenciasFornecedorUseCase useCase, CancellationToken ct)
    {
        if (!TryResolverUnidadeNegocio(identity, out var unidadeNegocioId, out var falha)) return falha!;

        var resultado = await useCase.ExecuteAsync(itemFiscalId, unidadeNegocioId, ct);
        return resultado.Sucesso ? Results.Ok(resultado.Valor) : Traduzir(resultado);
    }

    private static async Task<IResult> Incluir(
        Guid itemFiscalId, ItemFiscalReferenciaFornecedorCriarRequest? request, ICurrentIdentity identity,
        IIncluirReferenciaFornecedorUseCase useCase, CancellationToken ct)
    {
        if (!TryResolverUnidadeNegocio(identity, out var unidadeNegocioId, out var falha)) return falha!;

        var input = new ItemFiscalReferenciaFornecedorCriarInput(request?.FornecedorId ?? Guid.Empty, request?.CodigoItemFornecedor ?? string.Empty);
        var resultado = await useCase.ExecuteAsync(itemFiscalId, input, unidadeNegocioId, ct);
        return resultado.Sucesso
            ? Results.Created($"{PerfisController.BaseRoute}/itens-fiscais/{itemFiscalId}/referencias-fornecedor/{resultado.Valor!.Id}", resultado.Valor)
            : Traduzir(resultado);
    }

    private static async Task<IResult> Atualizar(
        Guid itemFiscalId, Guid id, ItemFiscalReferenciaFornecedorAtualizarRequest? request, ICurrentIdentity identity,
        IAtualizarReferenciaFornecedorUseCase useCase, CancellationToken ct)
    {
        if (!TryResolverUnidadeNegocio(identity, out var unidadeNegocioId, out var falha)) return falha!;

        var input = new ItemFiscalReferenciaFornecedorAtualizarInput(request?.CodigoItemFornecedor ?? string.Empty);
        var resultado = await useCase.ExecuteAsync(itemFiscalId, id, input, unidadeNegocioId, ct);
        return resultado.Sucesso ? Results.Ok(resultado.Valor) : Traduzir(resultado);
    }

    private static async Task<IResult> Remover(
        Guid itemFiscalId, Guid id, ICurrentIdentity identity, IRemoverReferenciaFornecedorUseCase useCase, CancellationToken ct)
    {
        if (!TryResolverUnidadeNegocio(identity, out var unidadeNegocioId, out var falha)) return falha!;

        var resultado = await useCase.ExecuteAsync(itemFiscalId, id, unidadeNegocioId, ct);
        return resultado.Sucesso ? Results.NoContent() : Traduzir(resultado);
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
        RbacFalha.ItemFiscalReferenciaFornecedorNaoEncontrada => Results.NotFound(new { code = "referencia_fornecedor_nao_encontrada", message = resultado.Mensagem }),
        RbacFalha.FornecedorObrigatorio => Results.BadRequest(new { code = "fornecedor_obrigatorio", message = resultado.Mensagem }),
        RbacFalha.FornecedorNaoEncontrado => Results.BadRequest(new { code = "fornecedor_nao_encontrado", message = resultado.Mensagem }),
        RbacFalha.FornecedorInvalidoOuInativo => Results.BadRequest(new { code = "fornecedor_invalido", message = resultado.Mensagem }),
        RbacFalha.CodigoItemFornecedorObrigatorio => Results.BadRequest(new { code = "codigo_item_fornecedor_obrigatorio", message = resultado.Mensagem }),
        RbacFalha.ReferenciaJaExistenteParaFornecedor => Results.Conflict(new { code = "referencia_ja_existente", message = resultado.Mensagem }),
        RbacFalha.CodigoItemFornecedorDuplicadoParaFornecedor => Results.Conflict(new { code = "codigo_item_fornecedor_duplicado", message = resultado.Mensagem }),
        _ => Results.BadRequest(new { code = "requisicao_invalida", message = resultado.Mensagem }),
    };
}
