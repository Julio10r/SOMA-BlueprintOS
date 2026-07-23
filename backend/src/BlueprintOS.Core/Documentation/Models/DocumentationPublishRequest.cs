namespace BlueprintOS.Core.Documentation.Models;

/// <summary>
/// Representa uma solicitação de publicação de um documento: o caminho relativo de destino
/// (dentro da raiz de documentação), o título do documento e o corpo Markdown já gerado
/// (sem o envelope de cabeçalho, que é adicionado pelo publicador).
/// </summary>
/// <param name="RelativePath">Caminho relativo do arquivo de destino (ex.: <c>executive/dashboard.md</c>).</param>
/// <param name="Title">Título do documento.</param>
/// <param name="Body">Corpo Markdown do documento, sem cabeçalho de envelope.</param>
public sealed record DocumentationPublishRequest(
    string RelativePath,
    string Title,
    string Body);
