using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Application.Identity.Security;
using BlueprintOS.Domain.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BlueprintOS.Application.Identity;

public sealed class ValidarOtpUseCase(
    IUsuarioRepository usuarios,
    ICodigoVerificacaoOtpRepository codigos,
    ISessaoAutenticacaoRepository sessoes,
    TimeProvider clock,
    IOptions<AuthSessionOptions> sessionOptions,
    ILogger<ValidarOtpUseCase> logger) : IValidarOtpUseCase
{
    private const string MotivoGenerico = "Código inválido ou expirado.";

    public async Task<ValidarOtpResultado> ExecuteAsync(string email, string codigo, CancellationToken ct)
    {
        var normalizedEmail = (email ?? string.Empty).Trim().ToLowerInvariant();
        var auditId = EmailAuditHasher.Hash(normalizedEmail);
        var usuario = await usuarios.ObterPorEmailAsync(normalizedEmail, ct);
        var agora = clock.GetUtcNow();

        if (usuario is null || !usuario.EstaAtivo())
        {
            logger.LogInformation("Tentativa de validação de OTP rejeitada (auditId={AuditId}, motivo genérico).", auditId);
            return new ValidarOtpResultado(false, MotivoGenerico, null, null, null, null, null);
        }

        var pendente = await codigos.ObterPendentePorUsuarioAsync(usuario.Id, ct);
        if (pendente is null || !pendente.EstaValidoEm(agora))
        {
            logger.LogInformation("Tentativa de validação de OTP rejeitada (auditId={AuditId}, código ausente/expirado).", auditId);
            return new ValidarOtpResultado(false, MotivoGenerico, null, null, null, null, null);
        }

        if (!OtpHasher.Verify(codigo ?? string.Empty, pendente.Hash, pendente.Salt))
        {
            pendente.RegistrarTentativaFalha();
            await codigos.AtualizarAsync(pendente, ct);
            try
            {
                await codigos.SalvarAlteracoesAsync(ct);
            }
            catch (ConcurrencyConflictException)
            {
                // O código foi consumido ou invalidado concorrentemente entre a leitura e esta escrita —
                // a tentativa incorreta não pôde ser registrada nele, mas o resultado (rejeição) já é
                // o correto de qualquer forma.
            }
            logger.LogInformation("Tentativa de validação de OTP incorreta (auditId={AuditId}).", auditId);
            return new ValidarOtpResultado(false, MotivoGenerico, null, null, null, null, null);
        }

        pendente.Consumir();
        await codigos.AtualizarAsync(pendente, ct);
        try
        {
            await codigos.SalvarAlteracoesAsync(ct);
        }
        catch (ConcurrencyConflictException)
        {
            // Consumo único atômico (O1.4.2.1, Achado B): outra requisição concorrente já consumiu este
            // exato código entre a leitura e esta escrita (RowVersion não corresponde mais). Esta
            // chamada perde a corrida e é rejeitada com o mesmo motivo genérico — nunca cria sessão.
            logger.LogInformation("Validação de OTP perdeu corrida de concorrência (auditId={AuditId}).", auditId);
            return new ValidarOtpResultado(false, MotivoGenerico, null, null, null, null, null);
        }

        // Nunca reutilizar identificador pré-existente — sempre um novo, gerado após a validação
        // bem-sucedida (prevenção de session fixation, §2.4).
        var rawToken = OpaqueSessionToken.GenerateRawToken();
        var hash = OpaqueSessionToken.Hash(rawToken);
        var duracaoAbsoluta = TimeSpan.FromHours(sessionOptions.Value.AbsoluteExpirationHours);
        var sessao = new SessaoAutenticacao(usuario.Id, usuario.UnidadeNegocioId, hash, agora, duracaoAbsoluta);
        await sessoes.AdicionarAsync(sessao, ct);
        await sessoes.SalvarAlteracoesAsync(ct);

        logger.LogInformation("Login realizado com sucesso (auditId={AuditId}).", auditId);
        return new ValidarOtpResultado(true, null, rawToken, usuario.Id, usuario.Email, usuario.Nome, usuario.UnidadeNegocioId);
    }
}
