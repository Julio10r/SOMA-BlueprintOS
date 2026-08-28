namespace BlueprintOS.Core.Publication.Models.Assets;

/// <summary>
/// Um diagrama Mermaid. A fonte (<see cref="Definition"/>) já existe hoje (gerada pelo módulo
/// <c>Documentation</c>); a rasterização para imagem (<see cref="RenderedImageBytes"/>) é o
/// ponto de extensão ainda não implementado nesta sprint — nenhuma dependência de rasterização
/// (ex.: Mermaid CLI headless) foi adicionada. Enquanto <see cref="RenderedImageBytes"/> for
/// nulo, os renderizadores exibem a definição como bloco de código, de forma honesta (sem
/// fabricar uma imagem). Este modelo também serve de base para futuros fluxogramas, diagramas
/// BPMN/C4, organogramas e fluxos de agentes/integrações, todos expressáveis como texto Mermaid.
/// </summary>
/// <param name="Id">Identificador estável, referenciado por um <c>ContentBlock</c> de imagem via <c>AssetId</c> quando renderizado.</param>
/// <param name="Title">Título/legenda do diagrama.</param>
/// <param name="Definition">Definição do diagrama em sintaxe Mermaid.</param>
/// <param name="RenderedImageBytes">Imagem já rasterizada do diagrama (PNG/SVG), quando disponível.</param>
/// <param name="RenderedMediaType">Tipo MIME de <paramref name="RenderedImageBytes"/>, quando presente.</param>
public sealed record MermaidAsset(
    string Id,
    string Title,
    string Definition,
    byte[]? RenderedImageBytes = null,
    string? RenderedMediaType = null);
