namespace BlueprintOS.Infrastructure.Integrations.CnpjConsulta;

public sealed class CnpjConsultaOptions
{
    public const string SectionName = "CnpjConsulta";

    public string Provider { get; init; } = "BrasilApi";
    public string BaseUrl { get; init; } = "https://brasilapi.com.br/api/cnpj/v1/";
    public int TimeoutSeconds { get; init; } = 10;
}
