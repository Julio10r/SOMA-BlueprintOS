namespace BlueprintOS.Domain.Identity;

/// <summary>Vínculo N:N entre <see cref="FeatureFlag"/> e <see cref="UnidadeNegocio"/> (O1.11,
/// conforme `ComprasDataModel.md`) — carrega o estado ativo/inativo da flag para aquela Unidade.
/// Único por (<see cref="FeatureFlagId"/>, <see cref="UnidadeNegocioId"/>).</summary>
public sealed class FeatureFlagUnidadeNegocio
{
    public Guid Id { get; private set; }
    public Guid FeatureFlagId { get; private set; }
    public Guid UnidadeNegocioId { get; private set; }
    public bool Ativa { get; private set; }
    public DateTimeOffset AtualizadoEm { get; private set; }

    private FeatureFlagUnidadeNegocio() { }

    public FeatureFlagUnidadeNegocio(Guid featureFlagId, Guid unidadeNegocioId, bool ativa, DateTimeOffset agora)
    {
        Id = Guid.NewGuid();
        FeatureFlagId = featureFlagId;
        UnidadeNegocioId = unidadeNegocioId;
        Ativa = ativa;
        AtualizadoEm = agora;
    }

    public void DefinirAtiva(bool ativa, DateTimeOffset agora)
    {
        Ativa = ativa;
        AtualizadoEm = agora;
    }
}
