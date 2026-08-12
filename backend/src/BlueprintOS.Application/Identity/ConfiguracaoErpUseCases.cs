using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Domain.Identity;
using Microsoft.Extensions.Logging;

namespace BlueprintOS.Application.Identity;

internal static class ConfiguracaoErpProjection
{
    public static ConfiguracaoErpDto Projetar(ConfiguracaoErp configuracao) => new(
        configuracao.Id,
        configuracao.UnidadeNegocioId,
        configuracao.SistemaErp,
        configuracao.ParametrosConfigurados,
        configuracao.EstaAtivo(),
        configuracao.CriadoEm,
        configuracao.AtualizadoEm);
}

/// <summary>O1.11 — Configuração de ERP por Unidade de Negócio. Puramente registro de configuração: não
/// realiza nenhuma operação real de leitura/escrita no ERP (isso permanece com os leitores de
/// Filial/Centro de Custo já existentes).</summary>
public sealed class ObterConfiguracaoErpUseCase(
    IUnidadeNegocioRepository unidadesNegocio, IConfiguracaoErpRepository configuracoes) : IObterConfiguracaoErpUseCase
{
    public async Task<RbacResultado<ConfiguracaoErpDto?>> ExecuteAsync(Guid unidadeNegocioId, CancellationToken ct)
    {
        if (await unidadesNegocio.ObterPorIdAsync(unidadeNegocioId, ct) is null)
        {
            return RbacResultado<ConfiguracaoErpDto?>.Erro(RbacFalha.UnidadeNegocioNaoEncontrada, "Unidade de Negócio não encontrada.");
        }

        var configuracao = await configuracoes.ObterPorUnidadeNegocioAsync(unidadeNegocioId, ct);
        return RbacResultado<ConfiguracaoErpDto?>.Ok(configuracao is null ? null : ConfiguracaoErpProjection.Projetar(configuracao));
    }
}

/// <summary>Cria ou atualiza a configuração 1:1 da Unidade de Negócio (idempotente por design: não há
/// endpoint de criação separado do de edição, evitando um estado inconsistente de "duas configurações"
/// para a mesma UN).</summary>
public sealed class SalvarConfiguracaoErpUseCase(
    IUnidadeNegocioRepository unidadesNegocio, IConfiguracaoErpRepository configuracoes, IConfiguracaoErpSegredoProtector protector,
    TimeProvider clock, ILogger<SalvarConfiguracaoErpUseCase> logger) : ISalvarConfiguracaoErpUseCase
{
    public async Task<RbacResultado<ConfiguracaoErpDto>> ExecuteAsync(Guid unidadeNegocioId, ConfiguracaoErpInput input, CancellationToken ct)
    {
        if (await unidadesNegocio.ObterPorIdAsync(unidadeNegocioId, ct) is null)
        {
            return RbacResultado<ConfiguracaoErpDto>.Erro(RbacFalha.UnidadeNegocioNaoEncontrada, "Unidade de Negócio não encontrada.");
        }

        var sistemaErp = (input.SistemaErp ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(sistemaErp))
        {
            return RbacResultado<ConfiguracaoErpDto>.Erro(RbacFalha.SistemaErpObrigatorio, "Sistema ERP é obrigatório.");
        }

        var parametrosProtegidos = string.IsNullOrEmpty(input.ParametrosConexao) ? null : protector.Proteger(input.ParametrosConexao);
        var agora = clock.GetUtcNow();

        var existente = await configuracoes.ObterPorUnidadeNegocioAsync(unidadeNegocioId, ct);
        if (existente is not null)
        {
            existente.Editar(sistemaErp, parametrosProtegidos, agora);
            await configuracoes.SalvarAlteracoesAsync(ct);

            logger.LogInformation(
                "Configuração de ERP atualizada. UnidadeNegocioId={UnidadeNegocioId} SistemaErp={SistemaErp}",
                unidadeNegocioId, existente.SistemaErp);

            return RbacResultado<ConfiguracaoErpDto>.Ok(ConfiguracaoErpProjection.Projetar(existente));
        }

        var configuracao = new ConfiguracaoErp(unidadeNegocioId, sistemaErp, parametrosProtegidos, agora);
        await configuracoes.AdicionarAsync(configuracao, ct);
        await configuracoes.SalvarAlteracoesAsync(ct);

        logger.LogInformation(
            "Configuração de ERP criada. UnidadeNegocioId={UnidadeNegocioId} SistemaErp={SistemaErp}",
            unidadeNegocioId, configuracao.SistemaErp);

        return RbacResultado<ConfiguracaoErpDto>.Ok(ConfiguracaoErpProjection.Projetar(configuracao));
    }
}

public sealed class AlterarStatusConfiguracaoErpUseCase(
    IUnidadeNegocioRepository unidadesNegocio, IConfiguracaoErpRepository configuracoes, TimeProvider clock,
    ILogger<AlterarStatusConfiguracaoErpUseCase> logger) : IAlterarStatusConfiguracaoErpUseCase
{
    public async Task<RbacResultado<ConfiguracaoErpDto>> ExecuteAsync(Guid unidadeNegocioId, bool ativo, CancellationToken ct)
    {
        if (await unidadesNegocio.ObterPorIdAsync(unidadeNegocioId, ct) is null)
        {
            return RbacResultado<ConfiguracaoErpDto>.Erro(RbacFalha.UnidadeNegocioNaoEncontrada, "Unidade de Negócio não encontrada.");
        }

        var configuracao = await configuracoes.ObterPorUnidadeNegocioAsync(unidadeNegocioId, ct);
        if (configuracao is null)
        {
            return RbacResultado<ConfiguracaoErpDto>.Erro(RbacFalha.ConfiguracaoErpNaoEncontrada, "Configuração de ERP não encontrada para esta Unidade de Negócio.");
        }

        var agora = clock.GetUtcNow();
        if (ativo) configuracao.Ativar(agora); else configuracao.Inativar(agora);
        await configuracoes.SalvarAlteracoesAsync(ct);

        logger.LogInformation(
            "Status da Configuração de ERP alterado. UnidadeNegocioId={UnidadeNegocioId} Ativo={Ativo}", unidadeNegocioId, ativo);

        return RbacResultado<ConfiguracaoErpDto>.Ok(ConfiguracaoErpProjection.Projetar(configuracao));
    }
}
