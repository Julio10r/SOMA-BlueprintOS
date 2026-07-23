namespace BlueprintOS.Core.Documentation.Contracts.Engineering;

/// <summary>
/// Define o contrato do gerador de documentação de decisões arquiteturais, refletindo
/// o conteúdo real de <c>.ai/DECISIONS.md</c>.
/// </summary>
public interface IDecisionsGenerator
{
    /// <summary>
    /// Gera o corpo Markdown com a lista de ADRs.
    /// </summary>
    Task<string> GenerateAsync(CancellationToken cancellationToken = default);
}
