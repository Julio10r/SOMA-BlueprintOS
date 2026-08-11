using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Domain.Identity;
using Microsoft.Extensions.Logging;

namespace BlueprintOS.Application.Identity;

internal static class ConfiguracaoNotificacaoProjection
{
    public static ConfiguracaoNotificacaoDto Projetar(ConfiguracaoNotificacao configuracao) => new(
        configuracao.Id,
        configuracao.UnidadeNegocioId,
        configuracao.EmailAtivado,
        configuracao.EmailRemetente,
        configuracao.NomeRemetente,
        configuracao.CriadoEm,
        configuracao.AtualizadoEm);
}

/// <summary>O1.11, item #24 — Configuração de Notificações por Unidade de Negócio. ESCOPO MÍNIMO DE
/// FUNDAÇÃO (decisão formal do Product Owner): apenas registro de configuração do canal e-mail
/// (ativado/inativado, remetente, nome do remetente). Nenhum envio real de e-mail acontece por meio destes
/// casos de uso.</summary>
public sealed class ObterConfiguracaoNotificacaoUseCase(
    IUnidadeNegocioRepository unidadesNegocio, IConfiguracaoNotificacaoRepository configuracoes) : IObterConfiguracaoNotificacaoUseCase
{
    public async Task<RbacResultado<ConfiguracaoNotificacaoDto?>> ExecuteAsync(Guid unidadeNegocioId, CancellationToken ct)
    {
        if (await unidadesNegocio.ObterPorIdAsync(unidadeNegocioId, ct) is null)
        {
            return RbacResultado<ConfiguracaoNotificacaoDto?>.Erro(RbacFalha.UnidadeNegocioNaoEncontrada, "Unidade de Negócio não encontrada.");
        }

        var configuracao = await configuracoes.ObterPorUnidadeNegocioAsync(unidadeNegocioId, ct);
        return RbacResultado<ConfiguracaoNotificacaoDto?>.Ok(configuracao is null ? null : ConfiguracaoNotificacaoProjection.Projetar(configuracao));
    }
}

/// <summary>Cria ou atualiza a configuração 1:1 da Unidade de Negócio (idempotente por design, mesmo
/// padrão de <see cref="SalvarConfiguracaoErpUseCase"/>: não há endpoint de criação separado do de
/// edição).</summary>
public sealed class SalvarConfiguracaoNotificacaoUseCase(
    IUnidadeNegocioRepository unidadesNegocio, IConfiguracaoNotificacaoRepository configuracoes, TimeProvider clock,
    ILogger<SalvarConfiguracaoNotificacaoUseCase> logger) : ISalvarConfiguracaoNotificacaoUseCase
{
    public async Task<RbacResultado<ConfiguracaoNotificacaoDto>> ExecuteAsync(Guid unidadeNegocioId, ConfiguracaoNotificacaoInput input, CancellationToken ct)
    {
        if (await unidadesNegocio.ObterPorIdAsync(unidadeNegocioId, ct) is null)
        {
            return RbacResultado<ConfiguracaoNotificacaoDto>.Erro(RbacFalha.UnidadeNegocioNaoEncontrada, "Unidade de Negócio não encontrada.");
        }

        var emailRemetente = (input.EmailRemetente ?? string.Empty).Trim();
        var nomeRemetente = input.NomeRemetente;

        if (input.EmailAtivado && string.IsNullOrWhiteSpace(emailRemetente))
        {
            return RbacResultado<ConfiguracaoNotificacaoDto>.Erro(
                RbacFalha.EmailRemetenteInvalido, "E-mail remetente é obrigatório para ativar notificações por e-mail.");
        }

        if (!string.IsNullOrWhiteSpace(emailRemetente) && !EmailUsuarioValidator.EhValido(emailRemetente))
        {
            return RbacResultado<ConfiguracaoNotificacaoDto>.Erro(RbacFalha.EmailRemetenteInvalido, "E-mail remetente em formato inválido.");
        }

        var agora = clock.GetUtcNow();
        var existente = await configuracoes.ObterPorUnidadeNegocioAsync(unidadeNegocioId, ct);
        if (existente is not null)
        {
            existente.Editar(input.EmailAtivado, emailRemetente, nomeRemetente, agora);
            await configuracoes.SalvarAlteracoesAsync(ct);

            logger.LogInformation(
                "Configuração de Notificações atualizada. UnidadeNegocioId={UnidadeNegocioId} EmailAtivado={EmailAtivado}",
                unidadeNegocioId, existente.EmailAtivado);

            return RbacResultado<ConfiguracaoNotificacaoDto>.Ok(ConfiguracaoNotificacaoProjection.Projetar(existente));
        }

        var configuracao = new ConfiguracaoNotificacao(unidadeNegocioId, input.EmailAtivado, emailRemetente, nomeRemetente, agora);
        await configuracoes.AdicionarAsync(configuracao, ct);
        await configuracoes.SalvarAlteracoesAsync(ct);

        logger.LogInformation(
            "Configuração de Notificações criada. UnidadeNegocioId={UnidadeNegocioId} EmailAtivado={EmailAtivado}",
            unidadeNegocioId, configuracao.EmailAtivado);

        return RbacResultado<ConfiguracaoNotificacaoDto>.Ok(ConfiguracaoNotificacaoProjection.Projetar(configuracao));
    }
}
