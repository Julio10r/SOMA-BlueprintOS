using BlueprintOS.Domain.Integrations.Occurrences;

namespace BlueprintOS.Application.Integrations.Contracts;

public interface IIntegrationOccurrenceRepository
{
    /// <summary>Insere um lote de ocorrências em uma única operação de persistência — nunca uma
    /// SaveChangesAsync por ocorrência (mesma decisão de "processamento em lotes" já aplicada a
    /// Fornecedor/vínculo).</summary>
    Task AdicionarLoteAsync(IReadOnlyList<IntegrationOccurrence> ocorrencias, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IntegrationOccurrence>> ListarPorExecucaoAsync(Guid executionId, CancellationToken cancellationToken = default);
}
