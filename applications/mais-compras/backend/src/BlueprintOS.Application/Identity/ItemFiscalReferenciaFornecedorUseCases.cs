using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using BlueprintOS.Domain.Identity;

namespace BlueprintOS.Application.Identity;

internal static class ItemFiscalReferenciaFornecedorProjection
{
    public static ItemFiscalReferenciaFornecedorDto Projetar(ItemFiscalReferenciaFornecedor referencia, string fornecedorNome) => new(
        referencia.Id,
        referencia.ItemFiscalId,
        referencia.FornecedorId,
        fornecedorNome,
        referencia.CodigoItemFornecedor,
        referencia.CriadoEm,
        referencia.AtualizadoEm);
}

/// <summary>Validação compartilhada entre os casos de uso de escrita: confirma que o Item Fiscal pai existe
/// na Unidade de Negócio da identidade autenticada (nunca confia em <c>itemFiscalId</c> vindo apenas da
/// rota) e que o Fornecedor informado existe e está ativo — reaproveita o cadastro de Fornecedores já
/// existente (<see cref="IObterFornecedorUseCase"/>) como única fonte de verdade, nunca duplica a regra
/// aqui (mesmo princípio de <c>ItemFiscalValidacao</c> para Conta Contábil/Unidade de Medida).</summary>
internal static class ItemFiscalReferenciaFornecedorValidacao
{
    public static async Task<(ItemFiscal? item, RbacFalha? falha)> ValidarItemFiscalAsync(
        IItemFiscalRepository itensFiscais, Guid itemFiscalId, Guid unidadeNegocioId, CancellationToken ct)
    {
        var item = await itensFiscais.ObterPorIdEUnidadeNegocioAsync(itemFiscalId, unidadeNegocioId, ct);
        return item is null ? (null, RbacFalha.ItemFiscalNaoEncontrado) : (item, null);
    }

    public static async Task<(string? nome, RbacFalha? falha)> ValidarFornecedorAsync(
        IObterFornecedorUseCase fornecedores, Guid fornecedorId, CancellationToken ct)
    {
        if (fornecedorId == Guid.Empty) return (null, RbacFalha.FornecedorObrigatorio);

        var fornecedor = await fornecedores.ExecuteAsync(fornecedorId, ct);
        if (fornecedor is null) return (null, RbacFalha.FornecedorNaoEncontrado);
        if (!string.Equals(fornecedor.Status, "Ativo", StringComparison.OrdinalIgnoreCase)) return (null, RbacFalha.FornecedorInvalidoOuInativo);

        return (fornecedor.Nome, null);
    }

    public static string Mensagem(RbacFalha falha) => falha switch
    {
        RbacFalha.ItemFiscalNaoEncontrado => "Item Fiscal não encontrado.",
        RbacFalha.FornecedorObrigatorio => "Fornecedor é obrigatório.",
        RbacFalha.FornecedorNaoEncontrado => "Fornecedor não encontrado.",
        RbacFalha.FornecedorInvalidoOuInativo => "Fornecedor inválido ou inativo no +Compras.",
        RbacFalha.CodigoItemFornecedorObrigatorio => "Código do item no fornecedor é obrigatório.",
        RbacFalha.ReferenciaJaExistenteParaFornecedor => "Este fornecedor já possui uma referência cadastrada para este Item Fiscal.",
        RbacFalha.CodigoItemFornecedorDuplicadoParaFornecedor => "Este código já está associado a outro Item Fiscal para o mesmo fornecedor.",
        RbacFalha.ItemFiscalReferenciaFornecedorNaoEncontrada => "Referência de fornecedor não encontrada.",
        _ => "Falha de validação.",
    };
}

