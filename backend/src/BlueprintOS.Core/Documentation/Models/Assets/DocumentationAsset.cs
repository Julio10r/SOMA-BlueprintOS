namespace BlueprintOS.Core.Documentation.Models.Assets;

/// <summary>
/// Representa um ativo de documentação reutilizável (diagrama Mermaid ou Markdown), gerado
/// automaticamente pelo Asset Generator para ser consumido futuramente pelos Publishers.
/// </summary>
/// <param name="RelativePath">Caminho relativo do arquivo de destino (dentro da raiz de ativos).</param>
/// <param name="Content">Conteúdo textual do ativo (Mermaid ou Markdown), sem envelope de cabeçalho.</param>
public sealed record DocumentationAsset(string RelativePath, string Content);
