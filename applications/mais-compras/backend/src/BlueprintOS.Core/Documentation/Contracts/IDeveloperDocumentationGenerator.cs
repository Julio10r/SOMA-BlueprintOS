using BlueprintOS.Core.Documentation.Models;

namespace BlueprintOS.Core.Documentation.Contracts;

/// <summary>
/// Define o contrato do gerador de documentação voltada a desenvolvedores (estilo README).
/// </summary>
public interface IDeveloperDocumentationGenerator
{
    /// <summary>
    /// Gera o conteúdo Markdown do guia para desenvolvedores.
    /// </summary>
    string Generate(DeveloperDocumentationInput input);
}
