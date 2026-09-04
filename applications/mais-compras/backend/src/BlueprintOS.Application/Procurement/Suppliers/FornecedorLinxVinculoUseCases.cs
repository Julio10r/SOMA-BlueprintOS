using BlueprintOS.Application.Procurement.Suppliers.Contracts;

namespace BlueprintOS.Application.Procurement.Suppliers;

public sealed class ListarFornecedorLinxVinculosUseCase(
    IFornecedorRepository fornecedores, IFornecedorLinxVinculoRepository vinculos) : IListarFornecedorLinxVinculosUseCase
{
    public async Task<IReadOnlyList<FornecedorLinxVinculoDto>?> ExecuteAsync(Guid fornecedorId, CancellationToken cancellationToken = default)
    {
        var fornecedor = await fornecedores.ObterPorIdAsync(fornecedorId, cancellationToken);
        if (fornecedor is null) return null;

        var lista = await vinculos.ListarPorFornecedorAsync(fornecedorId, cancellationToken);
        return lista
            .OrderByDescending(v => v.Principal)
            .ThenByDescending(v => v.DataParaTransferencia)
            .Select(v => new FornecedorLinxVinculoDto(v.Id, v.ErpSistema, v.CodigoErp, v.NomeClifor, v.Ativo, v.Principal, v.DataParaTransferencia))
            .ToArray();
    }
}

/// <summary>B3 — Bloco 5A.9 (§5/§15): a única forma de trocar o Principal depois que ele já existe é essa
/// escolha explícita do comprador — nunca automática por recência. Rejeita vínculo inativo (§3) e vínculo
/// que não pertence ao Fornecedor informado.</summary>
public sealed class DefinirFornecedorLinxVinculoPrincipalUseCase(
    IFornecedorRepository fornecedores, IFornecedorLinxVinculoRepository vinculos) : IDefinirFornecedorLinxVinculoPrincipalUseCase
{
    public async Task<bool> ExecuteAsync(Guid fornecedorId, Guid vinculoId, CancellationToken cancellationToken = default)
    {
        var fornecedor = await fornecedores.ObterPorIdAsync(fornecedorId, cancellationToken);
        if (fornecedor is null) return false;

        var alvo = await vinculos.ObterPorIdAsync(vinculoId, cancellationToken);
        if (alvo is null || alvo.FornecedorId != fornecedorId) return false;

        if (!alvo.Ativo)
        {
            throw new InvalidOperationException("Um vínculo inativo não pode ser definido como Principal.");
        }

        if (!alvo.Principal)
        {
            var agora = DateTimeOffset.UtcNow;
            var todos = await vinculos.ListarPorFornecedorAsync(fornecedorId, cancellationToken);
            var principalAtual = todos.SingleOrDefault(v => v.Principal && v.Ativo);
            principalAtual?.RemoverComoPrincipal(agora);
            alvo.DefinirComoPrincipal(agora);
            fornecedor.RegistrarVinculoErp(fornecedor.BusinessUnit ?? "DEFAULT", alvo.ErpSistema, alvo.CodigoErp);
            await fornecedores.AtualizarAsync(fornecedor, cancellationToken);
            await vinculos.SalvarAlteracoesAsync(cancellationToken);
        }

        return true;
    }
}
