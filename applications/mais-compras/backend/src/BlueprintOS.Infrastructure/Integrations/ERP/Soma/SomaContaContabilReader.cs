using System.Data;
using BlueprintOS.Infrastructure.Integrations.ERP.Contracts;
using BlueprintOS.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BlueprintOS.Infrastructure.Integrations.ERP.Soma;

/// <summary>Leitura real de Contas Contábeis do SOMA_DESENV, mesmo padrão de <c>SomaCentroCustoReader</c>:
/// introspecção dinâmica via INFORMATION_SCHEMA.COLUMNS com resolução de aliases candidatos. Colunas reais
/// confirmadas em `CTB_CONTA_PLANO` (schema discovery da B3, `docs/audits/Discovery-ItemFiscal-Pedido-
/// EntradaFiscal-Consumiveis.md`): `CONTA_CONTABIL` (chave), `DESC_CONTA` (descrição), `INATIVA` (status
/// real do ERP). Por padrão lê de `CTB_CONTA_PLANO`, configurável via
/// `ErpIntegration:SomaDesenvol:ContasContabeisSchema`/`ContasContabeisTable`.</summary>
public sealed class SomaContaContabilReader(IConfiguration configuration, ILogger<SomaContaContabilReader> logger) : IContaContabilErpReader
{
    public async Task<IReadOnlyList<ContaContabilErpDto>> BuscarContasContabeisAsync(int skip, int take, CancellationToken cancellationToken = default)
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

        logger.LogInformation("Leitura operacional de contas contábeis SOMA iniciada. Skip {Skip}. Take {Take}", offset, pageSize);
        var contas = new List<ContaContabilErpDto>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) contas.Add(Map(reader));
        return contas;
    }

    public async Task<ContaContabilErpDto?> BuscarPorCodigoAsync(string codigoErp, CancellationToken cancellationToken = default)
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
        var connectionString = LinxConnectionStringResolver.Resolve(configuration, LinxConnectionProfiles.Development);
        var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(ct);
        return connection;
    }

    private async Task<TableShape> LoadShapeAsync(SqlConnection connection, CancellationToken ct)
    {
        var schema = configuration["ErpIntegration:SomaDesenvol:ContasContabeisSchema"] ?? "dbo";
        var table = configuration["ErpIntegration:SomaDesenvol:ContasContabeisTable"] ?? "CTB_CONTA_PLANO";
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
            throw new InvalidOperationException("Tabela de contas contábeis do ERP não possui colunas mínimas (código/descrição) configuradas.");
        return shape;
    }

    private static ContaContabilErpDto Map(IDataRecord reader) => new(
        Convert.ToString(reader["CodigoErp"])!.Trim(),
        Convert.ToString(reader["DescricaoErp"])?.Trim() ?? string.Empty,
        reader["InativaNoErp"] is not DBNull && Convert.ToBoolean(reader["InativaNoErp"]),
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
        // "conta_contabil"/"desc_conta"/"inativa" são os nomes reais confirmados em CTB_CONTA_PLANO pelo
        // schema discovery da B3 (Discovery-ItemFiscal-Pedido-EntradaFiscal-Consumiveis.md) — demais
        // candidatos mantidos por tolerância a variações de ambiente, mesmo padrão dos demais readers.
        public string? CodigoColumn => Find(columns, "conta_contabil", "cod_conta_contabil", "codigo_conta_contabil", "codigo");
        public string? DescricaoColumn => Find(columns, "desc_conta", "descricao_conta", "descricao");
        public string? InativaColumn => Find(columns, "inativa", "inativo");
        public string? UltimaAlteracaoColumn => Find(columns, "data_para_transferencia", "ultima_alteracao", "updated_at", "data_alteracao", "ultima_alteracao_em");
        public string CodigoColumnQ => Q(CodigoColumn!);
        public string SelectList => string.Join(", ", new[]
        {
            $"{Q(CodigoColumn!)} AS CodigoErp",
            $"{Q(DescricaoColumn!)} AS DescricaoErp",
            Select(InativaColumn, "InativaNoErp"),
            Select(UltimaAlteracaoColumn, "UltimaAlteracaoEm"),
        });
        private static string? Find(IReadOnlyList<string> source, params string[] aliases) => source.FirstOrDefault(x => aliases.Contains(x, StringComparer.OrdinalIgnoreCase));
        private static string Select(string? column, string alias) => column is null ? $"NULL AS {alias}" : $"{Q(column)} AS {alias}";
        private static string Q(string value) => $"[{value.Replace("]", "]]", StringComparison.Ordinal)}]";
    }
}
