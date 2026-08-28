using BlueprintOS.Core.Documentation.Models;

namespace BlueprintOS.Core.Documentation.Contracts;

/// <summary>
/// Define o contrato do gerador de documentação funcional (casos de uso), a partir de entrada estruturada.
/// </summary>
public interface IFunctionalDocumentationGenerator
{
    /// <summary>
    /// Gera o conteúdo Markdown da documentação funcional de um módulo.
    /// </summary>
    string Generate(FunctionalDocumentationInput input);
}
