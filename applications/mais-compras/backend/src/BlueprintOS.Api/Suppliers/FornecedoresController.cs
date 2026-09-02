using BlueprintOS.Api.Authorization;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Application.Procurement.Suppliers;
using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using BlueprintOS.Domain.Identity;

namespace BlueprintOS.Api.Suppliers;

/// <summary>Endpoints REST do cadastro persistente de fornecedores. RBAC granular (O1.13, quitando a
/// dívida técnica de O1.5): escrita exige <c>Fornecedor.Criar</c>/<c>Fornecedor.Editar</c>, decisões de
/// enriquecimento exigem <c>Fornecedor.Aprovar</c>. Leitura (GET) permanece apenas autenticada, mesmo
/// padrão dos demais módulos de leitura.</summary>
public static class FornecedoresController
{
    public static IEndpointRouteBuilder MapFornecedores(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/fornecedores").WithTags("Fornecedores");
        group.MapPost("", Create).RequireAuthorization(RbacPolicies.For(PermissaoCatalogo.FornecedorCriar));
        group.MapGet("", Search);
        group.MapPost("/consulta-cnpj", ConsultCnpj).RequireAuthorization(RbacPolicies.For(PermissaoCatalogo.FornecedorCriar));
        // Gate de homologação de Fornecedores (2026-09-01), item 6 — consulta de CEP pelo backend
        // (nunca chamada externa direta do frontend), mesma exigência de RBAC do consulta-cnpj: quem
        // pode consultar CNPJ (fluxo de criação) também pode consultar CEP no formulário de cadastro.
        group.MapPost("/consulta-cep", ConsultCep).RequireAuthorization(RbacPolicies.For(PermissaoCatalogo.FornecedorCriar));
        // Gate de homologação de Fornecedores (2026-09-01) — cidade como combo dependente da UF.
        group.MapGet("/municipios", ListarMunicipios).RequireAuthorization(RbacPolicies.For(PermissaoCatalogo.FornecedorCriar));
        // Gate de homologação de Fornecedores (2026-09-01) — catálogo pré-cadastrado de Categoria.
        group.MapGet("/categorias", ListarCategorias).RequireAuthorization(RbacPolicies.For(PermissaoCatalogo.FornecedorCriar));
        group.MapGet("/{id:guid}", GetById);
        group.MapPut("/{id:guid}", Update).RequireAuthorization(RbacPolicies.For(PermissaoCatalogo.FornecedorEditar));
        group.MapDelete("/{id:guid}", Delete).RequireAuthorization(RbacPolicies.For(PermissaoCatalogo.FornecedorEditar));
        // Rota semântica que substitui o uso de DELETE para expressar ativação/inativação — mantém o
        // DELETE acima funcionando por compatibilidade (não remove, apenas roteia para o mesmo mecanismo).
        group.MapPatch("/{id:guid}/status", AlterarStatus).RequireAuthorization(RbacPolicies.For(PermissaoCatalogo.FornecedorEditar));
        group.MapPost("/{id:guid}/enriquecimento-cnpj", AnalyzeCnpjEnrichment)
            .RequireAuthorization(RbacPolicies.For(PermissaoCatalogo.FornecedorEditar));
        group.MapPost("/{id:guid}/enriquecimento-cnpj/aprovar", ApproveCnpjEnrichment)
            .RequireAuthorization(RbacPolicies.For(PermissaoCatalogo.FornecedorAprovar));
        group.MapPost("/{id:guid}/enriquecimento-cnpj/rejeitar", RejectCnpjEnrichment)
            .RequireAuthorization(RbacPolicies.For(PermissaoCatalogo.FornecedorAprovar));
        return endpoints;
    }

    private static async Task<IResult> Create(FornecedorRequest? request, ICadastrarFornecedorUseCase useCase, CancellationToken ct)
    {
        if (request is null) return Results.BadRequest(new { code = "invalid_request", message = "Request body is required." });
        try
        {
            var supplier = await useCase.ExecuteAsync(request.ToCreateDto(), ct);
            return Results.Created($"/fornecedores/{supplier.Id}", supplier);
        }
        catch (ArgumentException ex) { return Results.BadRequest(new { code = "validation_error", message = ex.Message }); }
        catch (FornecedorJaExisteNoErpException ex)
        {
            // Gate de homologação (2026-09-01): CNPJ/CPF já existe como Fornecedor no Linx — nunca
            // duplicar. O frontend usa fornecedorId para abrir diretamente a tela de detalhe.
            return Results.Conflict(new
            {
                code = "ja_existe_no_erp",
                fornecedorId = ex.FornecedorId,
                message = "Este fornecedor já está cadastrado no Linx. Os dados existentes serão exibidos."
            });
        }
        catch (InvalidOperationException ex) { return Results.Conflict(new { code = "duplicate_cnpj", message = ex.Message }); }
        catch (IdentityUnavailableException) { return Results.StatusCode(StatusCodes.Status503ServiceUnavailable); }
    }

