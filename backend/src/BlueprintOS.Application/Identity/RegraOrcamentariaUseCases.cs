using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Domain.Identity;

namespace BlueprintOS.Application.Identity;

internal static class RegraOrcamentariaProjection
{
    public static RegraOrcamentariaDto Projetar(RegraOrcamentaria regra) => new(
        regra.Id, regra.Nome, regra.UnidadeNegocioId, regra.CentroCustoMetadadoId, regra.ValorLimite,
        regra.Periodo, regra.Ativo, regra.CriadoEm, regra.AtualizadoEm);
}

/// <summary>O1.12 — Fundação de Administração de Controle Orçamentário. CRUD administrativo por Unidade
/// de Negócio, sem exclusão física. APENAS o cadastro: nenhuma reserva contábil, consumo real ou bloqueio
/// operacional é implementado aqui.</summary>
public sealed class ListarRegrasOrcamentariasUseCase(IRegraOrcamentariaRepository regras) : IListarRegrasOrcamentariasUseCase
{
    public async Task<IReadOnlyList<RegraOrcamentariaDto>> ExecuteAsync(Guid unidadeNegocioId, CancellationToken ct)
    {
        var encontradas = await regras.ListarPorUnidadeNegocioAsync(unidadeNegocioId, ct);
        return encontradas.Select(RegraOrcamentariaProjection.Projetar).ToArray();
    }
}

public sealed class CriarRegraOrcamentariaUseCase(
    IRegraOrcamentariaRepository regras, IUnidadeNegocioRepository unidadesNegocio,
    ICentroCustoMetadadoRepository centrosCusto, TimeProvider clock) : ICriarRegraOrcamentariaUseCase
{
    public async Task<RbacResultado<RegraOrcamentariaDto>> ExecuteAsync(RegraOrcamentariaInput input, Guid unidadeNegocioId, CancellationToken ct)
    {
        if (await unidadesNegocio.ObterPorIdAsync(unidadeNegocioId, ct) is null)
        {
            return RbacResultado<RegraOrcamentariaDto>.Erro(RbacFalha.UnidadeNegocioNaoEncontrada, "Unidade de Negócio não encontrada.");
        }

        var nome = (input.Nome ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(nome))
        {
            return RbacResultado<RegraOrcamentariaDto>.Erro(RbacFalha.NomeObrigatorio, "Nome da Regra Orçamentária é obrigatório.");
        }

        if (input.ValorLimite <= 0)
        {
            return RbacResultado<RegraOrcamentariaDto>.Erro(RbacFalha.ValorLimiteInvalido, "Valor limite deve ser maior que zero.");
        }

        var centroCusto = await centrosCusto.ObterPorIdEUnidadeNegocioAsync(input.CentroCustoMetadadoId, unidadeNegocioId, ct);
        if (centroCusto is null)
        {
            return RbacResultado<RegraOrcamentariaDto>.Erro(
                RbacFalha.CentroCustoInvalidoNaUnidadeDeNegocio, "Centro de Custo informado não pertence a esta Unidade de Negócio.");
        }

        var agora = clock.GetUtcNow();
        var regra = new RegraOrcamentaria(nome, unidadeNegocioId, input.CentroCustoMetadadoId, input.ValorLimite, input.Periodo, agora);
        await regras.AdicionarAsync(regra, ct);
        await regras.SalvarAlteracoesAsync(ct);

        return RbacResultado<RegraOrcamentariaDto>.Ok(RegraOrcamentariaProjection.Projetar(regra));
    }
}

public sealed class AtualizarRegraOrcamentariaUseCase(
    IRegraOrcamentariaRepository regras, ICentroCustoMetadadoRepository centrosCusto, TimeProvider clock) : IAtualizarRegraOrcamentariaUseCase
{
    public async Task<RbacResultado<RegraOrcamentariaDto>> ExecuteAsync(Guid id, RegraOrcamentariaInput input, Guid unidadeNegocioId, CancellationToken ct)
    {
        var regra = await regras.ObterPorIdEUnidadeNegocioAsync(id, unidadeNegocioId, ct);
        if (regra is null)
        {
            return RbacResultado<RegraOrcamentariaDto>.Erro(RbacFalha.RegraOrcamentariaNaoEncontrada, "Regra Orçamentária não encontrada.");
        }

        var nome = (input.Nome ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(nome))
        {
            return RbacResultado<RegraOrcamentariaDto>.Erro(RbacFalha.NomeObrigatorio, "Nome da Regra Orçamentária é obrigatório.");
        }

        if (input.ValorLimite <= 0)
        {
            return RbacResultado<RegraOrcamentariaDto>.Erro(RbacFalha.ValorLimiteInvalido, "Valor limite deve ser maior que zero.");
        }

        var centroCusto = await centrosCusto.ObterPorIdEUnidadeNegocioAsync(input.CentroCustoMetadadoId, unidadeNegocioId, ct);
        if (centroCusto is null)
        {
            return RbacResultado<RegraOrcamentariaDto>.Erro(
                RbacFalha.CentroCustoInvalidoNaUnidadeDeNegocio, "Centro de Custo informado não pertence a esta Unidade de Negócio.");
        }

        var agora = clock.GetUtcNow();
        regra.Editar(nome, input.CentroCustoMetadadoId, input.ValorLimite, input.Periodo, agora);
        await regras.SalvarAlteracoesAsync(ct);

        return RbacResultado<RegraOrcamentariaDto>.Ok(RegraOrcamentariaProjection.Projetar(regra));
    }
}

public sealed class AlterarStatusRegraOrcamentariaUseCase(IRegraOrcamentariaRepository regras, TimeProvider clock) : IAlterarStatusRegraOrcamentariaUseCase
{
    public async Task<RbacResultado<RegraOrcamentariaDto>> ExecuteAsync(Guid id, bool ativo, Guid unidadeNegocioId, CancellationToken ct)
    {
        var regra = await regras.ObterPorIdEUnidadeNegocioAsync(id, unidadeNegocioId, ct);
        if (regra is null)
        {
            return RbacResultado<RegraOrcamentariaDto>.Erro(RbacFalha.RegraOrcamentariaNaoEncontrada, "Regra Orçamentária não encontrada.");
        }

        var agora = clock.GetUtcNow();
        if (ativo) regra.Ativar(agora); else regra.Inativar(agora);
        await regras.SalvarAlteracoesAsync(ct);

        return RbacResultado<RegraOrcamentariaDto>.Ok(RegraOrcamentariaProjection.Projetar(regra));
    }
}
