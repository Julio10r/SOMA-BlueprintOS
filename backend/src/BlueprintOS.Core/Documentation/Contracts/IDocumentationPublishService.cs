using BlueprintOS.Core.Documentation.Models;

namespace BlueprintOS.Core.Documentation.Contracts;

/// <summary>
/// Define o contrato do serviço de publicação do portal de documentação viva: executa todos os
/// geradores de documentação (executiva, cliente e engenharia), publica os documentos resultantes
/// em disco e atualiza os artefatos de memória da AI Factory (<c>.ai/ROADMAP.md</c>,
/// <c>.ai/memory/completed_sprints.md</c> e <c>.ai/memory/known_issues.md</c>).
/// Projetado para ser futuramente acionado por um motor de workflow, sem depender de sua existência.
/// </summary>
public interface IDocumentationPublishService
{
    /// <summary>
    /// Executa todos os geradores registrados e publica a documentação completa.
    /// </summary>
    Task<IReadOnlyList<PublishedDocument>> PublishAllAsync(CancellationToken cancellationToken = default);
}
