namespace BlueprintOS.Infrastructure.Integrations.OpenAI;

/// <summary>
/// Configuração necessária para autenticar e direcionar chamadas à API da OpenAI.
/// </summary>
public sealed class OpenAIOptions
{
    /// <summary>
    /// Seção do appsettings onde esta configuração é lida.
    /// </summary>
    public const string SectionName = "AI:OpenAI";

    /// <summary>
    /// Chave de API utilizada para autenticação junto à OpenAI.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// URL base da API da OpenAI.
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.openai.com/v1/";
}
