namespace BlueprintOS.Core.Documentation.Contracts.Client;

/// <summary>
/// Define o contrato do gerador de documentação de API voltada a clientes/integradores,
/// refletindo a superfície real de endpoints expostos por <c>BlueprintOS.Api</c>.
/// </summary>
public interface IApiDocumentationGenerator
{
    /// <summary>
    /// Gera o corpo Markdown da documentação de API para clientes.
    /// </summary>
    Task<string> GenerateAsync(CancellationToken cancellationToken = default);
}
