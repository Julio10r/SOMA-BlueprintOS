namespace BlueprintOS.Core.Documentation.Contracts.Engineering;

/// <summary>
/// Define o contrato do gerador de documentação de deploy, refletindo os artefatos reais
/// de containerização (<c>Dockerfile</c>, <c>docker-compose</c>) presentes no repositório.
/// </summary>
public interface IDeployGenerator
{
    /// <summary>
    /// Gera o corpo Markdown da documentação de deploy.
    /// </summary>
    Task<string> GenerateAsync(CancellationToken cancellationToken = default);
}
