namespace BlueprintOS.Core.Documentation.Contracts.Client;

/// <summary>
/// Define o contrato do gerador de perguntas frequentes (FAQ). Quando não houver conteúdo
/// de FAQ real registrado, o gerador deve produzir um documento mínimo e honesto.
/// </summary>
public interface IFaqGenerator
{
    /// <summary>
    /// Gera o corpo Markdown do FAQ.
    /// </summary>
    Task<string> GenerateAsync(CancellationToken cancellationToken = default);
}
