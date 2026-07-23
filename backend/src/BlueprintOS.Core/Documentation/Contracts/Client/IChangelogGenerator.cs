namespace BlueprintOS.Core.Documentation.Contracts.Client;

/// <summary>
/// Define o contrato do gerador de changelog voltado a clientes, refletindo o histórico
/// real de sprints concluídas registrado em <c>.ai/memory/completed_sprints.md</c>.
/// </summary>
public interface IChangelogGenerator
{
    /// <summary>
    /// Gera o corpo Markdown do changelog.
    /// </summary>
    Task<string> GenerateAsync(CancellationToken cancellationToken = default);
}
