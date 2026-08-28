using BlueprintOS.Core.Documentation.Models;

namespace BlueprintOS.Core.Documentation.Contracts;

/// <summary>
/// Define o contrato do gerador de documentação destinada a agentes de IA (formato <c>.ai/context</c>).
/// </summary>
public interface IAiDocumentationGenerator
{
    /// <summary>
    /// Gera o conteúdo Markdown da documentação de IA para o tópico informado.
    /// </summary>
    string Generate(AiDocumentationInput input);
}
