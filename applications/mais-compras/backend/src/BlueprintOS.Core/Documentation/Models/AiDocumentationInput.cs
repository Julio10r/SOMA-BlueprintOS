namespace BlueprintOS.Core.Documentation.Models;

/// <summary>
/// Entrada estruturada para geração de documentação destinada a agentes de IA,
/// no formato utilizado em <c>.ai/context/*.md</c>.
/// </summary>
/// <param name="Topic">Tópico ou nome do documento (ex.: <c>knowledge</c>).</param>
/// <param name="Scope">Frase de escopo, exibida como citação logo após o título.</param>
/// <param name="Purpose">Propósito do documento.</param>
/// <param name="KeyConcepts">Conceitos-chave que o documento deve descrever.</param>
/// <param name="Guidelines">Diretrizes que agentes de IA devem seguir.</param>
/// <param name="References">Referências a outros documentos relacionados.</param>
public sealed record AiDocumentationInput(
    string Topic,
    string Scope,
    string Purpose,
    IReadOnlyList<string> KeyConcepts,
    IReadOnlyList<string> Guidelines,
    IReadOnlyList<string> References);
