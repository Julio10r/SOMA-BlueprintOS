namespace BlueprintOS.Core.Publication.Models.Assets;

/// <summary>
/// Um arquivo anexado ao documento (ex.: planilha, PDF complementar). Publicado como um arquivo
/// irmão do documento (sob <c>dist/{categoria}/attachments/</c>) e referenciado por link — ao
/// contrário de imagens/logos/ícones, não é embutido inline no conteúdo.
/// </summary>
/// <param name="Id">Identificador estável do anexo.</param>
/// <param name="FileName">Nome do arquivo, incluindo extensão.</param>
/// <param name="Bytes">Conteúdo binário do arquivo.</param>
/// <param name="MediaType">Tipo MIME do conteúdo.</param>
/// <param name="Description">Descrição do anexo, exibida na lista de anexos do documento.</param>
public sealed record AttachmentAsset(
    string Id,
    string FileName,
    byte[] Bytes,
    string MediaType,
    string Description);
