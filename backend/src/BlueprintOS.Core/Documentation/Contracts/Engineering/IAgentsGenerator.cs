namespace BlueprintOS.Core.Documentation.Contracts.Engineering;

/// <summary>
/// Define o contrato do gerador de documentação do módulo de Agentes de IA
/// (<c>BlueprintOS.Core.Agents</c>).
/// </summary>
public interface IAgentsGenerator
{
    /// <summary>
    /// Gera o corpo Markdown da documentação de agentes.
    /// </summary>
    Task<string> GenerateAsync(CancellationToken cancellationToken = default);
}
