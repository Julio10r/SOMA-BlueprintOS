using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Application.Identity.Security;
using Microsoft.Extensions.Options;

namespace BlueprintOS.Application.Identity;

/// <summary>Resolve a identidade a partir do identificador opaco de sessão, revalidando a cada chamada
/// que a sessão está ativa e que o usuário permanece Ativo — nunca confiando em estado congelado no login
/// (security-design-auth-o1.4.md, §2.5).</summary>
public sealed class ObterIdentidadeAtualUseCase(
    ISessaoAutenticacaoRepository sessoes,
    IUsuarioRepository usuarios,
    TimeProvider clock,
    IOptions<AuthSessionOptions> sessionOptions) : IObterIdentidadeAtualUseCase
{
    public async Task<IdentidadeAtualDto?> ExecuteAsync(string sessionRawToken, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(sessionRawToken)) return null;

        var hash = OpaqueSessionToken.Hash(sessionRawToken);
        var sessao = await sessoes.ObterPorIdentificadorHashAsync(hash, ct);
        if (sessao is null) return null;

        var agora = clock.GetUtcNow();
        var inatividade = TimeSpan.FromMinutes(sessionOptions.Value.InactivityTimeoutMinutes);
        if (!sessao.EstaAtivaEm(agora, inatividade)) return null;

        var usuario = await usuarios.ObterPorIdAsync(sessao.UsuarioId, ct);
        if (usuario is null || !usuario.EstaAtivo()) return null;

        sessao.RegistrarAtividade(agora);
        await sessoes.AtualizarAsync(sessao, ct);
        await sessoes.SalvarAlteracoesAsync(ct);

        return new IdentidadeAtualDto(usuario.Id, usuario.Email, usuario.Nome, sessao.UnidadeNegocioId);
    }
}
