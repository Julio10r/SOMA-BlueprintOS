using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Application.Identity.Security;
using BlueprintOS.Domain.Identity;
using Microsoft.Extensions.Logging;

namespace BlueprintOS.Application.Identity;

/// <summary>Valida o OTP do candidato de Bootstrap e, em caso de sucesso, cria a <see cref="BootstrapSessao"/>
/// (security-design-auth-o1.4.md §20.6/§20.7; Work Order O1.4.3, seção 8). Reaproveita, sem duplicar,
/// <see cref="OtpHasher"/> e a mesma entidade <see cref="CodigoVerificacaoOtp"/> do login normal — apenas a
/// consulta é por e-mail candidato, não por <c>UsuarioId</c>.</summary>
public sealed class ValidarOtpBootstrapUseCase(
    IBootstrapEstadoRepository estados,
    ICodigoVerificacaoOtpRepository codigos,
    IBootstrapSessaoRepository bootstrapSessoes,
    TimeProvider clock,
    ILogger<ValidarOtpBootstrapUseCase> logger) : IValidarOtpBootstrapUseCase
{
    private const string MotivoGenerico = "Código inválido ou expirado.";

    public async Task<ValidarOtpBootstrapResultado> ExecuteAsync(string email, string codigo, CancellationToken ct)
    {
        var normalizedEmail = (email ?? string.Empty).Trim().ToLowerInvariant();
        var auditId = EmailAuditHasher.Hash(normalizedEmail);
        var agora = clock.GetUtcNow();

        var estado = await estados.ObterAsync(ct);
        if (estado is null || estado.Concluido)
        {
            // Defesa em profundidade: mesmo que uma tentativa de /bootstrap/iniciar tenha sido concluída
            // antes do encerramento do Bootstrap, nenhuma validação de OTP produz efeito depois de
            // Concluido == true (§20.10). Este endpoint não tem o requisito explícito de 404 (esse é
            // exclusivo de /bootstrap/iniciar, §20.6 passo 2) — decisão de implementação desta etapa,
            // registrada no relatório de conclusão.
            logger.LogInformation("Validação de OTP de Bootstrap rejeitada — Bootstrap indisponível/concluído (auditId={AuditId}).", auditId);
            return new ValidarOtpBootstrapResultado(false, MotivoGenerico, null, null);
        }

        var pendente = await codigos.ObterPendentePorEmailCandidatoAsync(normalizedEmail, ct);
        if (pendente is null || !pendente.EstaValidoEm(agora))
        {
            logger.LogInformation("Validação de OTP de Bootstrap rejeitada — código ausente/expirado (auditId={AuditId}).", auditId);
            return new ValidarOtpBootstrapResultado(false, MotivoGenerico, null, null);
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
                // O código foi consumido/invalidado concorrentemente — a rejeição já é o resultado correto.
            }
            logger.LogInformation("Tentativa de validação de OTP de Bootstrap incorreta (auditId={AuditId}).", auditId);
            return new ValidarOtpBootstrapResultado(false, MotivoGenerico, null, null);
        }

        pendente.Consumir();
        await codigos.AtualizarAsync(pendente, ct);
        try
        {
            await codigos.SalvarAlteracoesAsync(ct);
        }
        catch (ConcurrencyConflictException)
        {
            // Consumo único atômico: outra requisição concorrente já consumiu este exato código —
            // esta chamada perde a corrida e é rejeitada com o mesmo motivo genérico, nunca cria sessão.
            logger.LogInformation("Validação de OTP de Bootstrap perdeu corrida de concorrência (auditId={AuditId}).", auditId);
            return new ValidarOtpBootstrapResultado(false, MotivoGenerico, null, null);
        }

        // Nunca reutilizar identificador pré-existente — sempre um novo, gerado após validação bem-sucedida
        // (prevenção de session fixation, mesmo princípio do login normal §2.4).
        var rawToken = OpaqueSessionToken.GenerateRawToken();
        var hash = OpaqueSessionToken.Hash(rawToken);
        var sessao = new BootstrapSessao(normalizedEmail, hash, agora);
        await bootstrapSessoes.AdicionarAsync(sessao, ct);
        await bootstrapSessoes.SalvarAlteracoesAsync(ct);

        logger.LogInformation("OTP de Bootstrap validado com sucesso (auditId={AuditId}).", auditId);
        return new ValidarOtpBootstrapResultado(true, null, rawToken, normalizedEmail);
    }
}
