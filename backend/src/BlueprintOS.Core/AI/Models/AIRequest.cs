namespace BlueprintOS.Core.AI.Models;

/// <summary>
/// Representa uma requisição de execução enviada a um modelo de IA.
/// </summary>
/// <param name="Model">Modelo de IA a ser utilizado na execução.</param>
/// <param name="Messages">Histórico de mensagens da conversa enviado ao modelo.</param>
/// <param name="Options">Parâmetros opcionais de geração.</param>
/// <param name="Tools">Ferramentas disponibilizadas ao modelo para execução, quando aplicável.</param>
public sealed record AIRequest(
    AIModel Model,
    IReadOnlyList<ChatMessage> Messages,
    AIOptions? Options = null,
    IReadOnlyList<ToolDefinition>? Tools = null)
{
    private static readonly AIModel DefaultModel = new("gpt-4o-mini", "openai");

    /// <summary>
    /// Cria uma requisição simples a partir de um prompt de usuário, utilizando o modelo padrão do runtime.
    /// </summary>
    /// <param name="prompt">Texto enviado como mensagem de usuário.</param>
    public AIRequest(string prompt)
        : this(DefaultModel, new[] { new ChatMessage(ChatRole.User, prompt) })
    {
    }
}
