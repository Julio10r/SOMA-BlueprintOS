using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Security;
using Microsoft.Extensions.Logging;

namespace BlueprintOS.Application.Identity;

public sealed class LogoutUseCase(
    ISessaoAutenticacaoRepository sessoes,
    TimeProvider clock,
    ILogger<LogoutUseCase> logger) : ILogoutUseCase
{
    public async Task ExecuteAsync(string sessionRawToken, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(sessionRawToken)) return;

        var hash = OpaqueSessionToken.Hash(sessionRawToken);
        var sessao = await sessoes.ObterPorIdentificadorHashAsync(hash, ct);
        if (sessao is null) return;

        sessao.Revogar(clock.GetUtcNow());
        await sessoes.AtualizarAsync(sessao, ct);
        await sessoes.SalvarAlteracoesAsync(ct);
        // UsuarioId é um identificador interno (Guid), não um segredo — apto para correlação de
        // auditoria (O1.4.2.1, Achado G), diferente de OTP/sessão/token, nunca logados.
        logger.LogInformation("Logout realizado; sessão revogada (usuarioId={UsuarioId}).", sessao.UsuarioId);
    }
}
