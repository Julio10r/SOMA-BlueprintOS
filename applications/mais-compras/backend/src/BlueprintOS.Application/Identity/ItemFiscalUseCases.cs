using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Domain.Identity;

namespace BlueprintOS.Application.Identity;

internal static class ItemFiscalProjection
{
    /// <summary>Situação cadastral (<c>item.Ativo</c>) e aptidão operacional são conceitos independentes
    /// (B3 — Bloco 5A, decisão do Product Owner): aptidão exige Conta Contábil E Unidade de Medida
    /// preenchidas, existentes e ativas — nunca deriva de <c>item.Ativo</c>. Um item cadastralmente Ativo
    /// mas incompleto (real no Linx: 144 sem Conta, 79 com Conta inativa, 2 sem Unidade, 2 com Unidade
    /// inexistente) é representado como está, nunca corrigido/inventado nem re-classificado como Inativo.</summary>
    public static ItemFiscalDto Projetar(ItemFiscal item, ContaContabilDto? conta, UnidadeMedidaDto? unidade)
    {
        var motivos = new List<string>();
        if (string.IsNullOrWhiteSpace(item.ContaContabilCodigoErp)) motivos.Add("Conta Contábil não informada.");
        else if (conta is null) motivos.Add("Conta Contábil não encontrada.");
        else if (!conta.AtivoEfetivo) motivos.Add("Conta Contábil inativa.");

        if (string.IsNullOrWhiteSpace(item.UnidadeMedidaCodigoErp)) motivos.Add("Unidade de Medida não informada.");
        else if (unidade is null) motivos.Add("Unidade de Medida não encontrada.");
        else if (!unidade.AtivoNoMaisCompras) motivos.Add("Unidade de Medida inativa.");

        return new ItemFiscalDto(
            item.Id,
            item.Codigo,
            item.Descricao,
            item.UnidadeMedidaCodigoErp,
            unidade?.DescricaoErp,
            item.ContaContabilCodigoErp,
            conta?.DescricaoErp,
            item.Ativo,
            item.OrigemInformacao.ToString(),
            motivos.Count == 0,
            motivos,
            item.CriadoEm,
            item.AtualizadoEm);
    }
}

/// <summary>Validação compartilhada entre <see cref="CriarItemFiscalUseCase"/> e
/// <see cref="AtualizarItemFiscalUseCase"/>: reaproveita a leitura combinada ERP+metadados locais já
/// existente dos Blocos 1/2 (<see cref="IListarContasContabeisUseCase"/>/<see cref="IListarUnidadesMedidaUseCase"/>)
/// como única fonte de verdade sobre o que é "válido e ativo" — nunca duplica essa regra aqui.
///
/// Conta Contábil usa <c>AtivoEfetivo</c> (não `AtivoNoMaisCompras`): respeita `ADR-0024` — uma conta
/// inativa no Linx nunca pode ser selecionada no Item Fiscal, mesmo que o metadado local do +Compras diga
/// "ativo". Unidade de Medida não tem essa distinção (sem status no Linx, comprovado no Bloco 2) — usa
/// `AtivoNoMaisCompras` diretamente. Unidades com código vazio/só espaços já são excluídas na origem por
/// <c>ListarUnidadesMedidaUseCase</c> (homologação do Bloco 2) — nunca aparecem aqui como candidatas.</summary>
internal static class ItemFiscalValidacao
{
    public static async Task<RbacFalha?> ValidarContaContabilEUnidadeAsync(
        IListarContasContabeisUseCase contasContabeis, IListarUnidadesMedidaUseCase unidadesMedida,
        string contaContabilCodigoErp, string unidadeMedidaCodigoErp, Guid unidadeNegocioId, CancellationToken ct)
    {
        var contas = await contasContabeis.ExecuteAsync(unidadeNegocioId, ct);
        var conta = contas.FirstOrDefault(c => string.Equals(c.CodigoErp, contaContabilCodigoErp, StringComparison.OrdinalIgnoreCase));
        if (conta is null || !conta.AtivoEfetivo) return RbacFalha.ContaContabilInvalidaOuInativa;

        var unidades = await unidadesMedida.ExecuteAsync(unidadeNegocioId, ct);
        var unidade = unidades.FirstOrDefault(u => string.Equals(u.CodigoErp, unidadeMedidaCodigoErp, StringComparison.OrdinalIgnoreCase));
        if (unidade is null || !unidade.AtivoNoMaisCompras) return RbacFalha.UnidadeMedidaInvalidaOuInativa;

        return null;
    }

    public static string Mensagem(RbacFalha falha) => falha switch
    {
        RbacFalha.ContaContabilInvalidaOuInativa => "Conta Contábil inválida ou inativa no +Compras.",
        RbacFalha.UnidadeMedidaInvalidaOuInativa => "Unidade de Medida inválida ou inativa no +Compras.",
        _ => "Falha de validação.",
    };
}

