using System.Diagnostics;
using System.Net.Http.Json;
using BlueprintOS.Core.AI.Contracts;
using BlueprintOS.Core.AI.Models;

namespace BlueprintOS.Infrastructure.Integrations.OpenAI;

/// <summary>
/// Implementação de <see cref="IAIProvider"/> que integra com a API de Chat Completions da OpenAI.
/// </summary>
public sealed class OpenAIProvider : IAIProvider
{
    private readonly HttpClient _httpClient;

    public OpenAIProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public string Name => "openai";

    public async Task<AIResponse> CompleteAsync(AIRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        var payload = new
        {
            model = request.Model.Id,
            messages = request.Messages.Select(ToOpenAIMessage),
            temperature = request.Options?.Temperature,
            max_tokens = request.Options?.MaxOutputTokens,
            top_p = request.Options?.TopP,
            stop = request.Options?.StopSequences
        };

        using var httpResponse = await _httpClient
            .PostAsJsonAsync("chat/completions", payload, cancellationToken)
            .ConfigureAwait(false);

        httpResponse.EnsureSuccessStatusCode();

        var body = await httpResponse.Content
            .ReadFromJsonAsync<OpenAIChatCompletionResponse>(cancellationToken)
            .ConfigureAwait(false);

        stopwatch.Stop();

        if (body is null || body.Choices.Count == 0)
        {
            throw new InvalidOperationException("A API da OpenAI retornou uma resposta sem escolhas (choices).");
        }

        var choice = body.Choices[0];
        var message = new ChatMessage(ChatRole.Assistant, choice.Message.Content ?? string.Empty);
        var usage = new TokenUsage(body.Usage?.PromptTokens ?? 0, body.Usage?.CompletionTokens ?? 0);
        var metrics = new AIExecutionMetrics(Name, request.Model.Id, stopwatch.Elapsed, usage);

        return new AIResponse(message, usage, metrics, choice.FinishReason);
    }

    private static object ToOpenAIMessage(ChatMessage message) => new
    {
        role = message.Role.ToString().ToLowerInvariant(),
        content = message.Content
    };
}
