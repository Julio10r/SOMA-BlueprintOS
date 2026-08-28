using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Domain.Identity;

namespace BlueprintOS.Application.Identity;

internal static class RegraWorkflowProjection
{
    public static RegraWorkflowDto Projetar(RegraWorkflow regra) => new(
        regra.Id, regra.Nome, regra.UnidadeNegocioId, regra.TipoProcesso, regra.Ordem, regra.Ativo, regra.CriadoEm, regra.AtualizadoEm);
}

/// <summary>O1.12 — Fundação de Administração de Workflow. CRUD administrativo por Unidade de Negócio,
/// sem exclusão física (apenas ativar/inativar). Nenhum motor de execução de workflow é acionado aqui.</summary>
public sealed class ListarRegrasWorkflowUseCase(IRegraWorkflowRepository regras) : IListarRegrasWorkflowUseCase
{
    public async Task<IReadOnlyList<RegraWorkflowDto>> ExecuteAsync(Guid unidadeNegocioId, CancellationToken ct)
    {
        var encontradas = await regras.ListarPorUnidadeNegocioAsync(unidadeNegocioId, ct);
        return encontradas.Select(RegraWorkflowProjection.Projetar).ToArray();
    }
}

public sealed class CriarRegraWorkflowUseCase(
    IRegraWorkflowRepository regras, IUnidadeNegocioRepository unidadesNegocio, TimeProvider clock) : ICriarRegraWorkflowUseCase
{
    public async Task<RbacResultado<RegraWorkflowDto>> ExecuteAsync(RegraWorkflowInput input, Guid unidadeNegocioId, CancellationToken ct)
    {
        if (await unidadesNegocio.ObterPorIdAsync(unidadeNegocioId, ct) is null)
        {
            return RbacResultado<RegraWorkflowDto>.Erro(RbacFalha.UnidadeNegocioNaoEncontrada, "Unidade de Negócio não encontrada.");
        }

        var nome = (input.Nome ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(nome))
        {
            return RbacResultado<RegraWorkflowDto>.Erro(RbacFalha.NomeObrigatorio, "Nome da Regra de Workflow é obrigatório.");
        }

        var tipoProcesso = (input.TipoProcesso ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(tipoProcesso))
        {
            return RbacResultado<RegraWorkflowDto>.Erro(RbacFalha.TipoProcessoObrigatorio, "Tipo de processo é obrigatório.");
        }

        if (input.Ordem < 0)
        {
            return RbacResultado<RegraWorkflowDto>.Erro(RbacFalha.OrdemInvalida, "Ordem não pode ser negativa.");
        }

        var agora = clock.GetUtcNow();
        var regra = new RegraWorkflow(nome, unidadeNegocioId, tipoProcesso, input.Ordem, agora);
        await regras.AdicionarAsync(regra, ct);
        await regras.SalvarAlteracoesAsync(ct);

        return RbacResultado<RegraWorkflowDto>.Ok(RegraWorkflowProjection.Projetar(regra));
    }
}

public sealed class AtualizarRegraWorkflowUseCase(IRegraWorkflowRepository regras, TimeProvider clock) : IAtualizarRegraWorkflowUseCase
{
    public async Task<RbacResultado<RegraWorkflowDto>> ExecuteAsync(Guid id, RegraWorkflowInput input, Guid unidadeNegocioId, CancellationToken ct)
    {
        var regra = await regras.ObterPorIdEUnidadeNegocioAsync(id, unidadeNegocioId, ct);
        if (regra is null)
        {
            return RbacResultado<RegraWorkflowDto>.Erro(RbacFalha.RegraWorkflowNaoEncontrada, "Regra de Workflow não encontrada.");
        }

        var nome = (input.Nome ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(nome))
        {
            return RbacResultado<RegraWorkflowDto>.Erro(RbacFalha.NomeObrigatorio, "Nome da Regra de Workflow é obrigatório.");
        }

        var tipoProcesso = (input.TipoProcesso ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(tipoProcesso))
        {
            return RbacResultado<RegraWorkflowDto>.Erro(RbacFalha.TipoProcessoObrigatorio, "Tipo de processo é obrigatório.");
        }

        if (input.Ordem < 0)
        {
            return RbacResultado<RegraWorkflowDto>.Erro(RbacFalha.OrdemInvalida, "Ordem não pode ser negativa.");
        }

        var agora = clock.GetUtcNow();
        regra.Editar(nome, tipoProcesso, input.Ordem, agora);
        await regras.SalvarAlteracoesAsync(ct);

        return RbacResultado<RegraWorkflowDto>.Ok(RegraWorkflowProjection.Projetar(regra));
    }
}

public sealed class AlterarStatusRegraWorkflowUseCase(IRegraWorkflowRepository regras, TimeProvider clock) : IAlterarStatusRegraWorkflowUseCase
{
    public async Task<RbacResultado<RegraWorkflowDto>> ExecuteAsync(Guid id, bool ativo, Guid unidadeNegocioId, CancellationToken ct)
    {
        var regra = await regras.ObterPorIdEUnidadeNegocioAsync(id, unidadeNegocioId, ct);
        if (regra is null)
        {
            return RbacResultado<RegraWorkflowDto>.Erro(RbacFalha.RegraWorkflowNaoEncontrada, "Regra de Workflow não encontrada.");
        }

        var agora = clock.GetUtcNow();
        if (ativo) regra.Ativar(agora); else regra.Inativar(agora);
        await regras.SalvarAlteracoesAsync(ct);

        return RbacResultado<RegraWorkflowDto>.Ok(RegraWorkflowProjection.Projetar(regra));
    }
}
