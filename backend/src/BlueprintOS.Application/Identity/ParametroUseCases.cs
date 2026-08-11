using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Domain.Identity;
using Microsoft.Extensions.Logging;

namespace BlueprintOS.Application.Identity;

internal static class ParametroProjection
{
    public static ParametroDto Projetar(Parametro parametro) => new(
        parametro.Id, parametro.Chave, parametro.Valor, parametro.Descricao, parametro.UnidadeNegocioId);
}

/// <summary>O1.11 — Parâmetros gerais por Unidade de Negócio (ou globais, quando <c>UnidadeNegocioId</c>
/// é nulo). Exclusão física é aceitável aqui (decisão registrada na Work Order): Parâmetro não é dado
/// mestre de ERP e não possui histórico externo a preservar, ao contrário de Unidade de Negócio/Unidade
/// de Alocação/Centro de Custo.</summary>
public sealed class ListarParametrosUseCase(IParametroRepository parametros) : IListarParametrosUseCase
{
    public async Task<IReadOnlyList<ParametroDto>> ExecuteAsync(Guid? unidadeNegocioId, CancellationToken ct)
    {
        var encontrados = await parametros.ListarAsync(unidadeNegocioId, ct);
        return encontrados.Select(ParametroProjection.Projetar).ToArray();
    }
}

public sealed class CriarParametroUseCase(
    IParametroRepository parametros, IUnidadeNegocioRepository unidadesNegocio, TimeProvider clock,
    ILogger<CriarParametroUseCase> logger) : ICriarParametroUseCase
{
    public async Task<RbacResultado<ParametroDto>> ExecuteAsync(ParametroCriarInput input, CancellationToken ct)
    {
        var chave = (input.Chave ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(chave))
        {
            return RbacResultado<ParametroDto>.Erro(RbacFalha.ChaveObrigatoria, "Chave do Parâmetro é obrigatória.");
        }

        if (input.UnidadeNegocioId is { } unidadeNegocioId && await unidadesNegocio.ObterPorIdAsync(unidadeNegocioId, ct) is null)
        {
            return RbacResultado<ParametroDto>.Erro(RbacFalha.UnidadeNegocioNaoEncontrada, "Unidade de Negócio não encontrada.");
        }

        // Pré-checagem amigável; a garantia real é o índice único (Chave, UnidadeNegocioId) no SQL Server.
        if (await parametros.ExisteComChaveAsync(chave, input.UnidadeNegocioId, excluirId: null, ct))
        {
            return RbacResultado<ParametroDto>.Erro(RbacFalha.ParametroDuplicado, "Já existe um Parâmetro com esta chave nesta Unidade de Negócio.");
        }

        var agora = clock.GetUtcNow();
        var parametro = new Parametro(chave, input.Valor, input.Descricao, input.UnidadeNegocioId, agora);
        await parametros.AdicionarAsync(parametro, ct);
        await parametros.SalvarAlteracoesAsync(ct);

        logger.LogInformation("Parâmetro criado. ParametroId={ParametroId} Chave={Chave}", parametro.Id, parametro.Chave);

        return RbacResultado<ParametroDto>.Ok(ParametroProjection.Projetar(parametro));
    }
}

public sealed class AtualizarParametroUseCase(
    IParametroRepository parametros, TimeProvider clock, ILogger<AtualizarParametroUseCase> logger) : IAtualizarParametroUseCase
{
    public async Task<RbacResultado<ParametroDto>> ExecuteAsync(Guid id, ParametroAtualizarInput input, CancellationToken ct)
    {
        var parametro = await parametros.ObterPorIdAsync(id, ct);
        if (parametro is null)
        {
            return RbacResultado<ParametroDto>.Erro(RbacFalha.ParametroNaoEncontrado, "Parâmetro não encontrado.");
        }

        parametro.AtualizarValor(input.Valor, input.Descricao, clock.GetUtcNow());
        await parametros.SalvarAlteracoesAsync(ct);

        logger.LogInformation("Parâmetro atualizado. ParametroId={ParametroId}", parametro.Id);

        return RbacResultado<ParametroDto>.Ok(ParametroProjection.Projetar(parametro));
    }
}

public sealed class ExcluirParametroUseCase(
    IParametroRepository parametros, ILogger<ExcluirParametroUseCase> logger) : IExcluirParametroUseCase
{
    public async Task<RbacResultado<bool>> ExecuteAsync(Guid id, CancellationToken ct)
    {
        var parametro = await parametros.ObterPorIdAsync(id, ct);
        if (parametro is null)
        {
            return RbacResultado<bool>.Erro(RbacFalha.ParametroNaoEncontrado, "Parâmetro não encontrado.");
        }

        await parametros.RemoverAsync(parametro, ct);
        await parametros.SalvarAlteracoesAsync(ct);

        logger.LogInformation("Parâmetro excluído. ParametroId={ParametroId}", id);

        return RbacResultado<bool>.Ok(true);
    }
}
