namespace BlueprintOS.Domain.Identity;

/// <summary>Configuração de um provedor de identidade (ex.: Microsoft Entra ID, OTP por e-mail
/// corporativo) para uma Unidade de Negócio (O1.11). Puramente registro de configuração — não substitui
/// nem altera o mecanismo real de autenticação/sessão já implementado na O1.4.x
/// (<c>SessionCurrentIdentity</c>/claims/cookies), que esta Work Order não toca.
///
/// <see cref="ParametrosProtegidos"/> é sempre o texto já cifrado por <c>IDataProtector</c> — a entidade de
/// domínio nunca lida com o segredo em claro; cifrar/decifrar é responsabilidade da camada de
/// Infraestrutura que persiste/lê esta entidade. Nunca exposto pela API depois de salvo — apenas
/// <c>ParametrosConfigurados: bool</c> na projeção de leitura.
///
/// <see cref="DominiosAutorizadosCsv"/> guarda a lista de domínios como CSV simples (sem vírgulas nos
/// domínios individuais) — opção deliberada de "tabela filha simples vs. CSV" (permitida pela spec da
/// Work Order) para evitar uma tabela adicional só para uma lista pequena e de baixa cardinalidade.</summary>
public sealed class IdentityProvider
{
    public Guid Id { get; private set; }
    public Guid UnidadeNegocioId { get; private set; }
    public string Tipo { get; private set; }
    public string DominiosAutorizadosCsv { get; private set; }
    public string? ParametrosProtegidos { get; private set; }
    public StatusConfiguracaoTecnica Status { get; private set; }
    public DateTimeOffset CriadoEm { get; private set; }
    public DateTimeOffset AtualizadoEm { get; private set; }

    public IReadOnlyList<string> DominiosAutorizados =>
        DominiosAutorizadosCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private IdentityProvider() { Tipo = string.Empty; DominiosAutorizadosCsv = string.Empty; }

    public IdentityProvider(
        Guid unidadeNegocioId, string tipo, IEnumerable<string>? dominiosAutorizados,
        string? parametrosProtegidos, DateTimeOffset agora)
    {
        if (string.IsNullOrWhiteSpace(tipo)) throw new ArgumentException("Tipo do Identity Provider é obrigatório.", nameof(tipo));

        Id = Guid.NewGuid();
        UnidadeNegocioId = unidadeNegocioId;
        Tipo = tipo.Trim();
        DominiosAutorizadosCsv = ParaCsv(dominiosAutorizados);
        ParametrosProtegidos = parametrosProtegidos;
        Status = StatusConfiguracaoTecnica.Ativo;
        CriadoEm = agora;
        AtualizadoEm = agora;
    }

    public bool ParametrosConfigurados => !string.IsNullOrEmpty(ParametrosProtegidos);

    public bool EstaAtivo() => Status == StatusConfiguracaoTecnica.Ativo;

    /// <summary><paramref name="parametrosProtegidos"/> nulo preserva o segredo já salvo (edição sem
    /// reenvio do segredo) — só é substituído quando um novo valor cifrado é informado.</summary>
    public void Editar(string tipo, IEnumerable<string>? dominiosAutorizados, string? parametrosProtegidos, DateTimeOffset agora)
    {
        if (string.IsNullOrWhiteSpace(tipo)) throw new ArgumentException("Tipo do Identity Provider é obrigatório.", nameof(tipo));

        Tipo = tipo.Trim();
        DominiosAutorizadosCsv = ParaCsv(dominiosAutorizados);
        if (parametrosProtegidos is not null) ParametrosProtegidos = parametrosProtegidos;
        AtualizadoEm = agora;
    }

    public void Ativar(DateTimeOffset agora)
    {
        if (Status == StatusConfiguracaoTecnica.Ativo) return;
        Status = StatusConfiguracaoTecnica.Ativo;
        AtualizadoEm = agora;
    }

    public void Inativar(DateTimeOffset agora)
    {
        if (Status == StatusConfiguracaoTecnica.Inativo) return;
        Status = StatusConfiguracaoTecnica.Inativo;
        AtualizadoEm = agora;
    }

    private static string ParaCsv(IEnumerable<string>? dominios) => dominios is null
        ? string.Empty
        : string.Join(',', dominios
            .Where(d => !string.IsNullOrWhiteSpace(d))
            .Select(d => d.Trim().ToLowerInvariant())
            .Distinct());
}
