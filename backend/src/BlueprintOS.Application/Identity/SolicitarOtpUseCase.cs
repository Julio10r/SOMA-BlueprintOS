using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Application.Identity.Security;
using BlueprintOS.Domain.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BlueprintOS.Application.Identity;

/// <summary>Solicita um OTP para o e-mail informado. Resposta é sempre a mesma independentemente de o
/// usuário existir/estar ativo — anti-enumeração (security-design-auth-o1.4.md, §2.1). O throttle por
/// e-mail (O1.4.2.1, Achado A) é aplicado antes e da mesma forma para e-mail existente/inexistente, para
/// que o próprio mecanismo de throttle nunca seja um oráculo de enumeração.</summary>
public sealed class SolicitarOtpUseCase(
    IUsuarioRepository usuarios,
    ICodigoVerificacaoOtpRepository codigos,
    IOtpRequestThrottleRepository throttles,
    IOtpEmailSender emailSender,
    TimeProvider clock,
    IOptions<OtpRequestThrottleOptions> throttleOptions,
    ILogger<SolicitarOtpUseCase> logger) : ISolicitarOtpUseCase
{
    private const int MaxTentativasConcorrencia = 3;

    public async Task<SolicitarOtpResultado> ExecuteAsync(string email, CancellationToken ct)
    {
        var normalizedEmail = (email ?? string.Empty).Trim().ToLowerInvariant();
        var auditId = EmailAuditHasher.Hash(normalizedEmail);
        var agora = clock.GetUtcNow();

        var permitido = await TentarRegistrarSolicitacaoAsync(normalizedEmail, agora, ct);
        if (!permitido)
        {
            // Nenhuma distinção de resposta/tempo entre "limitado" e os demais casos abaixo — o
            // throttle nunca pode ser usado para inferir se o e-mail existe (Achado A).
            logger.LogInformation("OTP solicitado além do limite permitido (auditId={AuditId}).", auditId);
            return new SolicitarOtpResultado(DomainAutorizado: true);
        }

        var usuario = await usuarios.ObterPorEmailAsync(normalizedEmail, ct);

        // Trabalho equivalente é executado tanto para usuário válido quanto inválido, para reduzir
        // side-channel de timing entre os dois caminhos (§2.1).
        if (usuario is null || !usuario.EstaAtivo())
        {
            _ = OtpHasher.Hash(OtpCodeGenerator.Generate());
            logger.LogInformation("OTP solicitado para e-mail não elegível (auditId={AuditId}).", auditId);
            return new SolicitarOtpResultado(DomainAutorizado: true);
        }

        await InvalidarCodigoAnteriorAsync(usuario.Id, ct);

        var codigo = OtpCodeGenerator.Generate();
        var (hash, salt) = OtpHasher.Hash(codigo);
        var novoCodigo = new CodigoVerificacaoOtp(usuario.Id, hash, salt, agora);
        await codigos.AdicionarAsync(novoCodigo, ct);
        try
        {
            await codigos.SalvarAlteracoesAsync(ct);
        }
        catch (DuplicateRecordException)
        {
            // Índice único filtrado (Status=Pendente) impede dois códigos pendentes simultâneos —
            // outra solicitação concorrente já criou um; esta é tratada como redundante, sem criar
            // um segundo código nem revelar a diferença ao chamador (Achado B/reenvio).
            logger.LogInformation("Solicitação de OTP concorrente redundante descartada (auditId={AuditId}).", auditId);
            return new SolicitarOtpResultado(DomainAutorizado: true);
        }

        var envio = await emailSender.SendAsync(usuario.Email, codigo, ct);
        logger.LogInformation("OTP enviado (auditId={AuditId}): sucesso={Sucesso}", auditId, envio.Success);

        return new SolicitarOtpResultado(DomainAutorizado: true);
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
                    // Outra requisição concorrente criou o registro primeiro — tenta novamente lendo-o.
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
                // RowVersion mudou entre a leitura e a escrita — outra solicitação concorrente para o
                // mesmo e-mail já atualizou o contador; tenta novamente com o estado mais recente.
            }
        }

        // Sob corrida extrema e persistente, falha para o lado seguro (nega), nunca para o lado
        // permissivo — consistente com o princípio fail-closed do restante do módulo.
        return false;
    }

    private async Task InvalidarCodigoAnteriorAsync(Guid usuarioId, CancellationToken ct)
    {
        var anterior = await codigos.ObterPendentePorUsuarioAsync(usuarioId, ct);
        if (anterior is null) return;

        anterior.InvalidarPorNovoCodigo();
        await codigos.AtualizarAsync(anterior, ct);
        try
        {
            await codigos.SalvarAlteracoesAsync(ct);
        }
        catch (ConcurrencyConflictException)
        {
            // O código anterior já estava sendo consumido/invalidado por outra requisição concorrente
            // (validação ou outro reenvio) — a garantia final de unicidade vem do índice único filtrado
            // ao inserir o novo código (Status=Pendente), não desta invalidação best-effort.
        }
    }
}
