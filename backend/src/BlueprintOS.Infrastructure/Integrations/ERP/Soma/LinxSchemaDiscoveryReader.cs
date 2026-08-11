using BlueprintOS.Infrastructure.Integrations.ERP.Contracts;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BlueprintOS.Infrastructure.Integrations.ERP.Soma;

/// <summary>Introspecção dinâmica de schema do `SOMA_DESENV` para o Linx Database Specialist (Work Order
/// O1.13.5, seção 9) — mesmo padrão de conexão/guarda de <c>SomaFilialReader</c>/<c>SomaCentroCustoReader</c>
/// (B2.1/O1.7): assume `ConnectionStrings:ErpConnection`, recusa-se a abrir se o banco não for
/// `SOMA_DESENV`. Read-only por construção: o único texto de SQL emitido consulta
/// `INFORMATION_SCHEMA.TABLES`/`INFORMATION_SCHEMA.COLUMNS`, nunca uma tabela de dados, e nunca contém
/// `INSERT`/`UPDATE`/`DELETE`/`MERGE`/`ALTER`/`DROP`/`CREATE`/`GRANT`/`REVOKE`/`TRUNCATE`/`EXEC` — não há
/// nenhum parâmetro de entrada interpolado como comando SQL (schema/tabela são sempre parâmetros
/// tipados).</summary>
public sealed class LinxSchemaDiscoveryReader(IConfiguration configuration, ILogger<LinxSchemaDiscoveryReader> logger) : ILinxSchemaDiscoveryReader
{
    public async Task<IReadOnlyList<LinxTabelaDto>> ListarTabelasAsync(string? schema, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandTimeout = TimeoutSeconds;
        command.CommandText = string.IsNullOrWhiteSpace(schema)
            ? "SELECT TABLE_SCHEMA, TABLE_NAME, TABLE_TYPE FROM INFORMATION_SCHEMA.TABLES ORDER BY TABLE_SCHEMA, TABLE_NAME"
            : "SELECT TABLE_SCHEMA, TABLE_NAME, TABLE_TYPE FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = @schema ORDER BY TABLE_NAME";
        if (!string.IsNullOrWhiteSpace(schema))
        {
            command.Parameters.Add(new SqlParameter("@schema", schema));
        }

        logger.LogInformation("Descoberta de schema Linx (tabelas) iniciada. Schema {Schema}", schema ?? "(todos)");
        var tabelas = new List<LinxTabelaDto>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            tabelas.Add(new LinxTabelaDto(reader.GetString(0), reader.GetString(1), reader.GetString(2)));
        }

        return tabelas;
    }

    public async Task<IReadOnlyList<LinxColunaDto>> ListarColunasAsync(string schema, string tabela, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(schema)) throw new ArgumentException("Schema é obrigatório.", nameof(schema));
        if (string.IsNullOrWhiteSpace(tabela)) throw new ArgumentException("Tabela é obrigatória.", nameof(tabela));

        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandTimeout = TimeoutSeconds;
        command.CommandText = "SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, ORDINAL_POSITION FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @schema AND TABLE_NAME = @table ORDER BY ORDINAL_POSITION";
        command.Parameters.Add(new SqlParameter("@schema", schema));
        command.Parameters.Add(new SqlParameter("@table", tabela));

        logger.LogInformation("Descoberta de schema Linx (colunas) iniciada. Schema {Schema}. Tabela {Tabela}", schema, tabela);
        var colunas = new List<LinxColunaDto>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            colunas.Add(new LinxColunaDto(
                reader.GetString(0),
                reader.GetString(1),
                string.Equals(reader.GetString(2), "YES", StringComparison.OrdinalIgnoreCase),
                reader.IsDBNull(3) ? null : reader.GetInt32(3)));
        }

        return colunas;
    }

    private async Task<SqlConnection> OpenAsync(CancellationToken ct)
    {
        var connectionString = configuration.GetConnectionString("ErpConnection");
        if (string.IsNullOrWhiteSpace(connectionString) || connectionString.StartsWith("__SET_", StringComparison.Ordinal))
            throw new InvalidOperationException("ConnectionStrings:ErpConnection deve ser configurada via User Secrets ou variável de ambiente.");
        var builder = new SqlConnectionStringBuilder(connectionString);
        if (!string.Equals(builder.InitialCatalog, "SOMA_DESENV", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("LinxSchemaDiscoveryReader exige o banco SOMA_DESENV.");
        var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(ct);
        return connection;
    }

    private int TimeoutSeconds => int.TryParse(configuration["ErpIntegration:TimeoutSeconds"], out var value) ? Math.Clamp(value, 1, 120) : 30;
}
