namespace BlueprintOS.Domain.Identity;

/// <summary>Sessão persistida server-side (modelo ratificado — security-design-auth-o1.4.md, seção 1.2/17).
/// O browser recebe apenas um identificador opaco; nenhuma informação de identidade/autorização é
/// armazenada no cookie. Aqui armazenamos apenas o hash do identificador, nunca o valor em claro
/// (mesmo princípio do OTP) — um dump de banco não permite sequestro direto de sessões ativas.</summary>
public sealed class SessaoAutenticacao
{
    public Guid Id { get; private set; }
    public Guid UsuarioId { get; private set; }
    public Guid UnidadeNegocioId { get; private set; }
    public string IdentificadorHash { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset LastActivityAt { get; private set; }
    public DateTimeOffset AbsoluteExpiresAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }

    private SessaoAutenticacao() { IdentificadorHash = string.Empty; }

    public SessaoAutenticacao(Guid usuarioId, Guid unidadeNegocioId, string identificadorHash, DateTimeOffset now, TimeSpan duracaoAbsoluta)
    {
        if (string.IsNullOrWhiteSpace(identificadorHash)) throw new ArgumentException("Hash do identificador de sessão é obrigatório.", nameof(identificadorHash));

        Id = Guid.NewGuid();
        UsuarioId = usuarioId;
        UnidadeNegocioId = unidadeNegocioId;
        IdentificadorHash = identificadorHash;
        CreatedAt = now;
        LastActivityAt = now;
        AbsoluteExpiresAt = now.Add(duracaoAbsoluta);
    }

    public bool EstaAtivaEm(DateTimeOffset momento, TimeSpan expiracaoPorInatividade) =>
        RevokedAt is null && momento < AbsoluteExpiresAt && (momento - LastActivityAt) < expiracaoPorInatividade;

    public void RegistrarAtividade(DateTimeOffset momento) => LastActivityAt = momento;

    public void Revogar(DateTimeOffset momento) => RevokedAt ??= momento;
}
