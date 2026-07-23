namespace BlueprintOS.Core.Publication.Models.Assets;

/// <summary>
/// Tipo de visualização de um <see cref="ChartAsset"/>. Cobre tanto os gráficos simples
/// suportados nativamente nesta sprint (<see cref="Bar"/>, <see cref="Gauge"/>) quanto os
/// pontos de extensão para os gráficos futuros do roadmap (evolução de sprints, cobertura de
/// testes, dívida técnica) — todos modelados com a mesma forma de dados (<see cref="ChartDataPoint"/>),
/// diferenciando-se apenas na interpretação visual de cada renderizador.
/// </summary>
public enum ChartKind
{
    /// <summary>Barras horizontais simples (suportado nativamente).</summary>
    Bar,

    /// <summary>Indicador único tipo velocímetro/gauge (suportado nativamente como texto/barra única).</summary>
    Gauge,

    /// <summary>Série temporal (ex.: evolução de sprints, cobertura ao longo do tempo).</summary>
    Line,

    /// <summary>Tabela de KPIs com indicadores visuais (ex.: setas de tendência).</summary>
    KpiTable,
}

/// <summary>
/// Um ponto de dado de um <see cref="ChartAsset"/>: rótulo, valor numérico e um rótulo de
/// tendência opcional (ex.: "+12%"), usado pelos indicadores visuais de KPI.
/// </summary>
/// <param name="Label">Rótulo do ponto (ex.: nome do indicador, sprint ou período).</param>
/// <param name="Value">Valor numérico do ponto.</param>
/// <param name="Trend">Rótulo de tendência opcional (ex.: "+12%", "▲"), sem interpretação semântica própria ainda.</param>
public sealed record ChartDataPoint(string Label, double Value, string? Trend = null);

/// <summary>
/// Um gráfico simples (KPIs, indicadores) representado de forma agnóstica ao renderizador:
/// título, tipo e uma série de <see cref="ChartDataPoint"/>. Nesta sprint, os renderizadores
/// desenham apenas uma representação textual/barras simples a partir destes dados — não há
/// motor de gráficos completo; isso é o ponto de extensão para os gráficos futuros de
/// evolução de sprints, cobertura de testes e dívida técnica, que poderão reaproveitar
/// exatamente este modelo.
/// </summary>
/// <param name="Id">Identificador estável, referenciado por um <c>ContentBlock</c> de gráfico via <c>AssetId</c>.</param>
/// <param name="Title">Título do gráfico.</param>
/// <param name="Kind">Tipo de visualização.</param>
/// <param name="DataPoints">Série de dados do gráfico.</param>
public sealed record ChartAsset(string Id, string Title, ChartKind Kind, IReadOnlyList<ChartDataPoint> DataPoints);
