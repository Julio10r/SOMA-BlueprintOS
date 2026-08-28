namespace BlueprintOS.Infrastructure.Integrations.ERP.Contracts;

/// <summary>Descoberta read-only de schema do `SOMA_DESENV`, usada pelo Linx Database Specialist (Work
/// Order O1.13.5, seção 9). Deliberadamente expõe apenas leitura de metadados (`INFORMATION_SCHEMA`) — não
/// existe, e nunca deve existir, nenhum método capaz de executar `INSERT`/`UPDATE`/`DELETE`/`ALTER`/`DROP`
/// ou qualquer DDL/DML de escrita nesta interface. Nenhum SQL arbitrário do chamador é aceito: apenas nome
/// de schema/tabela, usados como parâmetro em consultas fixas a `INFORMATION_SCHEMA`.</summary>
public interface ILinxSchemaDiscoveryReader
{
    Task<IReadOnlyList<LinxTabelaDto>> ListarTabelasAsync(string? schema, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LinxColunaDto>> ListarColunasAsync(string schema, string tabela, CancellationToken cancellationToken = default);
}

public sealed record LinxTabelaDto(string Schema, string Tabela, string Tipo);

public sealed record LinxColunaDto(string Coluna, string TipoDado, bool Nulavel, int? OrdinalPosition);
