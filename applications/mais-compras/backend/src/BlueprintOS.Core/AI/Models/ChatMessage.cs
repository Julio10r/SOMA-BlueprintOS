namespace BlueprintOS.Core.AI.Models;

/// <summary>
/// Representa uma mensagem trocada em uma conversa com um modelo de IA.
/// </summary>
/// <param name="Role">Papel do autor da mensagem.</param>
/// <param name="Content">Conteúdo textual da mensagem.</param>
/// <param name="ToolCalls">Chamadas de ferramentas solicitadas pelo modelo, quando aplicável.</param>
/// <param name="ToolCallId">Identificador da chamada de ferramenta associada, quando a mensagem representa um resultado de ferramenta.</param>
/// <param name="Name">Nome opcional do autor da mensagem.</param>
public sealed record ChatMessage(
    ChatRole Role,
    string Content,
    IReadOnlyList<ToolCall>? ToolCalls = null,
    string? ToolCallId = null,
    string? Name = null);
