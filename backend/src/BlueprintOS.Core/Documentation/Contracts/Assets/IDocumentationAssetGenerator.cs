using BlueprintOS.Core.Documentation.Models.Assets;

namespace BlueprintOS.Core.Documentation.Contracts.Assets;

/// <summary>
/// Define o contrato do Asset Generator: produz os ativos de documentação reutilizáveis
/// (diagrama de arquitetura, diagrama de dependências, árvore da solução e relação entre
/// agentes) a partir de informações reais da solução, sem alterar o conteúdo textual da
/// documentação já publicada.
/// </summary>
public interface IDocumentationAssetGenerator
{
    /// <summary>
    /// Gera todos os ativos de documentação reutilizáveis.
    /// </summary>
    Task<IReadOnlyList<DocumentationAsset>> GenerateAllAsync(CancellationToken cancellationToken = default);
}
