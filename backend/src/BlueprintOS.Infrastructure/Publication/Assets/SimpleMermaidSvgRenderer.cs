using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using BlueprintOS.Core.Publication.Models;

namespace BlueprintOS.Infrastructure.Publication.Assets;

/// <summary>
/// Gera um SVG simples (caixas e setas, layout vertical) a partir de uma definição Mermaid
/// básica (nós <c>Id[Rótulo]</c> e arestas <c>A --&gt; B</c> / <c>A --&gt;|rótulo| B</c>) — o
/// mesmo formato produzido pelos geradores de documentação do backend. Não é um motor Mermaid
/// completo: é o fallback explicitamente previsto para quando não há um motor de renderização
/// real disponível no backend (.NET puro, sem Node/Chromium), garantindo que nenhum código
/// Mermaid seja publicado como texto no documento.
/// </summary>
internal static class SimpleMermaidSvgRenderer
{
    private static readonly Regex NodePattern = new(@"^(\w+)\[([^\]]+)\]$", RegexOptions.Compiled);
    private static readonly Regex EdgePattern = new(
        @"^(\w+)\s*-->\s*(?:\|([^|]*)\|\s*)?(\w+)$", RegexOptions.Compiled);

    private const int BoxWidth = 260;
    private const int BoxHeight = 40;
    private const int RowGap = 70;
    private const int MarginX = 30;
    private const int MarginY = 30;

    public static byte[] Render(string mermaidDefinition, DocumentPalette palette)
    {
        var nodes = new Dictionary<string, string>(StringComparer.Ordinal);
        var order = new List<string>();
        var edges = new List<(string From, string To, string? Label)>();

        foreach (var rawLine in mermaidDefinition.Replace("\r\n", "\n").Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("graph", StringComparison.OrdinalIgnoreCase)
                || line.StartsWith("flowchart", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var nodeMatch = NodePattern.Match(line);
            if (nodeMatch.Success)
            {
                var id = nodeMatch.Groups[1].Value;
                if (!nodes.ContainsKey(id))
                {
                    order.Add(id);
                }

                nodes[id] = nodeMatch.Groups[2].Value;
                continue;
            }

            var edgeMatch = EdgePattern.Match(line);
            if (edgeMatch.Success)
            {
                var from = edgeMatch.Groups[1].Value;
                var to = edgeMatch.Groups[3].Value;
                var label = edgeMatch.Groups[2].Success ? edgeMatch.Groups[2].Value.Trim() : null;

                foreach (var id in new[] { from, to })
                {
                    if (!nodes.ContainsKey(id))
                    {
                        nodes[id] = id;
                        order.Add(id);
                    }
                }

                edges.Add((from, to, label));
            }
        }

        return RenderSvg(order, nodes, edges, palette);
    }

    private static byte[] RenderSvg(
        IReadOnlyList<string> order,
        IReadOnlyDictionary<string, string> nodes,
        IReadOnlyList<(string From, string To, string? Label)> edges,
        DocumentPalette palette)
    {
        var positions = new Dictionary<string, (int X, int Y)>(StringComparer.Ordinal);
        for (var i = 0; i < order.Count; i++)
        {
            positions[order[i]] = (MarginX, MarginY + i * RowGap);
        }

        var width = MarginX * 2 + BoxWidth;
        var height = order.Count == 0 ? MarginY * 2 + BoxHeight : positions[order[^1]].Y + BoxHeight + MarginY;

        var svg = new StringBuilder();
        svg.AppendLine($"""<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {width} {height}" width="{width}" height="{height}" font-family="sans-serif">""");
        svg.AppendLine($"""<defs><marker id="arrow" markerWidth="10" markerHeight="10" refX="8" refY="3" orient="auto"><path d="M0,0 L8,3 L0,6 Z" fill="{palette.MutedHex}" /></marker></defs>""");
        svg.AppendLine($"""<rect width="100%" height="100%" fill="{palette.SurfaceHex}" />""");

        foreach (var (from, to, label) in edges)
        {
            if (!positions.TryGetValue(from, out var fromPos) || !positions.TryGetValue(to, out var toPos))
            {
                continue;
            }

            var x1 = fromPos.X + BoxWidth / 2;
            var y1 = fromPos.Y + BoxHeight;
            var x2 = toPos.X + BoxWidth / 2;
            var y2 = toPos.Y;

            svg.AppendLine($"""<line x1="{x1}" y1="{y1}" x2="{x2}" y2="{y2}" stroke="{palette.MutedHex}" stroke-width="1.5" marker-end="url(#arrow)" />""");
            if (!string.IsNullOrEmpty(label))
            {
                var midX = (x1 + x2) / 2;
                var midY = (y1 + y2) / 2;
                svg.AppendLine($"""<text x="{midX + 6}" y="{midY - 4}" font-size="11" fill="{palette.MutedHex}">{Encode(label)}</text>""");
            }
        }

        foreach (var id in order)
        {
            var (x, y) = positions[id];
            svg.AppendLine($"""<rect x="{x}" y="{y}" width="{BoxWidth}" height="{BoxHeight}" rx="8" fill="{palette.BackgroundHex}" stroke="{palette.BorderHex}" stroke-width="1.5" />""");
            svg.AppendLine($"""<text x="{x + BoxWidth / 2}" y="{y + BoxHeight / 2 + 4}" font-size="13" fill="{palette.TextPrimaryHex}" text-anchor="middle">{Encode(nodes[id])}</text>""");
        }

        svg.AppendLine("</svg>");
        return Encoding.UTF8.GetBytes(svg.ToString());
    }

    private static string Encode(string text) => WebUtility.HtmlEncode(text);
}
