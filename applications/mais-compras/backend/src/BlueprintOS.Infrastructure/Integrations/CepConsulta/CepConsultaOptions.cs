namespace BlueprintOS.Infrastructure.Integrations.CepConsulta;

public sealed class CepConsultaOptions
{
    public const string SectionName = "CepConsulta";

    public string Provider { get; init; } = "ViaCep";
    public string BaseUrl { get; init; } = "https://viacep.com.br/ws/";
    public int TimeoutSeconds { get; init; } = 10;
}
