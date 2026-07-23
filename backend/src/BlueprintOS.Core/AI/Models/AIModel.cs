namespace BlueprintOS.Core.AI.Models;

/// <summary>
/// Identifica um modelo de IA de forma agnóstica ao provedor, sem qualquer acoplamento a SDKs externos.
/// </summary>
/// <param name="Id">Identificador do modelo reconhecido pelo provedor (ex.: "gpt-4o", "claude-sonnet-5").</param>
/// <param name="Provider">Nome do provedor responsável por executar o modelo (ex.: "openai", "anthropic").</param>
public sealed record AIModel(string Id, string Provider);
