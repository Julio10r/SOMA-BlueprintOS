namespace BlueprintOS.Core.Documentation.Contracts;

/// <summary>
/// Ponto de extensão para notificar o sistema de memória do BlueprintOS sobre eventos de
/// documentação (criação, versionamento, etc.).
/// </summary>
/// <remarks>
/// Hoje o BlueprintOS possui apenas memória específica de negociação (<c>INegotiationMemory</c>),
/// sem um módulo de Memória genérico. Este contrato existe como ponto de extensão não disruptivo
/// para uma futura integração com um módulo de Memória genérico (ver ADR correspondente e
/// <c>.ai/memory/known_issues.md</c>).
/// </remarks>
public interface IDocumentationMemoryNotifier
{
    /// <summary>
    /// Notifica, de forma best-effort, que um evento relacionado a documentação ocorreu.
    /// </summary>
    Task NotifyAsync(string documentId, string eventDescription, CancellationToken cancellationToken = default);
}
