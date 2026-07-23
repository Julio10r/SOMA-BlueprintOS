namespace BlueprintOS.Core.AI.Models;

/// <summary>
/// Representa o resultado da execução de uma ferramenta solicitada por um modelo de IA.
/// </summary>
/// <param name="ToolCallId">Identificador da chamada de ferramenta original.</param>
/// <param name="Content">Conteúdo retornado pela execução da ferramenta.</param>
/// <param name="IsError">Indica se a execução da ferramenta resultou em erro.</param>
public sealed record ToolResult(string ToolCallId, string Content, bool IsError = false);
