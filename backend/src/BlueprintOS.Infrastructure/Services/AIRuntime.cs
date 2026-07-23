using BlueprintOS.Core.AI.Contracts;
using BlueprintOS.Core.AI.Models;

namespace BlueprintOS.Infrastructure.Services;

/// <summary>
/// Implementação de <see cref="IAIRuntime"/> que seleciona o provedor de IA registrado
/// correspondente ao provedor indicado na requisição e delega a execução a ele.
/// </summary>
public sealed class AIRuntime : IAIRuntime
{
    private readonly IEnumerable<IAIProvider> _providers;

    public AIRuntime(IEnumerable<IAIProvider> providers)
    {
        _providers = providers;
    }

    public Task<AIResponse> ExecuteAsync(AIRequest request, CancellationToken cancellationToken = default)
    {
        var provider = _providers.FirstOrDefault(p =>
            string.Equals(p.Name, request.Model.Provider, StringComparison.OrdinalIgnoreCase));

        if (provider is null)
        {
            throw new InvalidOperationException(
                $"Nenhum provedor de IA registrado para '{request.Model.Provider}'.");
        }

        return provider.CompleteAsync(request, cancellationToken);
    }
}
