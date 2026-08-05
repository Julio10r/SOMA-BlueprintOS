namespace BlueprintOS.Core.Publication.Models;

/// <summary>
/// Representa um documento Markdown descoberto sob a raiz de <c>docs/</c>, pronto para ser
/// publicado. A categoria e o slug são derivados exclusivamente do caminho relativo em disco —
/// nenhuma lógica de audiência (executivo/cliente/engenharia) participa da descoberta.
/// </summary>
/// <param name="AbsolutePath">Caminho absoluto do arquivo Markdown fonte, em <c>docs/</c>.</param>
/// <param name="RelativePath">Caminho relativo à raiz de <c>docs/</c>, com separadores <c>/</c>.</param>
/// <param name="Category">
/// Subdiretório relativo à raiz de <c>docs/</c> onde o arquivo vive (ex.: <c>architecture</c>,
/// <c>backend/procurement</c>), usado como subpasta de destino em <c>dist/</c>. Vazio quando o
/// arquivo está na raiz de <c>docs/</c>.
/// </param>
/// <param name="Slug">Nome do arquivo, sem a extensão <c>.md</c>.</param>
public sealed record DiscoveredDocument(
    string AbsolutePath,
    string RelativePath,
    string Category,
    string Slug);
