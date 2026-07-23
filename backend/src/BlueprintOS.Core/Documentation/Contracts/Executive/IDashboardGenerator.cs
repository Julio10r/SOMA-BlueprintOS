namespace BlueprintOS.Core.Documentation.Contracts.Executive;

/// <summary>
/// Define o contrato do gerador do dashboard executivo, com o resumo de alto nível do
/// estado atual do projeto (fase, sprints concluídas, dívidas técnicas em aberto).
/// </summary>
public interface IDashboardGenerator
{
    /// <summary>
    /// Gera o corpo Markdown do dashboard executivo.
    /// </summary>
    Task<string> GenerateAsync(CancellationToken cancellationToken = default);
}
