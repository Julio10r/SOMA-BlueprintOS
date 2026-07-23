namespace BlueprintOS.Core.Documentation.Contracts.Client;

/// <summary>
/// Define o contrato do gerador de guia funcional, descrevendo as capacidades funcionais
/// reais já implementadas nos módulos do backend.
/// </summary>
public interface IFunctionalGuideGenerator
{
    /// <summary>
    /// Gera o corpo Markdown do guia funcional.
    /// </summary>
    Task<string> GenerateAsync(CancellationToken cancellationToken = default);
}
