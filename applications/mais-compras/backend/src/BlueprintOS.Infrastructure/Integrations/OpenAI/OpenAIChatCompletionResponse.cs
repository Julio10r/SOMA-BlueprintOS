using System.Text.Json.Serialization;

namespace BlueprintOS.Infrastructure.Integrations.OpenAI;

internal sealed class OpenAIChatCompletionResponse
{
    [JsonPropertyName("choices")]
    public List<OpenAIChoice> Choices { get; set; } = new();

    [JsonPropertyName("usage")]
    public OpenAIUsage? Usage { get; set; }
}

internal sealed class OpenAIChoice
{
    [JsonPropertyName("message")]
    public OpenAIMessage Message { get; set; } = new();

    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }
}

internal sealed class OpenAIMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string? Content { get; set; }
}

internal sealed class OpenAIUsage
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }

    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }
}
