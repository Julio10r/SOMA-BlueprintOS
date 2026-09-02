namespace BlueprintOS.Infrastructure.Integrations.Ibge;

public sealed class IbgeMunicipioOptions
{
    public const string SectionName = "IbgeMunicipio";

    public string BaseUrl { get; init; } = "https://servicodados.ibge.gov.br/api/v1/localidades/estados/";
    public int TimeoutSeconds { get; init; } = 10;
}
