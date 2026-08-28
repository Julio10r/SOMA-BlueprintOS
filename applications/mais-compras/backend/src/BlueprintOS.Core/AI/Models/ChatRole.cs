namespace BlueprintOS.Core.AI.Models;

/// <summary>
/// Papel do autor de uma mensagem em uma conversa com um modelo de IA.
/// </summary>
public enum ChatRole
{
    /// <summary>Instrução de sistema que define o comportamento do modelo.</summary>
    System,

    /// <summary>Mensagem enviada pelo usuário.</summary>
    User,

    /// <summary>Mensagem gerada pelo modelo de IA.</summary>
    Assistant,

    /// <summary>Resultado retornado pela execução de uma ferramenta.</summary>
    Tool
}
