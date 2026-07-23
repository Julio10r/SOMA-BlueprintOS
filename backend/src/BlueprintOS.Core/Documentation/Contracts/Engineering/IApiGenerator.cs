namespace BlueprintOS.Core.Documentation.Contracts.Engineering;

/// <summary>
/// Define o contrato do gerador de documentação técnica de API, refletindo os endpoints
/// reais mapeados em <c>BlueprintOS.Api/Program.cs</c>.
/// </summary>
public interface IApiGenerator
{
    /// <summary>
    /// Gera o corpo Markdown da documentação técnica de API.
    /// </summary>
    Task<string> GenerateAsync(CancellationToken cancellationToken = default);
}
