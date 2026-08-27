using System.Data;
using BlueprintOS.Infrastructure.Integrations.ERP.Contracts;
using BlueprintOS.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BlueprintOS.Infrastructure.Integrations.ERP.Soma;

/// <summary>Leitura real de Filiais do SOMA_DESENV, mesmo padrão de <c>SomaFornecedorReader</c>
/// (B2.1/B2.1.2): introspecção dinâmica via INFORMATION_SCHEMA.COLUMNS com resolução de aliases candidatos,
/// para tolerar nomes de coluna variáveis — não hardcoda um schema que não foi confirmado neste ambiente.
/// Por padrão lê de `CADASTRO_CLI_FOR` (mesma tabela já usada como fonte de Filial/CliFor pelo leitor de
/// Fornecedores), configurável via `ErpIntegration:SomaDesenvol:FiliaisSchema`/`FiliaisTable`.</summary>
public sealed class SomaFilialReader(IConfiguration configuration, ILogger<SomaFilialReader> logger) : IFilialErpReader
{
    public async Task<IReadOnlyList<FilialErpDto>> BuscarFiliaisAsync(int skip, int take, CancellationToken cancellationToken = default)
    {
        var offset = Math.Max(0, skip);
        var pageSize = Math.Clamp(take <= 0 ? 100 : take, 1, 5000);
        await using var connection = await OpenAsync(cancellationToken);
        var shape = await LoadShapeAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandTimeout = TimeoutSeconds;
        command.CommandText = $"SELECT {shape.SelectList} FROM {shape.Table} WHERE {shape.CodigoColumnQ} IS NOT NULL ORDER BY {shape.CodigoColumnQ} OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY";
        command.Parameters.Add(new SqlParameter("@skip", SqlDbType.Int) { Value = offset });
        command.Parameters.Add(new SqlParameter("@take", SqlDbType.Int) { Value = pageSize });

        logger.LogInformation("Leitura operacional de filiais SOMA iniciada. Skip {Skip}. Take {Take}", offset, pageSize);
        var filiais = new List<FilialErpDto>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) filiais.Add(Map(reader));
        return filiais;
    }

    public async Task<FilialErpDto?> BuscarPorCodigoAsync(string codigoCliFor, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(codigoCliFor)) return null;
        await using var connection = await OpenAsync(cancellationToken);
        var shape = await LoadShapeAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandTimeout = TimeoutSeconds;
        command.CommandText = $"SELECT {shape.SelectList} FROM {shape.Table} WHERE {shape.CodigoColumnQ} = @codigo";
        command.Parameters.Add(new SqlParameter("@codigo", SqlDbType.NVarChar, 100) { Value = codigoCliFor.Trim() });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
    }

    private async Task<SqlConnection> OpenAsync(CancellationToken ct)
    {
        var connectionString = LinxConnectionStringResolver.Resolve(configuration, LinxConnectionProfiles.Development);
        var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(ct);
        return connection;
    }

    private async Task<TableShape> LoadShapeAsync(SqlConnection connection, CancellationToken ct)
    {
        var schema = configuration["ErpIntegration:SomaDesenvol:FiliaisSchema"] ?? "dbo";
        var table = configuration["ErpIntegration:SomaDesenvol:FiliaisTable"] ?? "CADASTRO_CLI_FOR";
        await using var command = connection.CreateCommand();
        command.CommandTimeout = TimeoutSeconds;
        command.CommandText = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @schema AND TABLE_NAME = @table ORDER BY ORDINAL_POSITION";
        command.Parameters.Add(new SqlParameter("@schema", schema));
        command.Parameters.Add(new SqlParameter("@table", table));
        var columns = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct)) columns.Add(reader.GetString(0));

        var shape = new TableShape(schema, table, columns);
        if (shape.CodigoColumn is null || shape.NomeColumn is null)
            throw new InvalidOperationException("Tabela de filiais do ERP não possui colunas mínimas (código/nome) configuradas.");
        return shape;
    }

    private static FilialErpDto Map(IDataRecord reader) => new(
        Convert.ToString(reader["CodigoCliFor"])!.Trim(),
        Convert.ToString(reader["NomeCliFor"])?.Trim() ?? string.Empty,
        Nullable(reader, "UnidadeNegocioErpId"),
        ParseDate(reader, "UltimaAlteracaoEm"));

    private static string? Nullable(IDataRecord reader, string name) => reader[name] is DBNull ? null : Convert.ToString(reader[name])?.Trim();

    private static DateTimeOffset? ParseDate(IDataRecord reader, string name)
    {
        if (reader[name] is DBNull) return null;
        var local = DateTime.SpecifyKind(Convert.ToDateTime(reader[name]), DateTimeKind.Unspecified);
        var zone = TimeZoneInfo.FindSystemTimeZoneById(OperatingSystem.IsWindows() ? "E. South America Standard Time" : "America/Sao_Paulo");
        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local, zone));
    }

    private int TimeoutSeconds => int.TryParse(configuration["ErpIntegration:TimeoutSeconds"], out var value) ? Math.Clamp(value, 1, 120) : 30;

    private sealed class TableShape(string schema, string table, IReadOnlyList<string> columns)
    {
        public string Table { get; } = $"[{schema.Replace("]", "]]", StringComparison.Ordinal)}].[{table.Replace("]", "]]", StringComparison.Ordinal)}]";
        public string? CodigoColumn => Find(columns, "cod_clifor", "codigo_clifor", "cod_cliente_fornecedor", "cod_filial", "codigo_filial", "codigo");
        public string? NomeColumn => Find(columns, "nome_clifor", "nome_filial", "razao_social", "nome");
        public string? UnidadeColumn => Find(columns, "unidade_negocio", "cod_unidade_negocio", "unidade_negocio_id", "empresa");
        public string? UltimaAlteracaoColumn => Find(columns, "data_para_transferencia", "ultima_alteracao", "updated_at", "data_alteracao", "ultima_alteracao_em");
        public string CodigoColumnQ => Q(CodigoColumn!);
        public string SelectList => string.Join(", ", new[]
        {
            $"{Q(CodigoColumn!)} AS CodigoCliFor",
            $"{Q(NomeColumn!)} AS NomeCliFor",
            Select(UnidadeColumn, "UnidadeNegocioErpId"),
            Select(UltimaAlteracaoColumn, "UltimaAlteracaoEm"),
        });
        private static string? Find(IReadOnlyList<string> source, params string[] aliases) => source.FirstOrDefault(x => aliases.Contains(x, StringComparer.OrdinalIgnoreCase));
        private static string Select(string? column, string alias) => column is null ? $"NULL AS {alias}" : $"{Q(column)} AS {alias}";
        private static string Q(string value) => $"[{value.Replace("]", "]]", StringComparison.Ordinal)}]";
    }
}
