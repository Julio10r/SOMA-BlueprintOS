namespace BlueprintOS.Domain.Identity;

/// <summary>Registro de configuração de notificações por Unidade de Negócio (O1.11, item #24) — relação
/// 1:1 (uma Unidade de Negócio tem no máximo uma <see cref="ConfiguracaoNotificacao"/>), no mesmo padrão de
/// <see cref="ConfiguracaoErp"/>. ESCOPO MÍNIMO DE FUNDAÇÃO aprovado pelo Product Owner: esta entidade é
/// PURAMENTE configuração administrativa — nenhum envio real de e-mail, SMTP, fila, worker, scheduler,
/// retry, template ou histórico de notificação é implementado nesta Work Order. Isso é escopo de Work
/// Orders futuras, quando os workflows operacionais correspondentes existirem.
///
/// Não há, nesta sprint, catálogo formal aprovado de eventos configuráveis (verificado em docs/product/,
/// .ai/work-orders/ e ADRs) — por isso nenhuma lista/tabela de eventos é criada agora. O desenho evita
/// fechar o esquema de forma destrutiva: uma futura tabela `ConfiguracaoNotificacaoEventos` (N:N com um
/// catálogo de eventos) poderia ser adicionada depois referenciando <see cref="Id"/> sem exigir alterar
/// esta entidade.</summary>
public sealed class ConfiguracaoNotificacao
{
    public Guid Id { get; private set; }
    public Guid UnidadeNegocioId { get; private set; }
    public bool EmailAtivado { get; private set; }
    public string? EmailRemetente { get; private set; }
    public string? NomeRemetente { get; private set; }
    public DateTimeOffset CriadoEm { get; private set; }
    public DateTimeOffset AtualizadoEm { get; private set; }

    private ConfiguracaoNotificacao() { }

    public ConfiguracaoNotificacao(Guid unidadeNegocioId, bool emailAtivado, string? emailRemetente, string? nomeRemetente, DateTimeOffset agora)
    {
        Id = Guid.NewGuid();
        UnidadeNegocioId = unidadeNegocioId;
        CriadoEm = agora;
        AtualizadoEm = agora;
        AplicarValores(emailAtivado, emailRemetente, nomeRemetente);
    }

    /// <summary>Idempotente (mesmo padrão de <see cref="ConfiguracaoErp.Editar"/>) — não há endpoint de
    /// criação separado do de edição.</summary>
    public void Editar(bool emailAtivado, string? emailRemetente, string? nomeRemetente, DateTimeOffset agora)
    {
        AplicarValores(emailAtivado, emailRemetente, nomeRemetente);
        AtualizadoEm = agora;
    }

    private void AplicarValores(bool emailAtivado, string? emailRemetente, string? nomeRemetente)
    {
        // Ativar notificações por e-mail exige remetente configurado — regra de integridade mínima
        // (não é possível "ativar" uma configuração vazia).
        if (emailAtivado && string.IsNullOrWhiteSpace(emailRemetente))
        {
            throw new ArgumentException("E-mail remetente é obrigatório para ativar notificações por e-mail.", nameof(emailRemetente));
        }

        EmailAtivado = emailAtivado;
        EmailRemetente = string.IsNullOrWhiteSpace(emailRemetente) ? null : emailRemetente.Trim().ToLowerInvariant();
        NomeRemetente = string.IsNullOrWhiteSpace(nomeRemetente) ? null : nomeRemetente.Trim();
    }
}
