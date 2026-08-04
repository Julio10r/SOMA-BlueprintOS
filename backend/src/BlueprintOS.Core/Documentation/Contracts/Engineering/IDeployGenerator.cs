namespace BlueprintOS.Core.Documentation.Contracts.Engineering;

/// <summary>
/// Define o contrato do gerador de documentação de deploy, refletindo o ambiente de
/// desenvolvimento real do repositório (sem Docker — ver ADR-0019).
/// </summary>
public interface IDeployGenerator
{
    /// <summary>
    /// Gera o corpo Markdown da documentação de deploy.
    /// </summary>
    Task<string> GenerateAsync(CancellationToken cancellationToken = default);
}
