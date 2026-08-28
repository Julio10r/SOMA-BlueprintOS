using BlueprintOS.Core.AI.Memory.Models;

namespace BlueprintOS.Core.AI.Memory.Contracts;

/// <summary>
/// Define o contrato da memória de negociações utilizada pelo agente comprador sênior
/// para tomar decisões com base em negociações anteriores.
/// </summary>
public interface INegotiationMemory
{
    /// <summary>
    /// Registra uma negociação concluída, atualizando o histórico do fornecedor, o
    /// histórico de preços do produto e o score do fornecedor.
    /// </summary>
    /// <param name="negotiation">Dados da negociação concluída.</param>
    void RegisterNegotiation(NegotiationRecord negotiation);

    /// <summary>
    /// Obtém o histórico consolidado do fornecedor informado, caso exista.
    /// </summary>
    /// <param name="supplierId">Identificador do fornecedor.</param>
    SupplierHistory? GetSupplierHistory(Guid supplierId);

    /// <summary>
    /// Obtém o histórico de preços registrado para o produto informado.
    /// </summary>
    /// <param name="productId">Identificador do produto.</param>
    IReadOnlyCollection<PriceHistory> GetPriceHistory(Guid productId);

    /// <summary>
    /// Calcula o score atual (0 a 100) do fornecedor informado, com base em seu histórico.
    /// </summary>
    /// <param name="supplierId">Identificador do fornecedor.</param>
    double CalculateSupplierScore(Guid supplierId);

    /// <summary>
    /// Identifica, entre os fornecedores que já negociaram o produto informado, aquele
    /// com o maior score atual.
    /// </summary>
    /// <param name="productId">Identificador do produto.</param>
    SupplierHistory? FindBestSupplier(Guid productId);

    /// <summary>
    /// Obtém o menor preço já registrado no histórico do produto informado.
    /// </summary>
    /// <param name="productId">Identificador do produto.</param>
    decimal? FindBestHistoricalPrice(Guid productId);

    /// <summary>
    /// Calcula a tendência de preço do produto informado, com base no histórico cronológico
    /// de preços registrados.
    /// </summary>
    /// <param name="productId">Identificador do produto.</param>
    PriceTrend GetPriceTrend(Guid productId);
}
