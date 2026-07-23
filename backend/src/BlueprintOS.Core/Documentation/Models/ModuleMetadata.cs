namespace BlueprintOS.Core.Documentation.Models;

/// <summary>
/// Metadados de um módulo utilizados para gerar documentação técnica.
/// </summary>
/// <param name="ModuleName">Nome do módulo (ex.: <c>Knowledge</c>).</param>
/// <param name="Description">Descrição resumida da responsabilidade do módulo.</param>
/// <param name="Contracts">Lista de nomes de contratos (interfaces) públicos do módulo.</param>
/// <param name="Classes">Lista de nomes de classes públicas relevantes do módulo.</param>
public sealed record ModuleMetadata(
    string ModuleName,
    string Description,
    IReadOnlyList<string> Contracts,
    IReadOnlyList<string> Classes);
