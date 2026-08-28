namespace BlueprintOS.Core.AI.Models;

/// <summary>
/// Descreve uma ferramenta que pode ser oferecida a um modelo de IA para execução.
/// </summary>
/// <param name="Name">Nome único da ferramenta.</param>
/// <param name="Description">Descrição do propósito da ferramenta, usada pelo modelo para decidir quando invocá-la.</param>
/// <param name="ParametersSchema">Esquema JSON (JSON Schema) que descreve os parâmetros aceitos pela ferramenta.</param>
public sealed record ToolDefinition(string Name, string Description, string ParametersSchema);
