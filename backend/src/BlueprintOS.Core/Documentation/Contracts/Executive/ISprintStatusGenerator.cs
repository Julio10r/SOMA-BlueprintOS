namespace BlueprintOS.Core.Documentation.Contracts.Executive;

/// <summary>
/// Define o contrato do gerador de status de sprint, refletindo a entrada mais recente de
/// <c>.ai/memory/completed_sprints.md</c>.
/// </summary>
public interface ISprintStatusGenerator
{
    /// <summary>
    /// Gera o corpo Markdown com o status da sprint mais recente.
    /// </summary>
    Task<string> GenerateAsync(CancellationToken cancellationToken = default);
}