public sealed class ListarReferenciasFornecedorUseCase(
    IItemFiscalRepository itensFiscais,
    IItemFiscalReferenciaFornecedorRepository referencias,
    IObterFornecedorUseCase fornecedores) : IListarReferenciasFornecedorUseCase
{
    public async Task<RbacResultado<IReadOnlyList<ItemFiscalReferenciaFornecedorDto>>> ExecuteAsync(Guid itemFiscalId, Guid unidadeNegocioId, CancellationToken ct)
    {
        var (item, falhaItem) = await ItemFiscalReferenciaFornecedorValidacao.ValidarItemFiscalAsync(itensFiscais, itemFiscalId, unidadeNegocioId, ct);
        if (item is null)
        {
            return RbacResultado<IReadOnlyList<ItemFiscalReferenciaFornecedorDto>>.Erro(falhaItem!.Value, ItemFiscalReferenciaFornecedorValidacao.Mensagem(falhaItem.Value));
        }

        var lista = await referencias.ListarPorItemFiscalAsync(itemFiscalId, ct);
        var projetadas = new List<ItemFiscalReferenciaFornecedorDto>(lista.Count);
        foreach (var referencia in lista)
        {
            var fornecedor = await fornecedores.ExecuteAsync(referencia.FornecedorId, ct);
            projetadas.Add(ItemFiscalReferenciaFornecedorProjection.Projetar(referencia, fornecedor?.Nome ?? "Fornecedor não encontrado"));
        }

        return RbacResultado<IReadOnlyList<ItemFiscalReferenciaFornecedorDto>>.Ok(projetadas);
    }
}

public sealed class IncluirReferenciaFornecedorUseCase(
    IItemFiscalRepository itensFiscais,
    IItemFiscalReferenciaFornecedorRepository referencias,
    IObterFornecedorUseCase fornecedores,
    TimeProvider clock) : IIncluirReferenciaFornecedorUseCase
{
    public async Task<RbacResultado<ItemFiscalReferenciaFornecedorDto>> ExecuteAsync(Guid itemFiscalId, ItemFiscalReferenciaFornecedorCriarInput input, Guid unidadeNegocioId, CancellationToken ct)
    {
        var (item, falhaItem) = await ItemFiscalReferenciaFornecedorValidacao.ValidarItemFiscalAsync(itensFiscais, itemFiscalId, unidadeNegocioId, ct);
        if (item is null)
        {
            return RbacResultado<ItemFiscalReferenciaFornecedorDto>.Erro(falhaItem!.Value, ItemFiscalReferenciaFornecedorValidacao.Mensagem(falhaItem.Value));
        }

        var codigo = (input.CodigoItemFornecedor ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(codigo))
        {
            return RbacResultado<ItemFiscalReferenciaFornecedorDto>.Erro(RbacFalha.CodigoItemFornecedorObrigatorio, ItemFiscalReferenciaFornecedorValidacao.Mensagem(RbacFalha.CodigoItemFornecedorObrigatorio));
        }

        var (fornecedorNome, falhaFornecedor) = await ItemFiscalReferenciaFornecedorValidacao.ValidarFornecedorAsync(fornecedores, input.FornecedorId, ct);
        if (fornecedorNome is null)
        {
            return RbacResultado<ItemFiscalReferenciaFornecedorDto>.Erro(falhaFornecedor!.Value, ItemFiscalReferenciaFornecedorValidacao.Mensagem(falhaFornecedor.Value));
        }

        // Pré-checagens amigáveis; a garantia real são os índices únicos no SQL Server.
        if (await referencias.ExisteParaFornecedorNoItemAsync(itemFiscalId, input.FornecedorId, excluirId: null, ct))
        {
            return RbacResultado<ItemFiscalReferenciaFornecedorDto>.Erro(RbacFalha.ReferenciaJaExistenteParaFornecedor, ItemFiscalReferenciaFornecedorValidacao.Mensagem(RbacFalha.ReferenciaJaExistenteParaFornecedor));
        }
        if (await referencias.ExisteCodigoParaFornecedorAsync(input.FornecedorId, codigo, excluirId: null, ct))
        {
            return RbacResultado<ItemFiscalReferenciaFornecedorDto>.Erro(RbacFalha.CodigoItemFornecedorDuplicadoParaFornecedor, ItemFiscalReferenciaFornecedorValidacao.Mensagem(RbacFalha.CodigoItemFornecedorDuplicadoParaFornecedor));
        }

        var agora = clock.GetUtcNow();
        var referencia = new ItemFiscalReferenciaFornecedor(itemFiscalId, input.FornecedorId, codigo, agora);
        await referencias.AdicionarAsync(referencia, ct);
        await referencias.SalvarAlteracoesAsync(ct);

        return RbacResultado<ItemFiscalReferenciaFornecedorDto>.Ok(ItemFiscalReferenciaFornecedorProjection.Projetar(referencia, fornecedorNome));
    }
}

