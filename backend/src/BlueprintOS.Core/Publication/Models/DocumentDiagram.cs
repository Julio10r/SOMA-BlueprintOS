using BlueprintOS.Core.Publication.Models.Assets;

namespace BlueprintOS.Core.Publication.Models;

/// <summary>
/// Resultado de renderizar um diagrama Mermaid para uso num documento: a seção já pronta (com um
/// <see cref="ContentBlock"/> de imagem, nunca com código Mermaid bruto) e o
/// <see cref="MermaidAsset"/> correspondente, que deve ser incluído em
/// <see cref="PublicationAssets.Mermaid"/> para que a imagem seja resolvida na renderização.
/// </summary>
public sealed record DocumentDiagram(PublicationSection Section, MermaidAsset Asset);
