namespace BlueprintOS.Domain.Identity;

/// <summary>Registro de configuração de integração de ERP por Unidade de Negócio (O1.11) — relação 1:1
/// (uma Unidade de Negócio tem no máximo uma <see cref="ConfiguracaoErp"/>). Esta entidade é PURAMENTE
/// registro de configuração: esta Work Order não implementa nenhuma operação real de leitura/escrita no
/// ERP a partir dela — isso é escopo de outras Work Orders (ex. os leitores de Filial/Centro de Custo já
/// existentes em `Infrastructure/Integrations/ERP`), que continuam a fonte real de integração.
///
/// <see cref="ParametrosConexaoProtegidos"/> é sempre o texto já cifrado por <c>IDataProtector</c> — nunca
/// em claro no banco, no log ou na resposta da API; a API expõe somente
/// <c>parametrosConfigurados: bool</c>.</summary>
public sealed class ConfiguracaoErp
{
    public Guid Id { get; private set; }
    public Guid UnidadeNegocioId { get; private set; }
    public string SistemaErp { get; private set; }
    public string? ParametrosConexaoProtegidos { get; private set; }
    public StatusConfiguracaoTecnica Status { get; private set; }
    public DateTimeOffset CriadoEm { get; private set; }
    public DateTimeOffset AtualizadoEm { get; private set; }

    private ConfiguracaoErp() { SistemaErp = string.Empty; }

    public ConfiguracaoErp(Guid unidadeNegocioId, string sistemaErp, string? parametrosConexaoProtegidos, DateTimeOffset agora)
    {
        if (string.IsNullOrWhiteSpace(sistemaErp)) throw new ArgumentException("Sistema ERP é obrigatório.", nameof(sistemaErp));

        Id = Guid.NewGuid();
        UnidadeNegocioId = unidadeNegocioId;
        SistemaErp = sistemaErp.Trim();
        ParametrosConexaoProtegidos = parametrosConexaoProtegidos;
        Status = StatusConfiguracaoTecnica.Ativo;
        CriadoEm = agora;
        AtualizadoEm = agora;
    }

    public bool ParametrosConfigurados => !string.IsNullOrEmpty(ParametrosConexaoProtegidos);

    public bool EstaAtivo() => Status == StatusConfiguracaoTecnica.Ativo;

    /// <summary><paramref name="parametrosConexaoProtegidos"/> nulo preserva o segredo já salvo.</summary>
    public void Editar(string sistemaErp, string? parametrosConexaoProtegidos, DateTimeOffset agora)
    {
        if (string.IsNullOrWhiteSpace(sistemaErp)) throw new ArgumentException("Sistema ERP é obrigatório.", nameof(sistemaErp));

        SistemaErp = sistemaErp.Trim();
        if (parametrosConexaoProtegidos is not null) ParametrosConexaoProtegidos = parametrosConexaoProtegidos;
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
}
