using System.Data;
using BlueprintOS.Infrastructure.Integrations.ERP.Contracts;
using BlueprintOS.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BlueprintOS.Infrastructure.Integrations.ERP.Soma;

/// <summary>Leitura real de Unidades de Medida do SOMA_DESENV, mesmo padrão de <c>SomaContaContabilReader</c>.
/// Colunas reais confirmadas em `UNIDADES` (schema discovery dedicado do Bloco 2, B3): `UNIDADE` (chave,
/// char, PK real confirmada), `DESC_UNIDADE` (descrição, nullable — nem toda unidade tem descrição
/// preenchida). Colunas adicionais existentes (`USO_MATERIAIS`, `USO_PRODUTOS`, `TIPO_UNIDADE`,
/// `UNIDADE_RELACIONADA`, `INDICA_INTEIRO`) não são lidas: não fazem parte do conjunto mínimo homologado
/// para o cadastro de apoio de Unidade no +Compras (`ContratoFuncionalPreliminar-B3-ItemFiscal.md`).
/// Por padrão lê de `UNIDADES`, configurável via `ErpIntegration:SomaDesenvol:UnidadesSchema`/
/// `UnidadesTable`.</summary>
public sealed class SomaUnidadeMedidaReader(IConfiguration configuration, ILogger<SomaUnidadeMedidaReader> logger) : IUnidadeMedidaErpReader
{
    public async Task<IReadOnlyList<UnidadeMedidaErpDto>> BuscarUnidadesAsync(int skip, int take, CancellationToken cancellationToken = default)
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

        logger.LogInformation("Leitura operacional de unidades de medida SOMA iniciada. Skip {Skip}. Take {Take}", offset, pageSize);
        var unidades = new List<UnidadeMedidaErpDto>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) unidades.Add(Map(reader));
        return unidades;
    }

    public async Task<UnidadeMedidaErpDto?> BuscarPorCodigoAsync(string codigoErp, CancellationToken cancellationToken = default)
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
        var schema = configuration["ErpIntegration:SomaDesenvol:UnidadesSchema"] ?? "dbo";
        var table = configuration["ErpIntegration:SomaDesenvol:UnidadesTable"] ?? "UNIDADES";
        await using var command = connection.CreateCommand();
        command.CommandTimeout = TimeoutSeconds;
        command.CommandText = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @schema AND TABLE_NAME = @table ORDER BY ORDINAL_POSITION";
        command.Parameters.Add(new SqlParameter("@schema", schema));
        command.Parameters.Add(new SqlParameter("@table", table));
        var columns = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct)) columns.Add(reader.GetString(0));

        var shape = new TableShape(schema, table, columns);
        if (shape.CodigoColumn is null)
            throw new InvalidOperationException("Tabela de unidades de medida do ERP não possui a coluna mínima (código) configurada.");
        return shape;
    }

    private static UnidadeMedidaErpDto Map(IDataRecord reader) => new(
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
        // "unidade"/"desc_unidade" são os nomes reais confirmados em UNIDADES pelo schema discovery
        // dedicado do Bloco 2 (B3) — demais candidatos mantidos por tolerância a variações de ambiente.
        public string? CodigoColumn => Find(columns, "unidade", "cod_unidade", "codigo_unidade", "codigo");
        public string? DescricaoColumn => Find(columns, "desc_unidade", "descricao_unidade", "descricao");
        public string? UltimaAlteracaoColumn => Find(columns, "data_para_transferencia", "ultima_alteracao", "updated_at", "data_alteracao", "ultima_alteracao_em");
        public string CodigoColumnQ => Q(CodigoColumn!);
        public string SelectList => string.Join(", ", new[]
        {
            $"{Q(CodigoColumn!)} AS CodigoErp",
            Select(DescricaoColumn, "DescricaoErp"),
            Select(UltimaAlteracaoColumn, "UltimaAlteracaoEm"),
        });
        private static string? Find(IReadOnlyList<string> source, params string[] aliases) => source.FirstOrDefault(x => aliases.Contains(x, StringComparer.OrdinalIgnoreCase));
        private static string Select(string? column, string alias) => column is null ? $"NULL AS {alias}" : $"{Q(column)} AS {alias}";
        private static string Q(string value) => $"[{value.Replace("]", "]]", StringComparison.Ordinal)}]";
    }
}
