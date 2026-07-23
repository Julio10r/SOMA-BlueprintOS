namespace BlueprintOS.Core.Documentation.Contracts.Engineering;

/// <summary>
/// Define o contrato do gerador de runbook operacional. Quando não houver procedimentos
/// operacionais reais registrados, o gerador deve produzir um documento mínimo e honesto.
/// </summary>
public interface IRunbookGenerator
{
    /// <summary>
    /// Gera o corpo Markdown do runbook.
    /// </summary>
    Task<string> GenerateAsync(CancellationToken cancellationToken = default);
}