public sealed class AtualizarReferenciaFornecedorUseCase(
    IItemFiscalRepository itensFiscais,
    IItemFiscalReferenciaFornecedorRepository referencias,
    IObterFornecedorUseCase fornecedores,
    TimeProvider clock) : IAtualizarReferenciaFornecedorUseCase
{
    public async Task<RbacResultado<ItemFiscalReferenciaFornecedorDto>> ExecuteAsync(Guid itemFiscalId, Guid referenciaId, ItemFiscalReferenciaFornecedorAtualizarInput input, Guid unidadeNegocioId, CancellationToken ct)
    {
        var (item, falhaItem) = await ItemFiscalReferenciaFornecedorValidacao.ValidarItemFiscalAsync(itensFiscais, itemFiscalId, unidadeNegocioId, ct);
        if (item is null)
        {
            return RbacResultado<ItemFiscalReferenciaFornecedorDto>.Erro(falhaItem!.Value, ItemFiscalReferenciaFornecedorValidacao.Mensagem(falhaItem.Value));
        }

        var referencia = await referencias.ObterPorIdAsync(referenciaId, itemFiscalId, ct);
        if (referencia is null)
        {
            return RbacResultado<ItemFiscalReferenciaFornecedorDto>.Erro(RbacFalha.ItemFiscalReferenciaFornecedorNaoEncontrada, ItemFiscalReferenciaFornecedorValidacao.Mensagem(RbacFalha.ItemFiscalReferenciaFornecedorNaoEncontrada));
        }

        var codigo = (input.CodigoItemFornecedor ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(codigo))
        {
            return RbacResultado<ItemFiscalReferenciaFornecedorDto>.Erro(RbacFalha.CodigoItemFornecedorObrigatorio, ItemFiscalReferenciaFornecedorValidacao.Mensagem(RbacFalha.CodigoItemFornecedorObrigatorio));
        }

        if (await referencias.ExisteCodigoParaFornecedorAsync(referencia.FornecedorId, codigo, excluirId: referencia.Id, ct))
        {
            return RbacResultado<ItemFiscalReferenciaFornecedorDto>.Erro(RbacFalha.CodigoItemFornecedorDuplicadoParaFornecedor, ItemFiscalReferenciaFornecedorValidacao.Mensagem(RbacFalha.CodigoItemFornecedorDuplicadoParaFornecedor));
        }

        var agora = clock.GetUtcNow();
        referencia.Atualizar(codigo, agora);
        await referencias.SalvarAlteracoesAsync(ct);

        var fornecedor = await fornecedores.ExecuteAsync(referencia.FornecedorId, ct);
        return RbacResultado<ItemFiscalReferenciaFornecedorDto>.Ok(ItemFiscalReferenciaFornecedorProjection.Projetar(referencia, fornecedor?.Nome ?? "Fornecedor não encontrado"));
    }
}

/// <summary>Remoção FÍSICA (não é inativação lógica) — comprovado em Linx que
/// <c>ITEM_FISCAL_REF_FORNECEDOR</c> não tem coluna de status; o mesmo comportamento é replicado aqui
/// (Discovery homologado, seção "fluxo VFP de inclusão/edição/exclusão").</summary>
public sealed class RemoverReferenciaFornecedorUseCase(
    IItemFiscalRepository itensFiscais,
    IItemFiscalReferenciaFornecedorRepository referencias) : IRemoverReferenciaFornecedorUseCase
{
    public async Task<RbacResultado<bool>> ExecuteAsync(Guid itemFiscalId, Guid referenciaId, Guid unidadeNegocioId, CancellationToken ct)
    {
        var (item, falhaItem) = await ItemFiscalReferenciaFornecedorValidacao.ValidarItemFiscalAsync(itensFiscais, itemFiscalId, unidadeNegocioId, ct);
        if (item is null)
        {
            return RbacResultado<bool>.Erro(falhaItem!.Value, ItemFiscalReferenciaFornecedorValidacao.Mensagem(falhaItem.Value));
        }

        var referencia = await referencias.ObterPorIdAsync(referenciaId, itemFiscalId, ct);
        if (referencia is null)
        {
            return RbacResultado<bool>.Erro(RbacFalha.ItemFiscalReferenciaFornecedorNaoEncontrada, ItemFiscalReferenciaFornecedorValidacao.Mensagem(RbacFalha.ItemFiscalReferenciaFornecedorNaoEncontrada));
        }

        await referencias.RemoverAsync(referencia, ct);
        await referencias.SalvarAlteracoesAsync(ct);

        return RbacResultado<bool>.Ok(true);
    }
}
