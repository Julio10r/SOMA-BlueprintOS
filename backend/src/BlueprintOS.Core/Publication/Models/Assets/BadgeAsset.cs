namespace BlueprintOS.Core.Publication.Models.Assets;

/// <summary>
/// Status visual de um <see cref="BadgeAsset"/>, usado para escolher a cor do selo.
/// </summary>
public enum BadgeStatus
{
    /// <summary>Estado positivo (ex.: build com sucesso).</summary>
    Success,

    /// <summary>Estado de atenção (ex.: warnings presentes).</summary>
    Warning,

    /// <summary>Estado negativo (ex.: build falhou).</summary>
    Failure,

    /// <summary>Estado neutro/informativo.</summary>
    Neutral,
}

/// <summary>
/// Um selo curto (badge) do tipo "Build: passing", "Testes: 158", "Cobertura: —", renderizado
/// localmente (sem chamada a serviços externos como shields.io) a partir de indicadores reais
/// (ex.: <c>QualityMetrics</c>), para exibição na capa do documento.
/// </summary>
/// <param name="Id">Identificador estável do selo.</param>
/// <param name="Label">Rótulo à esquerda do selo (ex.: "Build").</param>
/// <param name="Value">Valor à direita do selo (ex.: "passing", "158").</param>
/// <param name="Status">Status visual do selo.</param>
public sealed record BadgeAsset(string Id, string Label, string Value, BadgeStatus Status);
