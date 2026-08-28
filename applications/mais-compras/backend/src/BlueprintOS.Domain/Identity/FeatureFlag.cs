namespace BlueprintOS.Domain.Identity;

/// <summary>Catálogo de Feature Flags (O1.11). Nasce vazio — nenhuma flag é semeada por migration; cada
/// flag é cadastrada explicitamente pela Administração. O estado ativo/inativo por Unidade de Negócio é
/// modelado pelo vínculo N:N <see cref="FeatureFlagUnidadeNegocio"/>.</summary>
public sealed class FeatureFlag
{
    public Guid Id { get; private set; }
    public string Nome { get; private set; }
    public string Descricao { get; private set; }
    public DateTimeOffset CriadoEm { get; private set; }

    private FeatureFlag() { Nome = string.Empty; Descricao = string.Empty; }

    public FeatureFlag(string nome, string descricao, DateTimeOffset agora)
    {
        if (string.IsNullOrWhiteSpace(nome)) throw new ArgumentException("Nome da Feature Flag é obrigatório.", nameof(nome));

        Id = Guid.NewGuid();
        Nome = nome.Trim();
        Descricao = (descricao ?? string.Empty).Trim();
        CriadoEm = agora;
    }
}
