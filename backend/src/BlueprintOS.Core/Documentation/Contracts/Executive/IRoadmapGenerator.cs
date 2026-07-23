namespace BlueprintOS.Core.Documentation.Contracts.Executive;

/// <summary>
/// Define o contrato do gerador do roadmap executivo, refletindo o conteúdo real de
/// <c>.ai/ROADMAP.md</c>.
/// </summary>
public interface IRoadmapGenerator
{
    /// <summary>
    /// Gera o corpo Markdown do roadmap executivo.
    /// </summary>
    Task<string> GenerateAsync(CancellationToken cancellationToken = default);
}
