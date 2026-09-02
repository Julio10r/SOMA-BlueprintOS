using System.Text.Json;
using System.Text.Json.Serialization;
using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using Microsoft.Extensions.Options;

namespace BlueprintOS.Infrastructure.Integrations.Ibge;

/// <summary>Lista municípios reais de uma UF via API pública do IBGE
/// (localidades/estados/{uf}/municipios) — mesma fonte de referência oficial de município usada
/// pelo governo. Nunca lança para UF inexistente/erro de rede: retorna lista vazia, deixando o
/// combo de cidade sem opções (o use case decide o fallback).</summary>
public sealed class IbgeMunicipioProvider(HttpClient httpClient, IOptions<IbgeMunicipioOptions> options) : IMunicipioProvider
{
    public async Task<IReadOnlyList<string>> ListarPorUfAsync(string uf, CancellationToken cancellationToken = default)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(Math.Max(1, options.Value.TimeoutSeconds)));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

        using var response = await httpClient.GetAsync($"{uf}/municipios", linked.Token);
        if (!response.IsSuccessStatusCode) return [];

        var corpo = await response.Content.ReadAsStringAsync(linked.Token);
        List<IbgeMunicipioResponse>? municipios;
        try
        {
            municipios = JsonSerializer.Deserialize<List<IbgeMunicipioResponse>>(corpo);
        }
        catch (JsonException)
        {
            return [];
        }

        if (municipios is null) return [];
        return municipios
            .Select(m => m.Nome)
            .Where(nome => !string.IsNullOrWhiteSpace(nome))
            .Select(nome => nome!.Trim())
            .OrderBy(nome => nome, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private sealed record IbgeMunicipioResponse([property: JsonPropertyName("nome")] string? Nome);
}
