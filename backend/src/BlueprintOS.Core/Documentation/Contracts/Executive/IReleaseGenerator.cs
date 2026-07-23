namespace BlueprintOS.Core.Documentation.Contracts.Executive;

/// <summary>
/// Define o contrato do gerador de notas de release/changelog executivo, refletindo o
/// histórico real de sprints concluídas (não há, até o momento, tags de release nem
/// arquivo CHANGELOG dedicado no repositório).
/// </summary>
public interface IReleaseGenerator
{
    /// <summary>
    /// Gera o corpo Markdown com as notas de release.
    /// </summary>
    Task<string> GenerateAsync(CancellationToken cancellationToken = default);
}
