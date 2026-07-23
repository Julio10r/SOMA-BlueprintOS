using BlueprintOS.Core.Documentation.Models;

namespace BlueprintOS.Core.Documentation.Contracts;

/// <summary>
/// Define o contrato do gerador de documentação técnica de módulos, a partir de metadados estruturados.
/// </summary>
public interface ITechnicalDocumentationGenerator
{
    /// <summary>
    /// Gera o conteúdo Markdown da documentação técnica de um módulo.
    /// </summary>
    string Generate(ModuleMetadata metadata);
}
