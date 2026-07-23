using BlueprintOS.Core.AI.Memory.Models;

namespace BlueprintOS.Core.AI.Memory.Contracts;

/// <summary>
/// Define o contrato de persistência utilizado pela memória de negociações. Abstrai o
/// mecanismo de armazenamento (memória, banco de dados, etc.) do restante da lógica de negócio.
/// </summary>
public interface INegotiationMemoryStore
{
    /// <summary>
    /// Obtém o histórico consolidado do fornecedor informado, caso exista.
    /// </summary>
    /// <param name="supplierId">Identificador do fornecedor.</param>
    SupplierHistory? GetSupplierHistory(Guid supplierId);

    /// <summary>
    /// Persiste o histórico consolidado do fornecedor.
    /// </summary>
    /// <param name="history">Histórico a ser persistido.</param>
    void SaveSupplierHistory(SupplierHistory history);

    /// <summary>
    /// Obtém as métricas agregadas de pontuação do fornecedor informado, criando uma
    /// instância vazia caso ainda não exista.
    /// </summary>
    /// <param name="supplierId">Identificador do fornecedor.</param>
    SupplierScoringMetrics GetOrCreateScoringMetrics(Guid supplierId);

    /// <summary>
    /// Persiste as métricas agregadas de pontuação do fornecedor.
    /// </summary>
    /// <param name="metrics">Métricas a serem persistidas.</param>
    void SaveScoringMetrics(SupplierScoringMetrics metrics);

    /// <summary>
    /// Adiciona um novo registro ao histórico de preços de um produto.
    /// </summary>
    /// <param name="priceHistory">Registro de preço a ser adicionado.</param>
    void AddPriceHistory(PriceHistory priceHistory);

    /// <summary>
    /// Obtém o histórico de preços registrado para o produto informado.
    /// </summary>
    /// <param name="productId">Identificador do produto.</param>
    IReadOnlyCollection<PriceHistory> GetPriceHistory(Guid productId);

    /// <summary>
    /// Registra que um fornecedor já negociou o produto informado, permitindo consultas
    /// futuras por melhor fornecedor de um produto.
    /// </summary>
    /// <param name="productId">Identificador do produto.</param>
    /// <param name="supplierId">Identificador do fornecedor.</param>
    void LinkSupplierToProduct(Guid productId, Guid supplierId);

    /// <summary>
    /// Obtém os fornecedores que já negociaram o produto informado.
    /// </summary>
    /// <param name="productId">Identificador do produto.</param>
    IReadOnlyCollection<Guid> GetSuppliersForProduct(Guid productId);
}
