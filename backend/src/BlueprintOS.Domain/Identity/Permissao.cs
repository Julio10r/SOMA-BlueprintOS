namespace BlueprintOS.Domain.Identity;

/// <summary>Catálogo global de permissões (ADR-0020, item 8; ratificado como catálogo global em
/// 07/08/2026), materializado no banco pelo seed determinístico de <see cref="PermissaoCatalogo"/>.
///
/// O1.5: o código passa a ser preservado na grafia canônica do catálogo (<c>Recurso.Acao</c>) em vez de
/// convertido para maiúsculas. Nenhum dado foi migrado por essa mudança porque nada no sistema criava
/// <see cref="Permissao"/> antes da O1.5 — a tabela `Permissoes` só passa a ter linhas com o seed desta
/// sprint. A comparação de códigos é sempre case-insensitive (ver <see cref="PermissaoCatalogo.Existe"/>),
/// então a caixa nunca é um fator de decisão de autorização.</summary>
public sealed class Permissao
{
    public Guid Id { get; private set; }
    public string Codigo { get; private set; }
    public string Descricao { get; private set; }

    private Permissao() { Codigo = string.Empty; Descricao = string.Empty; }

    public Permissao(string codigo, string descricao)
    {
        if (string.IsNullOrWhiteSpace(codigo)) throw new ArgumentException("Código da permissão é obrigatório.", nameof(codigo));

        Id = Guid.NewGuid();
        Codigo = codigo.Trim();
        Descricao = descricao ?? string.Empty;
    }

    /// <summary>Constrói a partir de uma definição do catálogo, preservando o <see cref="Guid"/> estável —
    /// usado exclusivamente pelo seed de banco.</summary>
    public static Permissao DoCatalogo(PermissaoDefinicao definicao) => new()
    {
        Id = definicao.Id,
        Codigo = definicao.Codigo,
        Descricao = definicao.Descricao,
    };
}
