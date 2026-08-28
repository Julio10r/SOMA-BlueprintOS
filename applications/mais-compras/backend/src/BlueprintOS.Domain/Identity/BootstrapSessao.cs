namespace BlueprintOS.Domain.Identity;

/// <summary>Sessão do fluxo de Bootstrap (security-design-auth-o1.4.md §20.7; Work Order O1.4.3, seção 8) —
/// distinta de <see cref="SessaoAutenticacao"/>: sem <c>UsuarioId</c> (o usuário ainda não existe até a
/// conclusão do Bootstrap, O1.4.3.2), vida útil curta e absoluta (15 minutos, sem renovação por atividade —
/// ao contrário da sessão normal), e uso único (nunca reemitida após sucesso ou falha definitiva). Reaproveita
/// a mesma primitiva criptográfica de <see cref="Security.OpaqueSessionToken"/> já usada pela sessão normal —
/// apenas o hash do identificador é persistido, nunca o valor bruto.</summary>
public sealed class BootstrapSessao
{
    public static readonly TimeSpan Validade = TimeSpan.FromMinutes(15);

    public Guid Id { get; private set; }
    public string EmailCandidato { get; private set; }
    public string IdentificadorHash { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? UsadaEm { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }

    private BootstrapSessao()
    {
        EmailCandidato = string.Empty;
        IdentificadorHash = string.Empty;
    }

    public BootstrapSessao(string emailCandidato, string identificadorHash, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(emailCandidato)) throw new ArgumentException("E-mail candidato é obrigatório.", nameof(emailCandidato));
        if (string.IsNullOrWhiteSpace(identificadorHash)) throw new ArgumentException("Hash do identificador é obrigatório.", nameof(identificadorHash));

        Id = Guid.NewGuid();
        EmailCandidato = emailCandidato;
        IdentificadorHash = identificadorHash;
        CreatedAt = now;
        ExpiresAt = now.Add(Validade);
    }

    /// <summary>Válida apenas se nunca usada, nunca revogada e dentro do prazo absoluto — nunca renovada por
    /// atividade (ao contrário de <see cref="SessaoAutenticacao.EstaAtivaEm"/>); um fluxo de Bootstrap não
    /// deve poder ficar "pendurado" indefinidamente por atividade intermitente (Work Order O1.4.3, seção 8).</summary>
    public bool EstaValidaEm(DateTimeOffset momento) =>
        UsadaEm is null && RevokedAt is null && momento < ExpiresAt;

    /// <summary>Marca a sessão como consumida — a sessão Bootstrap nunca é reutilizável, mesmo dentro do
    /// prazo de expiração, mesmo em caso de sucesso.</summary>
    public void MarcarUsada(DateTimeOffset momento) => UsadaEm ??= momento;

    /// <summary>Revoga explicitamente em qualquer falha definitiva do fluxo — nunca reaproveitável após.</summary>
    public void Revogar(DateTimeOffset momento) => RevokedAt ??= momento;
}
