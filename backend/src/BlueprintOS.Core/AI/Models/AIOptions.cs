namespace BlueprintOS.Core.AI.Models;

/// <summary>
/// Parâmetros opcionais que controlam o comportamento de geração de um modelo de IA.
/// </summary>
/// <param name="Temperature">Grau de aleatoriedade da resposta, tipicamente entre 0 e 2.</param>
/// <param name="MaxOutputTokens">Limite máximo de tokens a serem gerados na resposta.</param>
/// <param name="TopP">Parâmetro de amostragem por núcleo (nucleus sampling).</param>
/// <param name="StopSequences">Sequências de texto que, se geradas, interrompem a resposta.</param>
public sealed record AIOptions(
    double? Temperature = null,
    int? MaxOutputTokens = null,
    double? TopP = null,
    IReadOnlyList<string>? StopSequences = null);