public sealed class ListarItensFiscaisUseCase(
    IItemFiscalRepository itensFiscais,
    IListarContasContabeisUseCase contasContabeis,
    IListarUnidadesMedidaUseCase unidadesMedida) : IListarItensFiscaisUseCase
{
    public async Task<IReadOnlyList<ItemFiscalDto>> ExecuteAsync(Guid unidadeNegocioId, CancellationToken ct)
    {
        var itens = await itensFiscais.ListarPorUnidadeNegocioAsync(unidadeNegocioId, ct);
        var contas = (await contasContabeis.ExecuteAsync(unidadeNegocioId, ct))
            .ToDictionary(c => c.CodigoErp, StringComparer.OrdinalIgnoreCase);
        var unidades = (await unidadesMedida.ExecuteAsync(unidadeNegocioId, ct))
            .ToDictionary(u => u.CodigoErp, StringComparer.OrdinalIgnoreCase);

        return itens
            .Select(item => ItemFiscalProjection.Projetar(
                item,
                item.ContaContabilCodigoErp is null ? null : contas.GetValueOrDefault(item.ContaContabilCodigoErp),
                item.UnidadeMedidaCodigoErp is null ? null : unidades.GetValueOrDefault(item.UnidadeMedidaCodigoErp)))
            .ToArray();
    }
}

public sealed class ObterItemFiscalUseCase(
    IItemFiscalRepository itensFiscais,
    IListarContasContabeisUseCase contasContabeis,
    IListarUnidadesMedidaUseCase unidadesMedida) : IObterItemFiscalUseCase
{
    public async Task<ItemFiscalDto?> ExecuteAsync(Guid id, Guid unidadeNegocioId, CancellationToken ct)
    {
        var item = await itensFiscais.ObterPorIdEUnidadeNegocioAsync(id, unidadeNegocioId, ct);
        if (item is null) return null;

        var contas = await contasContabeis.ExecuteAsync(unidadeNegocioId, ct);
        var unidades = await unidadesMedida.ExecuteAsync(unidadeNegocioId, ct);
        var conta = contas.FirstOrDefault(c => string.Equals(c.CodigoErp, item.ContaContabilCodigoErp, StringComparison.OrdinalIgnoreCase));
        var unidade = unidades.FirstOrDefault(u => string.Equals(u.CodigoErp, item.UnidadeMedidaCodigoErp, StringComparison.OrdinalIgnoreCase));

        return ItemFiscalProjection.Projetar(item, conta, unidade);
    }
}

public sealed class CriarItemFiscalUseCase(
    IItemFiscalRepository itensFiscais,
    IListarContasContabeisUseCase contasContabeis,
    IListarUnidadesMedidaUseCase unidadesMedida,
    TimeProvider clock) : ICriarItemFiscalUseCase
{
    public async Task<RbacResultado<ItemFiscalDto>> ExecuteAsync(ItemFiscalCriarInput input, Guid unidadeNegocioId, CancellationToken ct)
    {
        var codigo = (input.Codigo ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(codigo))
        {
            return RbacResultado<ItemFiscalDto>.Erro(RbacFalha.CodigoObrigatorio, "Código do Item Fiscal é obrigatório.");
        }

        var descricao = (input.Descricao ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(descricao))
        {
            return RbacResultado<ItemFiscalDto>.Erro(RbacFalha.DescricaoObrigatoria, "Descrição do Item Fiscal é obrigatória.");
        }

        var contaCodigo = (input.ContaContabilCodigoErp ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(contaCodigo))
        {
            return RbacResultado<ItemFiscalDto>.Erro(RbacFalha.ContaContabilObrigatoria, "Conta Contábil é obrigatória para o Item Fiscal.");
        }

        var unidadeCodigo = (input.UnidadeMedidaCodigoErp ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(unidadeCodigo))
        {
            return RbacResultado<ItemFiscalDto>.Erro(RbacFalha.UnidadeMedidaObrigatoria, "Unidade de Medida é obrigatória para o Item Fiscal.");
        }

        // Pré-checagem amigável; a garantia real é o índice único global (Codigo) no SQL Server.
        if (await itensFiscais.ExisteComCodigoAsync(codigo, excluirId: null, ct))
        {
            return RbacResultado<ItemFiscalDto>.Erro(RbacFalha.CodigoDuplicado, "Já existe um Item Fiscal com este código.");
        }

        var falhaValidacao = await ItemFiscalValidacao.ValidarContaContabilEUnidadeAsync(
            contasContabeis, unidadesMedida, contaCodigo, unidadeCodigo, unidadeNegocioId, ct);
        if (falhaValidacao is not null)
        {
            return RbacResultado<ItemFiscalDto>.Erro(falhaValidacao.Value, ItemFiscalValidacao.Mensagem(falhaValidacao.Value));
        }

        var agora = clock.GetUtcNow();
        var item = new ItemFiscal(codigo, descricao, unidadeCodigo, contaCodigo, unidadeNegocioId, agora);
        await itensFiscais.AdicionarAsync(item, ct);
        await itensFiscais.SalvarAlteracoesAsync(ct);

        var conta = (await contasContabeis.ExecuteAsync(unidadeNegocioId, ct))
            .First(c => string.Equals(c.CodigoErp, contaCodigo, StringComparison.OrdinalIgnoreCase));
        var unidade = (await unidadesMedida.ExecuteAsync(unidadeNegocioId, ct))
            .First(u => string.Equals(u.CodigoErp, unidadeCodigo, StringComparison.OrdinalIgnoreCase));

        return RbacResultado<ItemFiscalDto>.Ok(ItemFiscalProjection.Projetar(item, conta, unidade));
    }
}

