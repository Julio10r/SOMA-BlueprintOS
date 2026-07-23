using BlueprintOS.Core.Documentation.Models;

namespace BlueprintOS.Core.Documentation.Contracts;

/// <summary>
/// Define o contrato de persistência (CRUD) de <see cref="DocumentationEntry"/>.
/// </summary>
public interface IDocumentationRepository
{
    /// <summary>
    /// Adiciona um novo documento ao repositório.
    /// </summary>
    Task<DocumentationEntry> AddAsync(DocumentationEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém um documento pelo seu identificador, ou <c>null</c> se não existir.
    /// </summary>
    Task<DocumentationEntry?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista todos os documentos armazenados.
    /// </summary>
    Task<IReadOnlyList<DocumentationEntry>> ListAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Atualiza um documento existente.
    /// </summary>
    Task<DocumentationEntry> UpdateAsync(DocumentationEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove um documento pelo seu identificador. Retorna <c>true</c> se removido com sucesso.
    /// </summary>
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
}
