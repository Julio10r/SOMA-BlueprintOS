namespace BlueprintOS.Core.Documentation.Models;

/// <summary>
/// Entrada estruturada para geração de documentação voltada a desenvolvedores,
/// no estilo de um README/guia técnico.
/// </summary>
/// <param name="Title">Título do guia.</param>
/// <param name="Overview">Visão geral do módulo ou componente documentado.</param>
/// <param name="SetupSteps">Passos necessários para configurar o ambiente/módulo.</param>
/// <param name="UsageExamples">Exemplos de uso do módulo ou componente.</param>
/// <param name="Troubleshooting">Dicas para solução de problemas comuns.</param>
public sealed record DeveloperDocumentationInput(
    string Title,
    string Overview,
    IReadOnlyList<string> SetupSteps,
    IReadOnlyList<string> UsageExamples,
    IReadOnlyList<string> Troubleshooting);
