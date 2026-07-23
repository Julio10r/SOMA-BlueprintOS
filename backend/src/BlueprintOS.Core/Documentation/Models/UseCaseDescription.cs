namespace BlueprintOS.Core.Documentation.Models;

/// <summary>
/// Descreve um caso de uso funcional para geração de documentação funcional.
/// </summary>
/// <param name="Name">Nome do caso de uso.</param>
/// <param name="Description">Descrição resumida do caso de uso.</param>
/// <param name="Actors">Atores envolvidos no caso de uso.</param>
/// <param name="Steps">Passos, em ordem, que compõem o fluxo do caso de uso.</param>
/// <param name="ExpectedResult">Resultado esperado ao final do caso de uso.</param>
public sealed record UseCaseDescription(
    string Name,
    string Description,
    IReadOnlyList<string> Actors,
    IReadOnlyList<string> Steps,
    string ExpectedResult);

/// <summary>
/// Entrada estruturada para geração de documentação funcional de um módulo.
/// </summary>
/// <param name="ModuleName">Nome do módulo documentado.</param>
/// <param name="UseCases">Casos de uso pertencentes ao módulo.</param>
public sealed record FunctionalDocumentationInput(
    string ModuleName,
    IReadOnlyList<UseCaseDescription> UseCases);
