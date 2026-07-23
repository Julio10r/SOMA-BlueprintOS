namespace BlueprintOS.Core.Documentation.Contracts.Engineering;

/// <summary>
/// Define o contrato do gerador de documentação de arquitetura de engenharia, agregando
/// a documentação técnica real de cada módulo do backend.
/// </summary>
public interface IArchitectureGenerator
{
    /// <summary>
    /// Gera o corpo Markdown da documentação de arquitetura.
    /// </summary>
    Task<string> GenerateAsync(CancellationToken cancellationToken = default);
}
