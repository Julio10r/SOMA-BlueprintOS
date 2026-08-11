namespace BlueprintOS.Domain.Identity;

/// <summary>Unidade de Alocação (O1.8 — Persistência Real). Conceito exclusivo do +Compras: nunca
/// integrada do ERP, ao contrário de Filial e Centro de Custo (ADR-0020, item 4). O relacionamento N:N com
/// Centro de Custo é escopo da O1.9 — não implementado aqui.
///
/// Não existe exclusão física: mesmo padrão de Perfis/Usuários/Filiais/Centros de Custo — apenas
/// Ativo/Inativo.</summary>
public sealed class UnidadeAlocacao
{
    public Guid Id { get; private set; }
    public string Nome { get; private set; }
    public string Descricao { get; private set; }
    public Guid UnidadeNegocioId { get; private set; }
    public StatusUnidadeAlocacao Status { get; private set; }
    public DateTimeOffset CriadoEm { get; private set; }
    public DateTimeOffset AtualizadoEm { get; private set; }

    private UnidadeAlocacao() { Nome = string.Empty; Descricao = string.Empty; }

    public UnidadeAlocacao(string nome, string descricao, Guid unidadeNegocioId, DateTimeOffset? agora = null)
    {
        if (string.IsNullOrWhiteSpace(nome)) throw new ArgumentException("Nome é obrigatório.", nameof(nome));

        var momento = agora ?? DateTimeOffset.UtcNow;

        Id = Guid.NewGuid();
        Nome = nome.Trim();
        Descricao = (descricao ?? string.Empty).Trim();
        UnidadeNegocioId = unidadeNegocioId;
        Status = StatusUnidadeAlocacao.Ativo;
        CriadoEm = momento;
        AtualizadoEm = momento;
    }

    public bool EstaAtiva() => Status == StatusUnidadeAlocacao.Ativo;

    public void Atualizar(string nome, string descricao, DateTimeOffset agora)
    {
        if (string.IsNullOrWhiteSpace(nome)) throw new ArgumentException("Nome é obrigatório.", nameof(nome));
        Nome = nome.Trim();
        Descricao = (descricao ?? string.Empty).Trim();
        AtualizadoEm = agora;
    }

    public void Ativar(DateTimeOffset agora)
    {
        if (Status == StatusUnidadeAlocacao.Ativo) return;
        Status = StatusUnidadeAlocacao.Ativo;
        AtualizadoEm = agora;
    }

    public void Inativar(DateTimeOffset agora)
    {
        if (Status == StatusUnidadeAlocacao.Inativo) return;
        Status = StatusUnidadeAlocacao.Inativo;
        AtualizadoEm = agora;
    }
}
