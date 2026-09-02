namespace BlueprintOS.Application.Procurement.Suppliers.Contracts;

/// <summary>Gate de homologação de Fornecedores (2026-09-01): cidade passa a ser selecionada de uma
/// lista real de municípios da UF escolhida (nunca texto livre), pelo mesmo motivo de UF ser combo
/// fechado — evita erro de digitação/normalização. Fonte: IBGE (localidades por UF), consultada
/// sempre pelo backend, nunca diretamente pelo frontend.</summary>
public interface IMunicipioProvider
{
    Task<IReadOnlyList<string>> ListarPorUfAsync(string uf, CancellationToken cancellationToken = default);
}
