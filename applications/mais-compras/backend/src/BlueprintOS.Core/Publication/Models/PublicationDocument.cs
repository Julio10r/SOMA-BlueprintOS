namespace BlueprintOS.Core.Publication.Models;

/// <summary>
/// Representa um documento completo a ser publicado (relatório executivo, guia de cliente ou
/// guia de engenharia), independente do formato final de renderização. Todos os
/// <see cref="Contracts.IContentRenderer"/> (Markdown, HTML, PDF e futuros formatos) consomem
/// exatamente esta mesma estrutura — nenhum deriva de outro.
/// </summary>
/// <param name="Slug">Nome base do arquivo de saída, sem extensão (ex.: <c>ExecutiveReport</c>).</param>
/// <param name="Category">Categoria de publicação (ex.: <c>executive</c>, <c>client</c>, <c>engineering</c>).</param>
/// <param name="Metadata">Metadados descritivos do documento (título, versão, autor, classificação etc.).</param>
/// <param name="Sections">Seções do corpo principal do documento, na ordem em que devem ser exibidas.</param>
/// <param name="Assets">Ativos visuais do documento (imagens, logos, ícones, gráficos, diagramas, anexos, QR Codes, selos).</param>
/// <param name="Appendix">Seções de apêndice, exibidas após o corpo principal (ex.: glossário, lista de acrônimos, histórico de versões) — mesma estrutura de <see cref="Sections"/>.</param>
/// <param name="Theme">Identidade visual (paleta de cores, cabeçalho/rodapé customizados) do documento.</param>
public sealed record PublicationDocument(
    string Slug,
    string Category,
    PublicationMetadata Metadata,
    IReadOnlyList<PublicationSection> Sections,
    PublicationAssets Assets,
    IReadOnlyList<PublicationSection> Appendix,
    PublicationTheme Theme);
