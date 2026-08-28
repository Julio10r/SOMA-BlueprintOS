using BlueprintOS.Core.Documentation.Contracts;
using Microsoft.Extensions.Logging;

namespace BlueprintOS.Infrastructure.Documentation;

/// <summary>
/// Implementação no-op (apenas log) de <see cref="IDocumentationMemoryNotifier"/>.
/// </summary>
/// <remarks>
/// O BlueprintOS ainda não possui um módulo de Memória genérico — apenas memória específica de
/// negociação (<c>INegotiationMemory</c>). Esta implementação apenas registra o evento via
/// <see cref="ILogger"/>, deixando o ponto de extensão pronto para uma integração completa futura.
/// </remarks>
public sealed class NoOpDocumentationMemoryNotifier : IDocumentationMemoryNotifier
{
    private readonly ILogger<NoOpDocumentationMemoryNotifier> _logger;

    public NoOpDocumentationMemoryNotifier(ILogger<NoOpDocumentationMemoryNotifier> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task NotifyAsync(string documentId, string eventDescription, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Evento de documentação para {DocumentId}: {EventDescription}",
            documentId,
            eventDescription);

        return Task.CompletedTask;
    }
}
