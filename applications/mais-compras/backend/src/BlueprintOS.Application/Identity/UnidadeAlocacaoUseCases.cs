using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Domain.Identity;

namespace BlueprintOS.Application.Identity;

internal static class UnidadeAlocacaoProjection
{
    public static UnidadeAlocacaoDto Projetar(UnidadeAlocacao unidade) => new(
        unidade.Id,
        unidade.Nome,
        unidade.Descricao,
        unidade.UnidadeNegocioId,
        unidade.EstaAtiva(),
        unidade.CriadoEm,
        unidade.AtualizadoEm);
}

public sealed class ListarUnidadesAlocacaoUseCase(IUnidadeAlocacaoRepository unidadesAlocacao) : IListarUnidadesAlocacaoUseCase
{
    public async Task<IReadOnlyList<UnidadeAlocacaoDto>> ExecuteAsync(Guid unidadeNegocioId, CancellationToken ct)
    {
        var encontradas = await unidadesAlocacao.ListarPorUnidadeNegocioAsync(unidadeNegocioId, ct);
        return encontradas.Select(UnidadeAlocacaoProjection.Projetar).ToArray();
    }
}

public sealed class ObterUnidadeAlocacaoUseCase(IUnidadeAlocacaoRepository unidadesAlocacao) : IObterUnidadeAlocacaoUseCase
{
    public async Task<UnidadeAlocacaoDto?> ExecuteAsync(Guid id, Guid unidadeNegocioId, CancellationToken ct)
    {
        var unidade = await unidadesAlocacao.ObterPorIdEUnidadeNegocioAsync(id, unidadeNegocioId, ct);
        return unidade is null ? null : UnidadeAlocacaoProjection.Projetar(unidade);
    }
}

public sealed class CriarUnidadeAlocacaoUseCase(
    IUnidadeAlocacaoRepository unidadesAlocacao, TimeProvider clock) : ICriarUnidadeAlocacaoUseCase
{
    public async Task<RbacResultado<UnidadeAlocacaoDto>> ExecuteAsync(
        UnidadeAlocacaoInput input, Guid unidadeNegocioId, CancellationToken ct)
    {
        var nome = (input.Nome ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(nome))
        {
            return RbacResultado<UnidadeAlocacaoDto>.Erro(RbacFalha.NomeObrigatorio, "Nome da Unidade de Alocação é obrigatório.");
        }

        // Pré-checagem amigável; a garantia real é o índice único (UnidadeNegocioId, Nome) no SQL Server.
        if (await unidadesAlocacao.ExisteComNomeAsync(nome, unidadeNegocioId, excluirId: null, ct))
        {
            return RbacResultado<UnidadeAlocacaoDto>.Erro(RbacFalha.NomeDuplicado, "Já existe uma Unidade de Alocação com este nome nesta Unidade de Negócio.");
        }

        var agora = clock.GetUtcNow();
        var unidade = new UnidadeAlocacao(nome, input.Descricao ?? string.Empty, unidadeNegocioId, agora);
        await unidadesAlocacao.AdicionarAsync(unidade, ct);
        await unidadesAlocacao.SalvarAlteracoesAsync(ct);

        return RbacResultado<UnidadeAlocacaoDto>.Ok(UnidadeAlocacaoProjection.Projetar(unidade));
    }
}

public sealed class AtualizarUnidadeAlocacaoUseCase(
    IUnidadeAlocacaoRepository unidadesAlocacao, TimeProvider clock) : IAtualizarUnidadeAlocacaoUseCase
{
    public async Task<RbacResultado<UnidadeAlocacaoDto>> ExecuteAsync(
        Guid id, UnidadeAlocacaoInput input, Guid unidadeNegocioId, CancellationToken ct)
    {
        var unidade = await unidadesAlocacao.ObterPorIdEUnidadeNegocioAsync(id, unidadeNegocioId, ct);
        if (unidade is null)
        {
            return RbacResultado<UnidadeAlocacaoDto>.Erro(RbacFalha.UnidadeAlocacaoNaoEncontrada, "Unidade de Alocação não encontrada.");
        }

        var nome = (input.Nome ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(nome))
        {
            return RbacResultado<UnidadeAlocacaoDto>.Erro(RbacFalha.NomeObrigatorio, "Nome da Unidade de Alocação é obrigatório.");
        }

        if (await unidadesAlocacao.ExisteComNomeAsync(nome, unidadeNegocioId, excluirId: id, ct))
        {
            return RbacResultado<UnidadeAlocacaoDto>.Erro(RbacFalha.NomeDuplicado, "Já existe uma Unidade de Alocação com este nome nesta Unidade de Negócio.");
        }

        var agora = clock.GetUtcNow();
        unidade.Atualizar(nome, input.Descricao ?? string.Empty, agora);
        await unidadesAlocacao.SalvarAlteracoesAsync(ct);

        return RbacResultado<UnidadeAlocacaoDto>.Ok(UnidadeAlocacaoProjection.Projetar(unidade));
    }
}

public sealed class AlterarStatusUnidadeAlocacaoUseCase(
    IUnidadeAlocacaoRepository unidadesAlocacao, TimeProvider clock) : IAlterarStatusUnidadeAlocacaoUseCase
{
    public async Task<RbacResultado<UnidadeAlocacaoDto>> ExecuteAsync(Guid id, bool ativo, Guid unidadeNegocioId, CancellationToken ct)
    {
        var unidade = await unidadesAlocacao.ObterPorIdEUnidadeNegocioAsync(id, unidadeNegocioId, ct);
        if (unidade is null)
        {
            return RbacResultado<UnidadeAlocacaoDto>.Erro(RbacFalha.UnidadeAlocacaoNaoEncontrada, "Unidade de Alocação não encontrada.");
        }

        var agora = clock.GetUtcNow();
        if (ativo) unidade.Ativar(agora); else unidade.Inativar(agora);
        await unidadesAlocacao.SalvarAlteracoesAsync(ct);

        return RbacResultado<UnidadeAlocacaoDto>.Ok(UnidadeAlocacaoProjection.Projetar(unidade));
    }
}
