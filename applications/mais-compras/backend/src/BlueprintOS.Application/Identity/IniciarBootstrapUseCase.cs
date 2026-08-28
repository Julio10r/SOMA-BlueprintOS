using System.Security.Cryptography;
using System.Text;
using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Application.Identity.Security;
using BlueprintOS.Domain.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BlueprintOS.Application.Identity;

/// <summary>Passo 0 do fluxo de Bootstrap (security-design-auth-o1.4.md §20.6; Work Order O1.4.3, seção 7/9/10).
/// Reaproveita, sem duplicar, os mesmos componentes de <c>Application/Identity</c> já usados pelo login
/// normal: <see cref="OtpHasher"/>, <see cref="OtpCodeGenerator"/>, <see cref="OtpRequestThrottle"/>,
/// <see cref="IOtpEmailSender"/> e a própria entidade <see cref="CodigoVerificacaoOtp"/> (estendida, não
/// duplicada — seção 11). Nenhum mecanismo de autenticação paralelo é criado.
///
/// Ordem de validação exigida (§20.6): (1) <c>Concluido == false</c>; (2) secret válido em tempo constante;
/// (3) e-mail pertence à allowlist pré-autorizada. Resposta ao cliente é idêntica para os casos (2)/(3)/
/// sucesso — apenas o caso "Concluido == true" produz um resultado distinto (<see cref="IniciarBootstrapResultado.BootstrapDisponivel"/>
/// = false), traduzido pelo controller em 404 indistinguível de rota inexistente.</summary>
public sealed class IniciarBootstrapUseCase(
    IBootstrapEstadoRepository estados,
    IBootstrapSessaoRepository bootstrapSessoes,
    ICodigoVerificacaoOtpRepository codigos,
    IOtpRequestThrottleRepository throttles,
    IOtpEmailSender emailSender,
    TimeProvider clock,
    IOptions<BootstrapSecretOptions> secretOptions,
    IOptions<BootstrapAllowedCandidatesOptions> allowedCandidatesOptions,
    IOptions<OtpRequestThrottleOptions> throttleOptions,
    ILogger<IniciarBootstrapUseCase> logger) : IIniciarBootstrapUseCase
{
    private const int MaxTentativasConcorrencia = 3;

    public async Task<IniciarBootstrapResultado> ExecuteAsync(string email, string secret, CancellationToken ct)
    {
        var normalizedEmail = (email ?? string.Empty).Trim().ToLowerInvariant();
        var auditId = EmailAuditHasher.Hash(normalizedEmail);
        var agora = clock.GetUtcNow();

        var estado = await estados.ObterAsync(ct);
        if (estado is null || estado.Concluido)
        {
            // Encerramento permanente (§20.10): a checagem de Concluido acontece antes de qualquer outra
            // validação — o controller traduz este resultado em 404, indistinguível de rota inexistente,
            // nunca 403 (que confirmaria a existência do endpoint). Linha ausente é tratada com a mesma
            // severidade de "concluído" (fail-closed, seção 12 da Work Order).
            logger.LogInformation("Tentativa de Bootstrap iniciada após encerramento/indisponibilidade (auditId={AuditId}).", auditId);
            return new IniciarBootstrapResultado(BootstrapDisponivel: false);
        }

        var permitido = await TentarRegistrarSolicitacaoAsync(normalizedEmail, agora, ct);
        if (!permitido)
        {
            // Mesmo princípio anti-oráculo do login normal (§18.1/§3.1): nenhuma distinção de resposta
            // entre "limitado" e os demais casos de rejeição abaixo.
            logger.LogInformation("Tentativa de Bootstrap além do limite permitido (auditId={AuditId}).", auditId);
            return new IniciarBootstrapResultado(BootstrapDisponivel: true);
        }

        var secretValido = SecretEhValido(secret);
        var emailAutorizado = allowedCandidatesOptions.Value.Autoriza(normalizedEmail);

        // Trabalho equivalente é executado independentemente do resultado, para reduzir side-channel de
        // timing entre os caminhos "secret inválido"/"e-mail não autorizado"/sucesso (§20.6/§20.12 —
        // mitigação de enumeração da allowlist).
        if (!secretValido)
        {
            logger.LogInformation("Tentativa de Bootstrap rejeitada — secret inválido (auditId={AuditId}).", auditId);
        }

        if (!emailAutorizado)
        {
            logger.LogInformation("Tentativa de Bootstrap rejeitada — identidade não autorizada (auditId={AuditId}).", auditId);
        }

        if (!secretValido || !emailAutorizado)
        {
            _ = OtpHasher.Hash(OtpCodeGenerator.Generate());
            return new IniciarBootstrapResultado(BootstrapDisponivel: true);
        }

        await InvalidarSessaoAnteriorAsync(normalizedEmail, agora, ct);
        await InvalidarCodigoAnteriorAsync(normalizedEmail, ct);

        var codigo = OtpCodeGenerator.Generate();
        var (hash, salt) = OtpHasher.Hash(codigo);
        var novoCodigo = CodigoVerificacaoOtp.ParaCandidatoBootstrap(normalizedEmail, hash, salt, agora);
        await codigos.AdicionarAsync(novoCodigo, ct);
        try
        {
            await codigos.SalvarAlteracoesAsync(ct);
        }
        catch (DuplicateRecordException)
        {
            // Índice único filtrado (Status=Pendente, EmailCandidato) impede dois códigos pendentes
            // simultâneos para o mesmo candidato — outra solicitação concorrente já criou um.
            logger.LogInformation("Solicitação de Bootstrap concorrente redundante descartada (auditId={AuditId}).", auditId);
            return new IniciarBootstrapResultado(BootstrapDisponivel: true);
        }

        var envio = await emailSender.SendAsync(normalizedEmail, codigo, ct);
        logger.LogInformation("OTP de Bootstrap enviado (auditId={AuditId}): sucesso={Sucesso}", auditId, envio.Success);
        logger.LogInformation("Tentativa de Bootstrap iniciada com sucesso (auditId={AuditId}).", auditId);

        return new IniciarBootstrapResultado(BootstrapDisponivel: true);
    }

    /// <summary>Comparação em tempo constante (security-design-auth-o1.4.md §20.4; Work Order O1.4.3, seção 9)
    /// — nunca <c>string.Equals</c>/<c>==</c>. Ausência de secret configurado nunca é tratada como "sempre
    /// válido": <see cref="CryptographicOperations.FixedTimeEquals"/> com um array vazio nunca corresponde
    /// a um valor recebido não vazio, e um valor recebido vazio é rejeitado explicitamente antes da
    /// comparação.</summary>
    private bool SecretEhValido(string? secretRecebido)
    {
        var secretConfigurado = secretOptions.Value.Secret ?? string.Empty;
        var recebido = secretRecebido ?? string.Empty;

        if (secretConfigurado.Length == 0 || recebido.Length == 0)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(recebido),
            Encoding.UTF8.GetBytes(secretConfigurado));
    }

    private async Task<bool> TentarRegistrarSolicitacaoAsync(string normalizedEmail, DateTimeOffset agora, CancellationToken ct)
    {
        var janela = TimeSpan.FromMinutes(throttleOptions.Value.JanelaMinutos);
        var cooldown = TimeSpan.FromSeconds(throttleOptions.Value.CooldownSegundos);
        var limite = throttleOptions.Value.MaxSolicitacoesPorJanela;

        for (var tentativa = 0; tentativa < MaxTentativasConcorrencia; tentativa++)
        {
            var throttle = await throttles.ObterPorEmailAsync(normalizedEmail, ct);
            if (throttle is null)
            {
                throttle = OtpRequestThrottle.Novo(normalizedEmail, agora);
                await throttles.AdicionarAsync(throttle, ct);
                try
                {
                    await throttles.SalvarAlteracoesAsync(ct);
                    return true;
                }
                catch (DuplicateRecordException)
                {
                    continue;
                }
            }

            var permitido = throttle.TentarRegistrar(agora, janela, limite, cooldown);
            try
            {
                await throttles.SalvarAlteracoesAsync(ct);
                return permitido;
            }
            catch (ConcurrencyConflictException)
            {
                // Outra requisição concorrente para o mesmo e-mail já atualizou o contador.
            }
        }

        // Fail-closed sob corrida extrema e persistente — nunca para o lado permissivo.
        return false;
    }

    private async Task InvalidarSessaoAnteriorAsync(string emailCandidato, DateTimeOffset agora, CancellationToken ct)
    {
        var anterior = await bootstrapSessoes.ObterAtivaPorEmailCandidatoAsync(emailCandidato, ct);
        if (anterior is null) return;

        anterior.Revogar(agora);
        await bootstrapSessoes.AtualizarAsync(anterior, ct);
        await bootstrapSessoes.SalvarAlteracoesAsync(ct);
    }

    private async Task InvalidarCodigoAnteriorAsync(string emailCandidato, CancellationToken ct)
    {
        var anterior = await codigos.ObterPendentePorEmailCandidatoAsync(emailCandidato, ct);
        if (anterior is null) return;

        anterior.InvalidarPorNovoCodigo();
        await codigos.AtualizarAsync(anterior, ct);
        try
        {
            await codigos.SalvarAlteracoesAsync(ct);
        }
        catch (ConcurrencyConflictException)
        {
            // O código anterior já estava sendo consumido/invalidado concorrentemente — a garantia final
            // de unicidade vem do índice único filtrado ao inserir o novo código, não desta invalidação
            // best-effort (mesmo princípio do login normal).
        }
    }
}