public sealed class AtualizarItemFiscalUseCase(
    IItemFiscalRepository itensFiscais,
    IListarContasContabeisUseCase contasContabeis,
    IListarUnidadesMedidaUseCase unidadesMedida,
    TimeProvider clock) : IAtualizarItemFiscalUseCase
{
    public async Task<RbacResultado<ItemFiscalDto>> ExecuteAsync(Guid id, ItemFiscalAtualizarInput input, Guid unidadeNegocioId, CancellationToken ct)
    {
        var item = await itensFiscais.ObterPorIdEUnidadeNegocioAsync(id, unidadeNegocioId, ct);
        if (item is null)
        {
            return RbacResultado<ItemFiscalDto>.Erro(RbacFalha.ItemFiscalNaoEncontrado, "Item Fiscal não encontrado.");
        }

        var descricao = (input.Descricao ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(descricao))
        {
            return RbacResultado<ItemFiscalDto>.Erro(RbacFalha.DescricaoObrigatoria, "Descrição do Item Fiscal é obrigatória.");
        }

        var contaCodigo = (input.ContaContabilCodigoErp ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(contaCodigo))
        {
            return RbacResultado<ItemFiscalDto>.Erro(RbacFalha.ContaContabilObrigatoria, "Conta Contábil é obrigatória para o Item Fiscal.");
        }

        var unidadeCodigo = (input.UnidadeMedidaCodigoErp ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(unidadeCodigo))
        {
            return RbacResultado<ItemFiscalDto>.Erro(RbacFalha.UnidadeMedidaObrigatoria, "Unidade de Medida é obrigatória para o Item Fiscal.");
        }

        var falhaValidacao = await ItemFiscalValidacao.ValidarContaContabilEUnidadeAsync(
            contasContabeis, unidadesMedida, contaCodigo, unidadeCodigo, unidadeNegocioId, ct);
        if (falhaValidacao is not null)
        {
            return RbacResultado<ItemFiscalDto>.Erro(falhaValidacao.Value, ItemFiscalValidacao.Mensagem(falhaValidacao.Value));
        }

        var agora = clock.GetUtcNow();
        item.Atualizar(descricao, unidadeCodigo, contaCodigo, agora);
        await itensFiscais.SalvarAlteracoesAsync(ct);

        var conta = (await contasContabeis.ExecuteAsync(unidadeNegocioId, ct))
            .First(c => string.Equals(c.CodigoErp, contaCodigo, StringComparison.OrdinalIgnoreCase));
        var unidade = (await unidadesMedida.ExecuteAsync(unidadeNegocioId, ct))
            .First(u => string.Equals(u.CodigoErp, unidadeCodigo, StringComparison.OrdinalIgnoreCase));

        return RbacResultado<ItemFiscalDto>.Ok(ItemFiscalProjection.Projetar(item, conta, unidade));
    }
}

/// <summary>Ativação/inativação LOCAL no +Compras (Discovery homologado, matriz de autoridade — seção 6).
/// Nunca propaga para o Linx nesta etapa (sem sincronização — Bloco 5).</summary>
public sealed class AlterarStatusItemFiscalUseCase(
    IItemFiscalRepository itensFiscais,
    IListarContasContabeisUseCase contasContabeis,
    IListarUnidadesMedidaUseCase unidadesMedida,
    TimeProvider clock) : IAlterarStatusItemFiscalUseCase
{
    public async Task<RbacResultado<ItemFiscalDto>> ExecuteAsync(Guid id, bool ativo, Guid unidadeNegocioId, CancellationToken ct)
    {
        var item = await itensFiscais.ObterPorIdEUnidadeNegocioAsync(id, unidadeNegocioId, ct);
        if (item is null)
        {
            return RbacResultado<ItemFiscalDto>.Erro(RbacFalha.ItemFiscalNaoEncontrado, "Item Fiscal não encontrado.");
        }

        var agora = clock.GetUtcNow();
        if (ativo) item.Ativar(agora); else item.Inativar(agora);
        await itensFiscais.SalvarAlteracoesAsync(ct);

        var contas = await contasContabeis.ExecuteAsync(unidadeNegocioId, ct);
        var unidades = await unidadesMedida.ExecuteAsync(unidadeNegocioId, ct);
        var conta = contas.FirstOrDefault(c => string.Equals(c.CodigoErp, item.ContaContabilCodigoErp, StringComparison.OrdinalIgnoreCase));
        var unidade = unidades.FirstOrDefault(u => string.Equals(u.CodigoErp, item.UnidadeMedidaCodigoErp, StringComparison.OrdinalIgnoreCase));

        return RbacResultado<ItemFiscalDto>.Ok(ItemFiscalProjection.Projetar(item, conta, unidade));
    }
}
