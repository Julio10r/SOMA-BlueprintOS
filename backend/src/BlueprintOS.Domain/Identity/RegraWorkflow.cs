namespace BlueprintOS.Domain.Identity;

/// <summary>O1.12 — Fundação de Administração de Workflow (ADR-0020, revisão R1.1). Registra a
/// CONFIGURAÇÃO de uma regra de workflow por Unidade de Negócio: nome, tipo de processo ao qual se
/// aplica e ordem/sequência relativa entre regras do mesmo tipo. ESCOPO MÍNIMO DE FUNDAÇÃO: o desenho
/// definitivo de etapas/condições do motor de workflow é dúvida de produto pendente (ver
/// `docs/product/ComprasFuncional.md` e `ComprasDataModel.md`, seção "Administração"). Nenhum motor de
/// execução, estado operacional ou orquestração de processo é implementado aqui — apenas o cadastro.
///
/// Não existe exclusão física: mesmo padrão de Perfis/Usuários/Unidades de Alocação — apenas
/// Ativo/Inativo.</summary>
public sealed class RegraWorkflow
{
    public Guid Id { get; private set; }
    public string Nome { get; private set; }
    public Guid UnidadeNegocioId { get; private set; }

    /// <summary>Tipo/processo ao qual a regra se aplica. Campo texto simples deliberado: não há, nesta
    /// Onda, catálogo formal aprovado de tipos de processo de compra (PENDÊNCIA de produto registrada em
    /// `ComprasDataModel.md`).</summary>
    public string TipoProcesso { get; private set; }

    /// <summary>Ordem/sequência de aplicação da regra entre as demais do mesmo <see cref="TipoProcesso"/>
    /// e Unidade de Negócio. Não há garantia de unicidade — a ordenação definitiva é responsabilidade do
    /// futuro motor de workflow (Onda 3).</summary>
    public int Ordem { get; private set; }

    public bool Ativo { get; private set; }
    public DateTimeOffset CriadoEm { get; private set; }
    public DateTimeOffset AtualizadoEm { get; private set; }

    private RegraWorkflow() { Nome = string.Empty; TipoProcesso = string.Empty; }

    public RegraWorkflow(string nome, Guid unidadeNegocioId, string tipoProcesso, int ordem, DateTimeOffset agora)
    {
        Id = Guid.NewGuid();
        UnidadeNegocioId = unidadeNegocioId;
        CriadoEm = agora;
        AtualizadoEm = agora;
        Ativo = true;
        AplicarValores(nome, tipoProcesso, ordem);
    }

    public void Editar(string nome, string tipoProcesso, int ordem, DateTimeOffset agora)
    {
        AplicarValores(nome, tipoProcesso, ordem);
        AtualizadoEm = agora;
    }

    public void Ativar(DateTimeOffset agora)
    {
        if (Ativo) return;
        Ativo = true;
        AtualizadoEm = agora;
    }

    public void Inativar(DateTimeOffset agora)
    {
        if (!Ativo) return;
        Ativo = false;
        AtualizadoEm = agora;
    }

    private void AplicarValores(string nome, string tipoProcesso, int ordem)
    {
        if (string.IsNullOrWhiteSpace(nome)) throw new ArgumentException("Nome é obrigatório.", nameof(nome));
        if (string.IsNullOrWhiteSpace(tipoProcesso)) throw new ArgumentException("Tipo de processo é obrigatório.", nameof(tipoProcesso));
        if (ordem < 0) throw new ArgumentException("Ordem não pode ser negativa.", nameof(ordem));

        Nome = nome.Trim();
        TipoProcesso = tipoProcesso.Trim();
        Ordem = ordem;
    }
}
