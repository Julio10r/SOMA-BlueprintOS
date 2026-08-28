namespace BlueprintOS.Domain.Knowledge.Linx;

/// <summary>Uma entrada versionada da base de conhecimento dos Agents Especialistas Linx (Work Order
/// O1.13.5). Nunca é sobrescrita silenciosamente: uma nova versão é sempre uma nova linha, encadeada por
/// <see cref="EntradaAnteriorId"/> e agrupada por <see cref="VersaoRaizId"/> — o histórico completo é
/// sempre recuperável (<c>ObterHistoricoAsync</c>).
///
/// <see cref="UnidadeNegocioId"/> nulo significa conhecimento GLOBAL do Visual Linx (conceito do ERP,
/// independente de BU); um valor presente restringe o conhecimento a uma Unidade de Negócio específica
/// (ex.: mapping/configuração do `SOMA_DESENV`) — nunca vaza implicitamente entre BUs (Work Order, seção
/// 19).
///
/// Conteúdo (<see cref="Conteudo"/>) é sempre tratado como DADO recuperado, nunca como instrução
/// privilegiada — quem constrói o prompt de um Agent (ver <c>Core.Agents</c>) nunca deve interpretar este
/// campo como comando de sistema (Work Order, seções 20/21).</summary>
public sealed class LinxKnowledgeEntry
{
    public Guid Id { get; private set; }
    public Guid VersaoRaizId { get; private set; }
    public Guid? EntradaAnteriorId { get; private set; }
    public int Versao { get; private set; }

    public LinxEspecialista Especialista { get; private set; }
    public LinxConhecimentoCategoria Categoria { get; private set; }
    public string Assunto { get; private set; }
    public string Conteudo { get; private set; }
    public LinxConhecimentoProveniencia Proveniencia { get; private set; }
    public string Fonte { get; private set; }
    public string Ator { get; private set; }
    public Guid? UnidadeNegocioId { get; private set; }
    public IReadOnlyList<string> Tags { get; private set; }

    public DateTimeOffset CriadoEm { get; private set; }
    public DateTimeOffset AtualizadoEm { get; private set; }

    private LinxKnowledgeEntry()
    {
        Assunto = string.Empty;
        Conteudo = string.Empty;
        Fonte = string.Empty;
        Ator = string.Empty;
        Tags = Array.Empty<string>();
    }

