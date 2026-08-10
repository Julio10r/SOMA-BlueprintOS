namespace BlueprintOS.Domain.Identity;

/// <summary>Contador de solicitações de OTP por e-mail normalizado — defesa por identidade, independente
/// do IP (security-design-auth-o1.4.md, §3.1/§3.3; O1.4.2.1, Achado A). Aplicado igualmente a e-mails
/// existentes/inexistentes/inativos, para que o próprio mecanismo de throttle nunca seja um oráculo de
/// enumeração de usuário.</summary>
public sealed class OtpRequestThrottle
{
    public Guid Id { get; private set; }
    public string EmailNormalizado { get; private set; }
    public DateTimeOffset JanelaIniciadaEm { get; private set; }
    public int SolicitacoesNaJanela { get; private set; }
    public DateTimeOffset UltimaSolicitacaoEm { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    private OtpRequestThrottle() { EmailNormalizado = string.Empty; }

    public static OtpRequestThrottle Novo(string emailNormalizado, DateTimeOffset agora) => new()
    {
        Id = Guid.NewGuid(),
        EmailNormalizado = emailNormalizado,
        JanelaIniciadaEm = agora,
        SolicitacoesNaJanela = 1,
        UltimaSolicitacaoEm = agora,
    };

    /// <summary>Decide se uma nova solicitação é permitida e, em caso afirmativo, já registra o efeito
    /// (nova contagem/janela) — chamada única, sem passo de "verificar depois registrar" separado, para
    /// reduzir a superfície de corrida (a garantia final de atomicidade vem do RowVersion + SaveChanges).</summary>
    public bool TentarRegistrar(DateTimeOffset agora, TimeSpan janela, int limitePorJanela, TimeSpan cooldown)
    {
        if (agora - UltimaSolicitacaoEm < cooldown)
        {
            return false;
        }

        if (agora - JanelaIniciadaEm >= janela)
        {
            JanelaIniciadaEm = agora;
            SolicitacoesNaJanela = 1;
            UltimaSolicitacaoEm = agora;
            return true;
        }

        if (SolicitacoesNaJanela >= limitePorJanela)
        {
            return false;
        }

        SolicitacoesNaJanela++;
        UltimaSolicitacaoEm = agora;
        return true;
    }
}
