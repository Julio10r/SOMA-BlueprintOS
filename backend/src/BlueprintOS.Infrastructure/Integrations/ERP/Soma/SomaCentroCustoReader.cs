using System.Data;
using BlueprintOS.Infrastructure.Integrations.ERP.Contracts;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BlueprintOS.Infrastructure.Integrations.ERP.Soma;

/// <summary>Leitura real de Centros de Custo do SOMA_DESENV, mesmo padrão de <c>SomaFornecedorReader</c>
/// (B2.1/B2.1.2): introspecção dinâmica via INFORMATION_SCHEMA.COLUMNS com resolução de aliases candidatos,
/// para tolerar nomes de coluna variáveis — não hardcoda um schema que não foi confirmado neste ambiente.
/// Por padrão lê de `CENTRO_CUSTO`, configurável via `ErpIntegration:SomaDesenvol:CentrosCustoSchema`/
/// `CentrosCustoTable`.</summary>
public sealed class SomaCentroCustoReader(IConfiguration configuration, ILogger<SomaCentroCustoReader> logger) : ICentroCustoErpReader
{
    public async Task<IReadOnlyList<CentroCustoErpDto>> BuscarCentrosCustoAsync(int skip, int take, CancellationToken cancellationToken = default)
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

        logger.LogInformation("Leitura operacional de centros de custo SOMA iniciada. Skip {Skip}. Take {Take}", offset, pageSize);
        var centros = new List<CentroCustoErpDto>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) centros.Add(Map(reader));
        return centros;
    }

    public async Task<CentroCustoErpDto?> BuscarPorCodigoAsync(string codigoErp, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(codigoErp)) return null;
        await using var connection = await OpenAsync(cancellationToken);
        var shape = await LoadShapeAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandTimeout = TimeoutSeconds;
        command.CommandText = $"SELECT {shape.SelectList} FROM {shape.Table} WHERE {shape.CodigoColumnQ} = @codigo";
        command.Parameters.Add(new SqlParameter("@codigo", SqlDbType.NVarChar, 100) { Value = codigoErp.Trim() });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
    }

    private async Task<SqlConnection> OpenAsync(CancellationToken ct)
    {
        var connectionString = configuration.GetConnectionString("ErpConnection");
        if (string.IsNullOrWhiteSpace(connectionString) || connectionString.StartsWith("__SET_", StringComparison.Ordinal))
            throw new InvalidOperationException("ConnectionStrings:ErpConnection deve ser configurada via User Secrets ou variável de ambiente.");
        var builder = new SqlConnectionStringBuilder(connectionString);
        if (!string.Equals(builder.InitialCatalog, "SOMA_DESENV", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("SomaCentroCustoReader exige o banco SOMA_DESENV.");
        var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(ct);
        return connection;
    }

    private async Task<TableShape> LoadShapeAsync(SqlConnection connection, CancellationToken ct)
    {
        var schema = configuration["ErpIntegration:SomaDesenvol:CentrosCustoSchema"] ?? "dbo";
        var table = configuration["ErpIntegration:SomaDesenvol:CentrosCustoTable"] ?? "CENTRO_CUSTO";
        await using var command = connection.CreateCommand();
        command.CommandTimeout = TimeoutSeconds;
        command.CommandText = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @schema AND TABLE_NAME = @table ORDER BY ORDINAL_POSITION";
        command.Parameters.Add(new SqlParameter("@schema", schema));
        command.Parameters.Add(new SqlParameter("@table", table));
        var columns = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct)) columns.Add(reader.GetString(0));

        var shape = new TableShape(schema, table, columns);
        if (shape.CodigoColumn is null || shape.DescricaoColumn is null)
            throw new InvalidOperationException("Tabela de centros de custo do ERP não possui colunas mínimas (código/descrição) configuradas.");
        return shape;
    }

    private static CentroCustoErpDto Map(IDataRecord reader) => new(
        Convert.ToString(reader["CodigoErp"])!.Trim(),
        Convert.ToString(reader["DescricaoErp"])?.Trim() ?? string.Empty,
        ParseDate(reader, "UltimaAlteracaoEm"));

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
        // "centro_custo"/"desc_centro_custo" são os nomes reais confirmados na tabela física
        // `CENTROS_CUSTO` do SOMA_DESENV (validação funcional do #41, Gate Final da Onda 1, continuação
        // 12/08/2026) — os demais candidatos são mantidos por tolerância a variações de ambiente,
        // conforme já documentado na classe.
        public string? CodigoColumn => Find(columns, "centro_custo", "cod_centro_custo", "codigo_centro_custo", "cod_ccusto", "codigo_ccusto", "codigo");
        public string? DescricaoColumn => Find(columns, "desc_centro_custo", "descricao_centro_custo", "descricao_ccusto", "descricao", "nome");
        public string? UltimaAlteracaoColumn => Find(columns, "data_para_transferencia", "ultima_alteracao", "updated_at", "data_alteracao", "ultima_alteracao_em");
        public string CodigoColumnQ => Q(CodigoColumn!);
        public string SelectList => string.Join(", ", new[]
        {
            $"{Q(CodigoColumn!)} AS CodigoErp",
            $"{Q(DescricaoColumn!)} AS DescricaoErp",
            Select(UltimaAlteracaoColumn, "UltimaAlteracaoEm"),
        });
        private static string? Find(IReadOnlyList<string> source, params string[] aliases) => source.FirstOrDefault(x => aliases.Contains(x, StringComparer.OrdinalIgnoreCase));
        private static string Select(string? column, string alias) => column is null ? $"NULL AS {alias}" : $"{Q(column)} AS {alias}";
        private static string Q(string value) => $"[{value.Replace("]", "]]", StringComparison.Ordinal)}]";
    }
}
