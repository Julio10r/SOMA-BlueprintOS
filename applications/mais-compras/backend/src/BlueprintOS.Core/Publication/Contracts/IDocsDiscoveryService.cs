using BlueprintOS.Core.Publication.Models;

namespace BlueprintOS.Core.Publication.Contracts;

/// <summary>
/// Descobre os documentos Markdown publicáveis sob a raiz de <c>docs/</c>. Não depende de nomes
/// fixos como <c>executive</c>, <c>client</c> ou <c>engineering</c> — qualquer subpasta atual ou
/// futura de <c>docs/</c> é descoberta da mesma forma.
/// </summary>
public interface IDocsDiscoveryService
{
    /// <summary>
    /// Percorre <paramref name="docsRootPath"/> recursivamente, retornando todo arquivo
    /// <c>.md</c> publicável, em ordem determinística (caminho relativo, ordinal). Diretórios
    /// configurados como excluídos (ex.: <c>audits</c>, <c>demo</c>) não são percorridos.
    /// </summary>
    IReadOnlyList<DiscoveredDocument> Discover(string docsRootPath);
}
