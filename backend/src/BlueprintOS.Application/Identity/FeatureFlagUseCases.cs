using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Domain.Identity;
using Microsoft.Extensions.Logging;

namespace BlueprintOS.Application.Identity;

/// <summary>O1.11 — Feature Flags. Catálogo nasce vazio (nenhuma flag semeada por migration); cada flag é
/// cadastrada explicitamente pela Administração e habilitada/desabilitada por Unidade de Negócio via o
/// vínculo N:N <see cref="FeatureFlagUnidadeNegocio"/> (conforme `ComprasDataModel.md`).</summary>
public sealed class FeatureFlagProjector(IUnidadeNegocioRepository unidadesNegocio, IFeatureFlagRepository flags)
{
    public async Task<FeatureFlagDto> ProjetarAsync(FeatureFlag flag, CancellationToken ct)
    {
        var status = await flags.ListarStatusPorFlagAsync(flag.Id, ct);
        var todasUnidades = await unidadesNegocio.ListarTodasAsync(ct);
        var nomesPorId = todasUnidades.ToDictionary(u => u.Id, u => u.Nome);

        var statusDto = status
            .Where(s => nomesPorId.ContainsKey(s.UnidadeNegocioId))
            .Select(s => new FeatureFlagStatusUnidadeDto(s.UnidadeNegocioId, nomesPorId[s.UnidadeNegocioId], s.Ativa))
            .ToArray();

        return new FeatureFlagDto(flag.Id, flag.Nome, flag.Descricao, statusDto);
    }
}

public sealed class ListarFeatureFlagsUseCase(IFeatureFlagRepository flags, FeatureFlagProjector projector) : IListarFeatureFlagsUseCase
{
    public async Task<IReadOnlyList<FeatureFlagDto>> ExecuteAsync(CancellationToken ct)
    {
        var todas = await flags.ListarAsync(ct);
        var resultado = new List<FeatureFlagDto>(todas.Count);
        foreach (var flag in todas) resultado.Add(await projector.ProjetarAsync(flag, ct));
        return resultado;
    }
}

public sealed class CriarFeatureFlagUseCase(
    IFeatureFlagRepository flags, FeatureFlagProjector projector, TimeProvider clock,
    ILogger<CriarFeatureFlagUseCase> logger) : ICriarFeatureFlagUseCase
{
    public async Task<RbacResultado<FeatureFlagDto>> ExecuteAsync(FeatureFlagCriarInput input, CancellationToken ct)
    {
        var nome = (input.Nome ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(nome))
        {
            return RbacResultado<FeatureFlagDto>.Erro(RbacFalha.NomeObrigatorio, "Nome da Feature Flag é obrigatório.");
        }

        if (await flags.ExisteComNomeAsync(nome, ct))
        {
            return RbacResultado<FeatureFlagDto>.Erro(RbacFalha.FeatureFlagDuplicada, "Já existe uma Feature Flag com este nome.");
        }

        var flag = new FeatureFlag(nome, input.Descricao, clock.GetUtcNow());
        await flags.AdicionarAsync(flag, ct);
        await flags.SalvarAlteracoesAsync(ct);

        logger.LogInformation("Feature Flag criada. FeatureFlagId={FeatureFlagId} Nome={Nome}", flag.Id, flag.Nome);

        return RbacResultado<FeatureFlagDto>.Ok(await projector.ProjetarAsync(flag, ct));
    }
}

public sealed class AlterarStatusFeatureFlagUnidadeUseCase(
    IFeatureFlagRepository flags, IUnidadeNegocioRepository unidadesNegocio, FeatureFlagProjector projector,
    TimeProvider clock, ILogger<AlterarStatusFeatureFlagUnidadeUseCase> logger) : IAlterarStatusFeatureFlagUnidadeUseCase
{
    public async Task<RbacResultado<FeatureFlagDto>> ExecuteAsync(Guid featureFlagId, Guid unidadeNegocioId, bool ativa, CancellationToken ct)
    {
        var flag = await flags.ObterPorIdAsync(featureFlagId, ct);
        if (flag is null)
        {
            return RbacResultado<FeatureFlagDto>.Erro(RbacFalha.FeatureFlagNaoEncontrada, "Feature Flag não encontrada.");
        }

        if (await unidadesNegocio.ObterPorIdAsync(unidadeNegocioId, ct) is null)
        {
            return RbacResultado<FeatureFlagDto>.Erro(RbacFalha.UnidadeNegocioNaoEncontrada, "Unidade de Negócio não encontrada.");
        }

        var agora = clock.GetUtcNow();
        var status = await flags.ObterStatusAsync(featureFlagId, unidadeNegocioId, ct);
        if (status is null)
        {
            status = new FeatureFlagUnidadeNegocio(featureFlagId, unidadeNegocioId, ativa, agora);
            await flags.AdicionarStatusAsync(status, ct);
        }
        else
        {
            status.DefinirAtiva(ativa, agora);
        }

        await flags.SalvarAlteracoesAsync(ct);

        logger.LogInformation(
            "Status de Feature Flag alterado. FeatureFlagId={FeatureFlagId} UnidadeNegocioId={UnidadeNegocioId} Ativa={Ativa}",
            featureFlagId, unidadeNegocioId, ativa);

        return RbacResultado<FeatureFlagDto>.Ok(await projector.ProjetarAsync(flag, ct));
    }
}
