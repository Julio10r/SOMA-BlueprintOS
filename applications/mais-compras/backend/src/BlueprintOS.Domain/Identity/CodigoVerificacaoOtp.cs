namespace BlueprintOS.Domain.Identity;

/// <summary>Código OTP de autenticação. Nunca persiste o código em texto claro — apenas hash+salt
/// (security-design-auth-o1.4.md, seção 3.1). Uso único, validade curta e limite de tentativas.</summary>
public sealed class CodigoVerificacaoOtp
{
    public const int MaxTentativas = 5;
    public static readonly TimeSpan Validade = TimeSpan.FromMinutes(10);

    public Guid Id { get; private set; }
    public Guid? UsuarioId { get; private set; }

    /// <summary>E-mail candidato pré-autorizado do fluxo de Bootstrap (Work Order O1.4.3, seção 11 — opção
    /// recomendada, adotada nesta implementação). Mutuamente exclusivo com <see cref="UsuarioId"/>: para
    /// código emitido pelo login normal, permanece <c>null</c>; para código emitido pelo Bootstrap,
    /// <see cref="UsuarioId"/> permanece <c>null</c> e este campo identifica o destinatário — nunca um
    /// <c>UsuarioId</c> sintético/reservado (opção descartada pela Work Order). Reaproveita 100% do
    /// hashing/tentativas/expiração/RowVersion desta mesma classe, sem duplicar o mecanismo.</summary>
    public string? EmailCandidato { get; private set; }
    public string Hash { get; private set; }
    public string Salt { get; private set; }
    public DateTimeOffset CriadoEm { get; private set; }
    public DateTimeOffset ExpiraEm { get; private set; }
    public int Tentativas { get; private set; }
    public StatusCodigoVerificacaoOtp Status { get; private set; }

    /// <summary>Token de concorrência otimista (O1.4.2.1, Achado B) — garante consumo único atômico:
    /// duas validações concorrentes do mesmo código produzem exatamente um sucesso e uma
    /// <c>DbUpdateConcurrencyException</c> na perdedora, nunca duas sessões a partir do mesmo OTP.</summary>
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    private CodigoVerificacaoOtp() { Hash = string.Empty; Salt = string.Empty; }

    public CodigoVerificacaoOtp(Guid usuarioId, string hash, string salt, DateTimeOffset criadoEm)
    {
        if (string.IsNullOrWhiteSpace(hash)) throw new ArgumentException("Hash do OTP é obrigatório.", nameof(hash));
        if (string.IsNullOrWhiteSpace(salt)) throw new ArgumentException("Salt do OTP é obrigatório.", nameof(salt));

        Id = Guid.NewGuid();
        UsuarioId = usuarioId;
        EmailCandidato = null;
        Hash = hash;
        Salt = salt;
        CriadoEm = criadoEm;
        ExpiraEm = criadoEm.Add(Validade);
        Tentativas = 0;
        Status = StatusCodigoVerificacaoOtp.Pendente;
    }

    /// <summary>Factory exclusiva do fluxo de Bootstrap (Work Order O1.4.3, seção 11) — candidato ainda sem
    /// <see cref="Usuario"/> existente. <paramref name="emailCandidato"/> já deve chegar normalizado
    /// (trim + lower invariant) pelo chamador, mesma normalização já usada em <see cref="Usuario"/>/
    /// <see cref="OtpRequestThrottle"/>.</summary>
    public static CodigoVerificacaoOtp ParaCandidatoBootstrap(string emailCandidato, string hash, string salt, DateTimeOffset criadoEm)
    {
        if (string.IsNullOrWhiteSpace(emailCandidato)) throw new ArgumentException("E-mail candidato é obrigatório.", nameof(emailCandidato));
        if (string.IsNullOrWhiteSpace(hash)) throw new ArgumentException("Hash do OTP é obrigatório.", nameof(hash));
        if (string.IsNullOrWhiteSpace(salt)) throw new ArgumentException("Salt do OTP é obrigatório.", nameof(salt));

        return new CodigoVerificacaoOtp
        {
            Id = Guid.NewGuid(),
            UsuarioId = null,
            EmailCandidato = emailCandidato,
            Hash = hash,
            Salt = salt,
            CriadoEm = criadoEm,
            ExpiraEm = criadoEm.Add(Validade),
            Tentativas = 0,
            Status = StatusCodigoVerificacaoOtp.Pendente,
        };
    }

    public bool EstaValidoEm(DateTimeOffset momento) =>
        Status == StatusCodigoVerificacaoOtp.Pendente && momento < ExpiraEm && Tentativas < MaxTentativas;

    /// <summary>Marca o código como consumido, atomicamente, na primeira validação bem-sucedida.
    /// Qualquer tentativa seguinte com o mesmo código deve ser rejeitada mesmo dentro da validade.</summary>
    public void Consumir()
    {
        Status = StatusCodigoVerificacaoOtp.Consumido;
    }

    /// <summary>Registra uma tentativa de validação incorreta; invalida o código ao exceder o limite.</summary>
    public void RegistrarTentativaFalha()
    {
        Tentativas++;
        if (Tentativas >= MaxTentativas)
        {
            Status = StatusCodigoVerificacaoOtp.Expirado;
        }
    }

    /// <summary>Invalida este código porque um novo foi emitido para o mesmo usuário (invalidação em cascata).</summary>
    public void InvalidarPorNovoCodigo()
    {
        if (Status == StatusCodigoVerificacaoOtp.Pendente)
        {
            Status = StatusCodigoVerificacaoOtp.Expirado;
        }
    }
}