    // Pesquisa paginada, filtrável por status e ordenável (O1.x, redesenho da tela de Fornecedores).
    // Leitura (GET) permanece apenas autenticada, mesmo padrão dos demais módulos de leitura — não existe
    // hoje uma permissão granular "Fornecedor.Visualizar" no catálogo de RBAC (PermissaoCatalogo).
    // TODO: se um dia a leitura de Fornecedores precisar de RBAC granular, adicionar
    // Fornecedor.Visualizar ao catálogo e trocar RequireAuthorization aqui.
    private static async Task<IResult> Search(string? q, string? status, string? sort, int? page, int? pageSize,
        IPesquisarFornecedorPaginadoUseCase useCase, CancellationToken ct) =>
        Results.Ok(await useCase.ExecuteAsync(new PesquisarFornecedorPaginadoParametros(q, status, sort, page ?? 1, pageSize ?? 20), ct));
    private static async Task<IResult> ConsultCnpj(FornecedorConsultaCnpjRequest? request, IConsultarCnpjFornecedorUseCase useCase, CancellationToken ct)
    {
        if (request is null) return Results.BadRequest(new { code = "invalid_request", message = "Request body is required." });
        try { return Results.Ok(await useCase.ExecuteAsync(request.ToDto(), ct)); }
        catch (ArgumentException ex) { return Results.BadRequest(new { code = "validation_error", message = ex.Message }); }
    }

    private static async Task<IResult> ConsultCep(FornecedorConsultaCepRequest? request, IConsultarCepFornecedorUseCase useCase, CancellationToken ct)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Cep))
            return Results.BadRequest(new { code = "validation_error", message = "Cep é obrigatório." });
        return Results.Ok(await useCase.ExecuteAsync(new(request.Cep), ct));
    }

    private static async Task<IResult> ListarMunicipios(string? uf, IListarMunicipiosPorUfUseCase useCase, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(uf)) return Results.BadRequest(new { code = "validation_error", message = "Uf é obrigatória." });
        return Results.Ok(await useCase.ExecuteAsync(uf, ct));
    }

    private static async Task<IResult> ListarCategorias(IListarCategoriasFornecedorUseCase useCase, CancellationToken ct) =>
        Results.Ok(await useCase.ExecuteAsync(ct));

    private static async Task<IResult> GetById(Guid id, IObterFornecedorUseCase useCase, CancellationToken ct) =>
        await useCase.ExecuteAsync(id, ct) is { } supplier ? Results.Ok(supplier) : Results.NotFound();
    private static async Task<IResult> Update(Guid id, AtualizarFornecedorRequest? request, IAtualizarFornecedorUseCase useCase, CancellationToken ct)
    {
        if (request is null) return Results.BadRequest(new { code = "invalid_request", message = "Request body is required." });
        try { return await useCase.ExecuteAsync(id, request.ToDto(), ct) is { } supplier ? Results.Ok(supplier) : Results.NotFound(); }
        catch (ArgumentException ex) { return Results.BadRequest(new { code = "validation_error", message = ex.Message }); }
    }
    private static async Task<IResult> Delete(Guid id, IInativarFornecedorUseCase useCase, CancellationToken ct) =>
        await useCase.ExecuteAsync(id, ct) ? Results.NoContent() : Results.NotFound();

    private static async Task<IResult> AlterarStatus(Guid id, FornecedorAlterarStatusRequest? request,
        IAlterarStatusFornecedorUseCase useCase, CancellationToken ct)
    {
        if (request is null) return Results.BadRequest(new { code = "invalid_request", message = "Request body is required." });
        return await useCase.ExecuteAsync(id, request.Ativo, ct) is { } supplier ? Results.Ok(supplier) : Results.NotFound();
    }

    private static async Task<IResult> AnalyzeCnpjEnrichment(Guid id, FornecedorEnriquecimentoRequest? request,
        IAnalisarEnriquecimentoFornecedorUseCase useCase, CancellationToken ct)
    {
        if (request is null) return Results.BadRequest(new { code = "invalid_request", message = "Request body is required." });
        try { return await useCase.ExecuteAsync(id, request.ToDto(), ct) is { } result ? Results.Ok(result) : Results.NotFound(); }
        catch (ArgumentException ex) { return Results.BadRequest(new { code = "validation_error", message = ex.Message }); }
    }

    private static async Task<IResult> ApproveCnpjEnrichment(Guid id, FornecedorEnriquecimentoDecisaoRequest? request,
        IAprovarEnriquecimentoFornecedorUseCase useCase, CancellationToken ct)
    {
        if (request is null) return Results.BadRequest(new { code = "invalid_request", message = "Request body is required." });
        try { return await useCase.ExecuteAsync(id, request.ToDto(), ct) is { } result ? Results.Ok(result) : Results.NotFound(); }
        catch (ArgumentException ex) { return Results.BadRequest(new { code = "validation_error", message = ex.Message }); }
    }

    private static async Task<IResult> RejectCnpjEnrichment(Guid id, FornecedorEnriquecimentoDecisaoRequest? request,
        IRejeitarEnriquecimentoFornecedorUseCase useCase, CancellationToken ct)
    {
        if (request is null) return Results.BadRequest(new { code = "invalid_request", message = "Request body is required." });
        try { return await useCase.ExecuteAsync(id, request.ToDto(), ct) is { } result ? Results.Ok(result) : Results.NotFound(); }
        catch (ArgumentException ex) { return Results.BadRequest(new { code = "validation_error", message = ex.Message }); }
    }
}