    /// <summary>Cria a primeira versão (Versão 1) de uma nova entrada de conhecimento. A proveniência
    /// inicial nunca pode ser <see cref="LinxConhecimentoProveniencia.Aprovado"/> — aprovação é sempre uma
    /// promoção explícita e autorizada (RBAC dedicado), nunca um estado de nascimento.</summary>
    public static LinxKnowledgeEntry Criar(
        LinxEspecialista especialista,
        LinxConhecimentoCategoria categoria,
        string assunto,
        string conteudo,
        LinxConhecimentoProveniencia proveniencia,
        string fonte,
        string ator,
        Guid? unidadeNegocioId,
        IReadOnlyList<string>? tags,
        DateTimeOffset agora)
    {
        if (string.IsNullOrWhiteSpace(assunto)) throw new ArgumentException("Assunto é obrigatório.", nameof(assunto));
        if (string.IsNullOrWhiteSpace(conteudo)) throw new ArgumentException("Conteúdo é obrigatório.", nameof(conteudo));
        if (string.IsNullOrWhiteSpace(fonte)) throw new ArgumentException("Fonte é obrigatória — conhecimento sem origem rastreável não é aceito.", nameof(fonte));
        if (string.IsNullOrWhiteSpace(ator)) throw new ArgumentException("Ator é obrigatório — toda entrada precisa de autoria rastreável.", nameof(ator));
        if (proveniencia == LinxConhecimentoProveniencia.Aprovado)
            throw new InvalidOperationException("Uma entrada nunca nasce Aprovada — aprovação é sempre uma promoção explícita e autorizada.");

        var id = Guid.NewGuid();
        return new LinxKnowledgeEntry
        {
            Id = id,
            VersaoRaizId = id,
            EntradaAnteriorId = null,
            Versao = 1,
            Especialista = especialista,
            Categoria = categoria,
            Assunto = assunto.Trim(),
            Conteudo = conteudo.Trim(),
            Proveniencia = proveniencia,
            Fonte = fonte.Trim(),
            Ator = ator.Trim(),
            UnidadeNegocioId = unidadeNegocioId,
            Tags = (tags ?? Array.Empty<string>()).Select(t => t.Trim()).Where(t => t.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            CriadoEm = agora,
            AtualizadoEm = agora,
        };
    }

    /// <summary>Cria uma NOVA VERSÃO (nova linha, nunca uma edição in-place) a partir da versão atual —
    /// refinamento/aprendizado incremental (Work Order, seção 12). A nova versão nasce sempre como
    /// <see cref="LinxConhecimentoProveniencia.Descoberto"/> ou <see cref="LinxConhecimentoProveniencia.Inferido"/>:
    /// mesmo que a versão anterior já estivesse Validada/Aprovada, o novo conteúdo precisa ser revalidado —
    /// nunca herda automaticamente o status de confiança da versão anterior.</summary>
    public LinxKnowledgeEntry NovaVersao(
        string novoConteudo,
        LinxConhecimentoProveniencia proveniencia,
        string fonte,
        string ator,
        IReadOnlyList<string>? tags,
        DateTimeOffset agora)
    {
        if (proveniencia is not (LinxConhecimentoProveniencia.Descoberto or LinxConhecimentoProveniencia.Inferido))
            throw new InvalidOperationException("Uma nova versão só pode nascer Descoberta ou Inferida — nunca herda Validado/Aprovado da versão anterior sem revalidação.");
        if (string.IsNullOrWhiteSpace(novoConteudo)) throw new ArgumentException("Conteúdo é obrigatório.", nameof(novoConteudo));
        if (string.IsNullOrWhiteSpace(fonte)) throw new ArgumentException("Fonte é obrigatória.", nameof(fonte));
        if (string.IsNullOrWhiteSpace(ator)) throw new ArgumentException("Ator é obrigatório.", nameof(ator));

        return new LinxKnowledgeEntry
        {
            Id = Guid.NewGuid(),
            VersaoRaizId = VersaoRaizId,
            EntradaAnteriorId = Id,
            Versao = Versao + 1,
            Especialista = Especialista,
            Categoria = Categoria,
            Assunto = Assunto,
            Conteudo = novoConteudo.Trim(),
            Proveniencia = proveniencia,
            Fonte = fonte.Trim(),
            Ator = ator.Trim(),
            UnidadeNegocioId = UnidadeNegocioId,
            Tags = (tags ?? Tags).Select(t => t.Trim()).Where(t => t.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            CriadoEm = agora,
            AtualizadoEm = agora,
        };
    }

    /// <summary>Promove a proveniência da entrada, respeitando as únicas transições válidas (Work Order,
    /// seção 11): Descoberto/Inferido → Validado; Validado → Aprovado. Nunca permite pular etapa
    /// (Descoberto/Inferido → Aprovado direto), nunca permite rebaixar, e <see cref="LinxConhecimentoProveniencia.Aprovado"/>
    /// é terminal. A checagem de QUEM pode promover para Aprovado é responsabilidade do RBAC na camada de
    /// Api/Application (permissão dedicada) — este método garante apenas a máquina de estados.</summary>
    public void Promover(LinxConhecimentoProveniencia novaProveniencia, string ator, DateTimeOffset agora)
    {
        if (string.IsNullOrWhiteSpace(ator)) throw new ArgumentException("Ator é obrigatório.", nameof(ator));

        var transicaoValida = (Proveniencia, novaProveniencia) switch
        {
            (LinxConhecimentoProveniencia.Descoberto, LinxConhecimentoProveniencia.Validado) => true,
            (LinxConhecimentoProveniencia.Inferido, LinxConhecimentoProveniencia.Validado) => true,
            (LinxConhecimentoProveniencia.Validado, LinxConhecimentoProveniencia.Aprovado) => true,
            _ => false,
        };

        if (!transicaoValida)
        {
            throw new InvalidOperationException(
                $"Transição de proveniência inválida: '{Proveniencia}' → '{novaProveniencia}'. Nenhuma promoção pula etapa, rebaixa ou reabre uma entrada Aprovada.");
        }

        Proveniencia = novaProveniencia;
        Ator = ator.Trim();
        AtualizadoEm = agora;
    }

    public bool EhGlobal() => UnidadeNegocioId is null;
}
