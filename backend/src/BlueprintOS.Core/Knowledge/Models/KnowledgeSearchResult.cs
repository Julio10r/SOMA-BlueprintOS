namespace BlueprintOS.Core.Knowledge.Models;

/// <summary>
/// Representa um trecho relevante de um <see cref="KnowledgeDocument"/> retornado por uma busca textual.
/// </summary>
/// <param name="Document">Documento ao qual o trecho pertence.</param>
/// <param name="Snippet">Trecho do conteúdo contendo o termo buscado.</param>
/// <param name="Score">Pontuação de relevância do resultado, baseada na quantidade de ocorrências.</param>
public sealed record KnowledgeSearchResult(KnowledgeDocument Document, string Snippet, int Score);
