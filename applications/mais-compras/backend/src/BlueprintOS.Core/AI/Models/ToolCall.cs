namespace BlueprintOS.Core.AI.Models;

/// <summary>
/// Representa uma solicitação de execução de ferramenta feita por um modelo de IA.
/// </summary>
/// <param name="Id">Identificador único da chamada, utilizado para correlacionar o resultado.</param>
/// <param name="ToolName">Nome da ferramenta solicitada.</param>
/// <param name="Arguments">Argumentos da chamada, serializados em JSON.</param>
public sealed record ToolCall(string Id, string ToolName, string Arguments);
