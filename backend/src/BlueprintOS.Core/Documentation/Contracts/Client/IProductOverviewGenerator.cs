namespace BlueprintOS.Core.Documentation.Contracts.Client;

/// <summary>
/// Define o contrato do gerador de visão geral do produto, voltado ao público cliente.
/// </summary>
public interface IProductOverviewGenerator
{
    /// <summary>
    /// Gera o corpo Markdown com a visão geral do produto.
    /// </summary>
    Task<string> GenerateAsync(CancellationToken cancellationToken = default);
}
